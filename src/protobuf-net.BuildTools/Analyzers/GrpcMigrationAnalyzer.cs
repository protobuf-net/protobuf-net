#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;

namespace ProtoBuf.BuildTools.Analyzers
{
    /// <summary>
    /// The gRPC counterpart of <see cref="AotMigrationAnalyzer"/>'s <c>PBN3012</c>: a project that asks
    /// for AOT or trimming, uses protobuf-net.Grpc, and has not squared the circle with
    /// <c>[ProtoGrpc]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Worth having separately from the serializer one because the failure is *further* from the
    /// developer: turning on <c>PublishAot</c> changes nothing at build time, the JIT run keeps working,
    /// and the first sign of trouble is a native publish or a startup that cannot bind. Both halves of
    /// protobuf-net.Grpc reach reflection without a model - the proxies through <c>ProxyEmitter</c>, the
    /// payloads through <c>MarshallerCache</c> - so there is nothing to fall back to.
    /// </para>
    /// <para>
    /// <b>The trigger is consumer-side usage, not the presence of service contracts</b>, and that
    /// distinction is load-bearing. Shipping <c>[Service]</c> interfaces in a shared package is the
    /// recommended layout, and such a package needs no <c>[ProtoGrpc]</c> of its own - its consumers do.
    /// Triggering on contract declarations would therefore nag hardest at exactly the project that is
    /// laid out correctly. So the two things that mean "this project is a client or a server" are what
    /// count: a plain <c>CreateGrpcService&lt;T&gt;</c>, and the server's <c>AddCodeFirstGrpc</c>.
    /// </para>
    /// <para>
    /// A <c>CreateGrpcService</c> call that already passes a factory is left alone - that consumer has
    /// done the thing we would ask for. It is the *plain* form that is going to reflect.
    /// </para>
    /// <para>
    /// A warning rather than an error, for the same reason <c>PBN3012</c> is: switching on
    /// <c>PublishAot</c> should not break someone's build on the spot. Anyone wanting it to is one
    /// <c>WarningsAsErrors</c> away.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GrpcMigrationAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor NoProtoGrpcUnderAot = new(
            id: "PBN4015",
            title: "This project publishes AOT or trimmed, but has no build-time gRPC proxies",
            messageFormat: "This project uses protobuf-net.Grpc and asks for {0}, but declares no "
                + "[ProtoGrpc]; the proxies and their payload marshallers will be built by reflection, "
                + "which is exactly what will not survive. See https://grpc.protobuf-net.dev/aot",
            category: "ProtoBuf.Grpc",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(NoProtoGrpcUnderAot);

        private const string ProtoGrpcAttributeName = "ProtoBuf.Grpc.Configuration.ProtoGrpcAttribute";
        private const string ClientFactoryHolder = "ProtoBuf.Grpc.Client.GrpcClientFactory";
        private const string ServerExtensions = "ProtoBuf.Grpc.Server.ServicesExtensions";
        private const string CreateGrpcService = "CreateGrpcService";
        private const string AddCodeFirstGrpc = "AddCodeFirstGrpc";

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            ctx.RegisterCompilationStartAction(static compilationStart =>
            {
                // the opening line everywhere in this assembly: declining the tooling costs one lookup
                if (compilationStart.Options.AnalyzerConfigOptionsProvider.BuildToolsDisabled()) return;

                // nothing to say to a project that has not asked for AOT or trimming; the runtime model
                // is a perfectly good way to use protobuf-net.Grpc
                if (compilationStart.Options.AnalyzerConfigOptionsProvider.AsksForAot() is not string asked) return;

                var compilation = compilationStart.Compilation;

                // ...nor to one that has already done what we would ask for
                if (compilation.GetTypeByMetadataName(ProtoGrpcAttributeName) is not { } protoGrpc) return;
                if (HasAnyProtoGrpc(compilation, protoGrpc)) return;

                compilationStart.RegisterOperationAction(context =>
                {
                    if (context.Operation is not IInvocationOperation invocation) return;
                    if (!IsUnmigratedUsage(invocation)) return;

                    context.ReportDiagnostic(Diagnostic.Create(
                        NoProtoGrpcUnderAot, invocation.Syntax.GetLocation(), asked));
                }, OperationKind.Invocation);
            });
        }

        /// <summary>
        /// Whether any type in this compilation carries <c>[ProtoGrpc]</c>.
        /// </summary>
        /// <remarks>
        /// Only this assembly: a <c>[ProtoGrpc]</c> in a referenced one names its own model and does
        /// nothing for this project, exactly as for seeding.
        /// </remarks>
        private static bool HasAnyProtoGrpc(Compilation compilation, INamedTypeSymbol protoGrpc)
        {
            foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
            {
                foreach (var attribute in type.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, protoGrpc)) return true;
                }
            }
            return false;
        }

        private static System.Collections.Generic.IEnumerable<INamedTypeSymbol> EnumerateTypes(
            INamespaceOrTypeSymbol root)
        {
            foreach (var member in root.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol ns:
                        foreach (var nested in EnumerateTypes(ns)) yield return nested;
                        break;
                    case INamedTypeSymbol type:
                        yield return type;
                        foreach (var nested in EnumerateTypes(type)) yield return nested;
                        break;
                }
            }
        }

        private static bool IsUnmigratedUsage(IInvocationOperation invocation)
        {
            var method = invocation.TargetMethod;
            var holder = method.ContainingType?.ToDisplayString();

            if (holder == ServerExtensions && method.Name == AddCodeFirstGrpc) return true;

            if (holder != ClientFactoryHolder || method.Name != CreateGrpcService) return false;

            // A call that already passes a factory is fine - that is the shape we would ask for. An
            // omitted optional argument arrives as ArgumentKind.DefaultValue, and an explicit null is
            // the same thing said out loud, so both count as "plain".
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.Type is not INamedTypeSymbol type) continue;
                if (type.ToDisplayString() != "ProtoBuf.Grpc.Configuration.ClientFactory") continue;

                return argument.ArgumentKind == ArgumentKind.DefaultValue
                    || argument.Value.ConstantValue is { HasValue: true, Value: null };
            }
            return true;
        }
    }
}
