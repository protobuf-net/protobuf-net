#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.BuildTools.Internal.Aot;
using ProtoBuf.BuildTools.Internal.Connect;
using ProtoBuf.BuildTools.Internal.Grpc;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Emits build-time Connect (connectrpc.com) proxies and server bindings for a consumer-declared
    /// <c>[ProtoConnect] partial class</c>, seeded by <c>[ProtoService]</c>.
    /// </summary>
    /// <remarks>
    /// A third generator in this assembly, beside <c>ProtoModelGenerator</c> and
    /// <c>GrpcProxyGenerator</c>. It <b>shares</b> the latter's contract classification rather than
    /// forking it - see <see cref="GrpcProxyGenerator.ParseContract"/>, which is transport-neutral -
    /// and differs only in what it emits.
    /// <para>
    /// The seeding vocabulary is protobuf-net.Grpc's unchanged: <c>[ProtoService]</c> already names a
    /// contract and optionally an implementation, so a Connect-specific copy would be a second thing
    /// to keep in step. Only the container attribute selects the transport. Both are matched by
    /// <b>full name</b>, as every trigger attribute here is, so the tests can stub them.
    /// </para>
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed partial class ProtoConnectGenerator : IIncrementalGenerator
    {
        internal const string ProtoConnectAttributeName = "ProtoBuf.Connect.ProtoConnectAttribute";

        /// <summary>
        /// Marks an operation free of side effects, so it may be served over <c>GET</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Matched by full name, like every other trigger here, and deliberately <em>not</em> probed for
        /// before use: this only ever asks "does this method carry it", which is false in a compilation
        /// that has never heard of the type. Nothing is emitted that names it.
        /// </para>
        /// <para>
        /// It lives in protobuf-net.Connect rather than here, so a consumer needs a recent enough
        /// runtime as well as recent enough tooling - the versioning cost the split imposes, and the
        /// reason the attribute carries no data: there is nothing about it to evolve.
        /// </para>
        /// </remarks>
        internal const string NoSideEffectsAttributeName = "ProtoBuf.Connect.NoSideEffectsAttribute";

        /// <summary>C# 12 is the floor, matching the other two generators here.</summary>
        internal const int MinimumLanguageVersion = 1200;

        internal const string PlanTrackingName = "ConnectPlan";

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // No post-initialization output: [ProtoConnect] is real API in protobuf-net.Connect, so a
            // project that has never heard of it simply never fires this - which costs nothing and says
            // nothing, the right answer for both.
            var disabled = context.AnalyzerConfigOptionsProvider
                .Select(static (options, _) => options.BuildToolsDisabled());

            var plans = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ProtoConnectAttributeName,
                    predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
                    transform: static (ctx, ct) => ParseModel(ctx, ct))
                .WithTrackingName(PlanTrackingName);

            // Diagnostics on their own track, as both siblings do: they carry locations, which move
            // whenever anything above them moves, while the plan does not - so the emit stays cached
            // across edits that only shuffle code.
            context.RegisterSourceOutput(plans.Combine(disabled), static (ctx, pair) =>
            {
                var (candidate, off) = pair;
                if (candidate is null || off) return;
                foreach (var diagnostic in candidate.Diagnostics) ctx.ReportDiagnostic(ToDiagnostic(diagnostic));
            });

            context.RegisterSourceOutput(plans.Combine(disabled), static (ctx, pair) =>
            {
                var (candidate, off) = pair;
                if (candidate is null || off || candidate.Plan is not { } plan) return;
                ctx.AddSource(HintName(plan), Emit(plan));
            });
        }

        private static string HintName(ConnectContainerPlan plan)
            => (plan.ContainerNamespace is { Length: > 0 } ns ? ns + "." : "") + plan.ContainerName + ".connect.g.cs";

        private static ConnectCandidate? ParseModel(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
        {
            if (ctx.TargetSymbol is not INamedTypeSymbol container) return null;

            var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

            if (((CSharpParseOptions)ctx.SemanticModel.SyntaxTree.Options).LanguageVersion < (LanguageVersion)MinimumLanguageVersion)
            {
                // the kind enum is GrpcDiagnosticKind by name only - its members say what is wrong with
                // a *contract*, which is transport-neutral, so Connect reuses them and maps to its own
                // PBN5xxx descriptors rather than inventing a parallel set
                diagnostics.Add(new DiagnosticInfo(
                    GrpcDiagnosticKind.LanguageVersionTooLow,
                    container.Locations.FirstOrDefault(),
                    container.ToDisplayString()));
                return new ConnectCandidate(null, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
            }

            var modelType = ReadModel(ctx.Attributes);
            var services = ImmutableArray.CreateBuilder<GrpcInterfaceModel>();

            foreach (var attribute in container.GetAttributes())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (attribute.AttributeClass?.ToDisplayString() != GrpcProxyGenerator.ProtoServiceAttributeName) continue;
                if (attribute.ConstructorArguments.Length == 0) continue;
                if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol contract) continue;

                var implementation = attribute.ConstructorArguments.Length > 1
                    ? attribute.ConstructorArguments[1].Value as INamedTypeSymbol
                    : null;

                // the shared classification: every method shape, context kind, void/Empty rule and
                // [SubService] walk arrives from here rather than being re-derived
                //
                // The compilation is passed so the parse also reconstructs each operation's endpoint
                // metadata. That is not optional here the way it is for gRPC: protobuf-net.Grpc's
                // binding can fall back to the reflective ServiceBinder.GetMetadata, and does, whereas
                // nothing on this path reflects - so an operation whose metadata is not reconstructed
                // gets none, and an [Authorize] on the implementation would be silently dropped. The
                // fallback here is PBN5008 rather than a runtime lookup.
                var parsed = GrpcProxyGenerator.ParseContract(contract, implementation, cancellationToken,
                    payloadSink: null, compilation: ctx.SemanticModel.Compilation);
                foreach (var diagnostic in parsed.Diagnostics) diagnostics.Add(diagnostic);
                if (parsed.Model is not { } model) continue;

                // Byte streaming is classified by the shared parse but not emitted here. It is NOT a
                // duplex shape - protobuf-net.Grpc carries Task<Stream> as a *server-streaming* call of
                // BytesValue - and we do have server streaming, so this is a gap rather than an
                // impossibility. What is missing is a bytes-carrier message with its own marshalling,
                // plus the Stream-to-chunks reshape: protobuf-net.Grpc's lives in Reshape.WriteStream
                // and ServerByteStreaming*Async, which work against Grpc.Core's writer types. Refusing
                // loudly beats emitting something that compiles and then truncates a download.
                if (TryFindByteStream(model) is { } offending)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        GrpcDiagnosticKind.UnsupportedMethodShape,
                        contract.Locations.FirstOrDefault(),
                        model.InterfaceFullName,
                        offending,
                        "it returns a Stream, which is not implemented yet - protobuf-net.Grpc carries "
                            + "that as a server-streaming call of a bytes message, which needs a "
                            + "carrier type and a Stream-to-chunks reshape this generator does not have"));
                    continue;
                }

                // Connect GET is unary-only, so the attribute is inert anywhere else; say so rather than
                // letting a consumer believe a streaming method is cacheable
                foreach (var op in model.Operations)
                {
                    if (op.HttpMethodAttribute is { } verb)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            GrpcDiagnosticKind.InertHttpMethodAttribute,
                            contract.Locations.FirstOrDefault(),
                            model.InterfaceFullName,
                            op.MethodName,
                            StripAttributeSuffix(verb)));
                    }

                    if (op.NoSideEffects && op.Kind != GrpcMethodKind.Unary)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            GrpcDiagnosticKind.NoSideEffectsNotUnary,
                            contract.Locations.FirstOrDefault(),
                            model.InterfaceFullName,
                            op.MethodName,
                            Describe(op.Kind)));
                    }
                }

                services.Add(model);
            }

            var plan = new ConnectContainerPlan(
                containerNamespace: container.ContainingNamespace is { IsGlobalNamespace: false } ns
                    ? ns.ToDisplayString() : null,
                containerName: container.Name,
                accessibility: container.DeclaredAccessibility == Accessibility.Public ? "public" : "internal",
                isStatic: container.IsStatic,
                declaresConstructor: container.InstanceConstructors.Any(static c => !c.IsImplicitlyDeclared),
                modelTypeFullName: modelType,
                services: new EquatableArray<GrpcInterfaceModel>(services.ToArray()),
                // by metadata name, exactly as ProtoModelGenerator probes for the JSON seam: the two
                // packages version independently, so the type being nameable has to be established
                // rather than assumed
                jsonCodecAvailable: ctx.SemanticModel.Compilation
                    .GetTypeByMetadataName("ProtoBuf.Connect.JsonConnectCodec") is not null);

            return new ConnectCandidate(plan, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
        }

        /// <summary>How the attribute is written in source, rather than its type name.</summary>
        private static string StripAttributeSuffix(string name)
            => name.EndsWith("Attribute", System.StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Attribute".Length) : name;

        /// <summary>A method kind in the words a consumer would use.</summary>
        private static string Describe(GrpcMethodKind kind) => kind switch
        {
            GrpcMethodKind.ClientStreaming => "client-streaming",
            GrpcMethodKind.ServerStreaming => "server-streaming",
            GrpcMethodKind.DuplexStreaming => "bidirectional-streaming",
            _ => "unary",
        };

        /// <summary>
        /// The first operation returning a <see cref="System.IO.Stream"/>, if any.
        /// </summary>
        private static string? TryFindByteStream(GrpcInterfaceModel model)
        {
            foreach (var op in model.Operations)
            {
                if (op.ResponseShape is GrpcResultShape.TaskStream or GrpcResultShape.ValueTaskStream)
                {
                    return op.MethodName;
                }
            }
            return null;
        }

        private static string? ReadModel(ImmutableArray<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                foreach (var named in attribute.NamedArguments)
                {
                    if (named.Key == "Model" && named.Value.Value is INamedTypeSymbol model)
                    {
                        return model.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }
                }
            }
            return null;
        }
    }
}
