#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.BuildTools.Internal.Grpc;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Fills in a consumer-declared <c>[ProtoGrpc] partial class X : ClientFactory</c> with
    /// compile-time gRPC client proxies and server bindings, so that protobuf-net.Grpc does not have
    /// to build them by reflection and ref-emit at run time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape is deliberately the one <c>[ProtoModel]</c> already uses for serializers: the
    /// consumer declares a type, the generator fills in the other half, and everything is reached by
    /// name. There is no registry, no <c>[ModuleInitializer]</c>, and nothing keyed on
    /// <see cref="System.Type"/> at run time - which is the property that survives ILC. A registry
    /// would leave the payload marshallers resolving through
    /// <c>MarshallerCache.CreateMarshaller&lt;T&gt;</c> -&gt; <c>CanSerialize(typeof(T))</c> -&gt;
    /// <c>DynamicStub</c> -&gt; <c>MakeGenericType</c>, which is exactly what native AOT removes.
    /// </para>
    /// <para>
    /// Consequently this needs <em>no</em> changes to protobuf-net.Grpc: <c>ClientFactory</c> is
    /// already an abstract class with an implicit protected constructor and two overridable members,
    /// and <c>Grpc.AspNetCore.Server</c>'s <c>IServiceMethodProvider&lt;T&gt;</c> is registered
    /// through <c>TryAddEnumerable</c>, so a generated provider can simply be added alongside.
    /// </para>
    /// <para>
    /// The trigger attributes are real API in protobuf-net.Grpc (1.3.0+), marked
    /// <c>[Experimental("PBN9001")]</c> — the same id protobuf-net's <c>[ProtoModel]</c> uses, so one
    /// <c>NoWarn</c> opts into both halves. They were generator-owned (emitted per consuming assembly
    /// as <c>internal</c> from <c>RegisterPostInitializationOutput</c>) while the shape was moving,
    /// which is how <c>[ProtoModel]</c> started and what kept this off the critical path of a
    /// protobuf-net.Grpc release.
    /// </para>
    /// <para>
    /// They are still matched by <b>full name</b> rather than by symbol, and that must stay: the unit
    /// tests declare their own stubs — both to dodge the <c>[Experimental]</c> gate and because the
    /// golden tests compile against a snapshot of the runtime surface rather than the package.
    /// </para>
    /// <para>
    /// Note for anyone adding a third trigger attribute: a generator can only see its <em>own</em>
    /// post-initialization sources from <c>ForAttributeWithMetadataName</c>, so a generator-owned
    /// attribute has to be post-init and cannot be ordinary output.
    /// </para>
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed partial class GrpcProxyGenerator : IIncrementalGenerator
    {
        internal const string ProtoGrpcAttributeName = "ProtoBuf.Grpc.Configuration.ProtoGrpcAttribute";
        internal const string ProtoServiceAttributeName = "ProtoBuf.Grpc.Configuration.ProtoServiceAttribute";

        /// <summary>The protobuf-net.Grpc marker for a service contract.</summary>
        internal const string ServiceAttributeName = "ProtoBuf.Grpc.Configuration.ServiceAttribute";

        /// <summary>The WCF marker, which protobuf-net.Grpc also honours as a service contract.</summary>
        internal const string ServiceContractAttributeName = "System.ServiceModel.ServiceContractAttribute";

        /// <summary>Marks a base interface whose operations are bound as part of the inheriting contract.</summary>
        internal const string SubServiceAttributeName = "ProtoBuf.Grpc.Configuration.SubServiceAttribute";

        /// <summary>
        /// The lowest C# version this generator emits for. The emitted code is nullable-annotated and
        /// uses target-typed <c>new</c>, and the consumer's model is a partial - C# 9 covers all of it.
        /// </summary>
        /// <remarks>
        /// Spelled numerically because this assembly compiles against Roslyn 4.3.1, which predates the
        /// named constant; the numeric values are stable and at run time we bind to the host's Roslyn.
        /// </remarks>
        internal const LanguageVersion MinimumLanguageVersion = (LanguageVersion)900; // C# 9.0

        internal const string MinimumLanguageVersionDisplay = "9.0";

        /// <summary>Names the plan step, so tests can assert that its results are actually cached.</summary>
        internal const string PlanTrackingName = "GrpcPlan";

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // No post-initialization output: the trigger attributes are real API in
            // protobuf-net.Grpc 1.3.0+. If they are absent - an older package, or a project that has
            // never heard of protobuf-net.Grpc - ForAttributeWithMetadataName simply never fires, so
            // this generator costs nothing and says nothing, which is the right answer for both.
            var capabilities = context.CompilationProvider
                .Combine(context.ParseOptionsProvider)
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select(static (pair, _) => Capabilities.From(
                    pair.Left.Left, pair.Left.Right, pair.Right));

            var plans = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ProtoGrpcAttributeName,
                    predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
                    transform: static (ctx, ct) => ParseModel(ctx, ct))
                .WithTrackingName(PlanTrackingName);

            // Diagnostics travel on their own track, deliberately: they carry locations, which shift
            // whenever anything above them moves, while the plan does not - so the emit step stays
            // cached across edits that only move code around. Same split ProtoModelGenerator uses.
            context.RegisterSourceOutput(plans.Combine(capabilities), static (ctx, pair) =>
            {
                var (candidate, caps) = pair;
                if (candidate is null || caps.Disabled) return;
                foreach (var diagnostic in candidate.Diagnostics) ctx.ReportDiagnostic(diagnostic.ToDiagnostic());
            });

            context.RegisterSourceOutput(plans.Combine(capabilities), static (ctx, pair) =>
            {
                var (candidate, caps) = pair;
                if (candidate is null || caps.Disabled || candidate.Plan is not { } plan) return;

                if (caps.LanguageVersion < MinimumLanguageVersion)
                {
                    ctx.ReportDiagnostic(new DiagnosticInfo(
                        LanguageVersionTooLow, null, plan.TypeName, MinimumLanguageVersionDisplay).ToDiagnostic());
                    return;
                }

                ctx.AddSource(HintName(plan), Emit(plan, caps.HasAspNetCore));
            });
        }

        private static string HintName(GrpcModelPlan plan)
        {
            var prefix = string.IsNullOrEmpty(plan.NamespaceName) ? "" : plan.NamespaceName + ".";
            return prefix + plan.TypeName + ".grpc.g.cs";
        }

        internal static bool HasAttribute(ISymbol symbol, string attributeMetadataName)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == attributeMetadataName) return true;
            }
            return false;
        }

        /// <summary>
        /// What the compilation can actually support, resolved once per compilation.
        /// </summary>
        private readonly struct Capabilities
        {
            private Capabilities(bool disabled, bool hasAspNetCore, LanguageVersion languageVersion)
            {
                Disabled = disabled;
                HasAspNetCore = hasAspNetCore;
                LanguageVersion = languageVersion;
            }

            /// <summary>Whether <c>ProtoBufDisableBuildTools</c> turned the whole of BuildTools off.</summary>
            public bool Disabled { get; }

            /// <summary>Whether the server half can be emitted; see <see cref="GrpcModelPlan.HasAspNetCore"/>.</summary>
            public bool HasAspNetCore { get; }

            public LanguageVersion LanguageVersion { get; }

            public static Capabilities From(Compilation compilation, ParseOptions parseOptions,
                Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptionsProvider configOptions)
            {
                if (configOptions.BuildToolsDisabled()) return new Capabilities(true, false, LanguageVersion.Default);

                var hasAspNetCore = compilation.GetTypeByMetadataName(
                    "Grpc.AspNetCore.Server.Model.ServiceMethodProviderContext`1") is not null;

                var languageVersion = parseOptions is CSharpParseOptions csharp
                    ? csharp.LanguageVersion
                    : LanguageVersion.Default;

                return new Capabilities(false, hasAspNetCore, languageVersion);
            }
        }

    }
}
