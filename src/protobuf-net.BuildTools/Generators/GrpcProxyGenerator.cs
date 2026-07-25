#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal.Grpc;
using System.Collections.Immutable;
using System.Text;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Generates compile-time gRPC client proxies and server bindings for the code-first service
    /// contracts (<c>[Service]</c> / <c>[ServiceContract]</c> interfaces) in a compilation, so that
    /// protobuf-net.Grpc does not have to build them by reflection and <c>ref-emit</c> at runtime.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated code registers itself with <c>ProtoBuf.Grpc.Internal.GeneratedProxyRegistry</c>
    /// from a <c>[ModuleInitializer]</c>, which both the client factory and the server binder consult
    /// before falling back to the runtime path; nothing is stamped onto the user's own types.
    /// </para>
    /// <para>
    /// Contracts whose operations are not in a shape this generator emits are left entirely to the
    /// runtime path (which handles a wider set of shapes) and reported as diagnostics: a partially
    /// generated proxy would be worse than none.
    /// </para>
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed partial class GrpcProxyGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// The protobuf-net.Grpc marker for a service contract.
        /// </summary>
        internal const string ServiceAttributeName = "ProtoBuf.Grpc.Configuration.ServiceAttribute";

        /// <summary>
        /// The WCF marker, which protobuf-net.Grpc also honours as a service contract.
        /// </summary>
        internal const string ServiceContractAttributeName = "System.ServiceModel.ServiceContractAttribute";

        /// <summary>
        /// An explicit <c>[Proxy(typeof(...))]</c> means the user supplied their own proxy; we defer.
        /// </summary>
        internal const string ProxyAttributeName = "ProtoBuf.Grpc.Configuration.ProxyAttribute";

        /// <summary>
        /// The lowest C# version this generator emits for; the emitted code is nullable-annotated,
        /// which is C# 8 and up. Note that netstandard2.0/net4x projects default to C# 7.3, so those
        /// consumers must set <c>LangVersion</c> explicitly to get build-time proxies.
        /// </summary>
        /// <remarks>
        /// Spelled numerically because this analyzer compiles against Roslyn 4.3.1; the numeric
        /// values are stable, and at runtime we bind to whatever Roslyn the host supplies.
        /// </remarks>
        internal const LanguageVersion MinimumLanguageVersion = (LanguageVersion)800; // C# 8.0

        internal const string MinimumLanguageVersionDisplay = "8.0";

        /// <summary>
        /// Names the contract-parsing step, so tests can assert that its results are actually cached.
        /// </summary>
        internal const string ContractTrackingName = "GrpcContracts";

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // What the emitted code needs from the reference set and the compiler; if protobuf-net.Grpc
            // isn't referenced at all, this generator has nothing to say - not even a diagnostic, since
            // [ServiceContract] on its own is just as likely to be a plain WCF contract.
            var capabilities = context.CompilationProvider
                .Combine(context.ParseOptionsProvider)
                .Select(static (pair, _) => Capabilities.From(pair.Left, pair.Right));

            // An interface can carry both attributes; the [Service] provider wins, and the
            // [ServiceContract] provider skips anything that also has [Service]. Doing it this way
            // (rather than collecting everything and de-duplicating) keeps each contract on its own
            // incremental track, so editing one file doesn't re-emit every proxy in the project.
            var serviceContracts = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ServiceAttributeName,
                    predicate: static (node, _) => node is InterfaceDeclarationSyntax,
                    transform: static (ctx, ct) => Parse(ctx, ct))
                .WithTrackingName(ContractTrackingName);

            var wcfContracts = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ServiceContractAttributeName,
                    predicate: static (node, _) => node is InterfaceDeclarationSyntax,
                    transform: static (ctx, ct) => HasAttribute(ctx.TargetSymbol, ServiceAttributeName) ? null : Parse(ctx, ct))
                .WithTrackingName(ContractTrackingName);

            foreach (var contracts in new[] { serviceContracts, wcfContracts })
            {
                context.RegisterSourceOutput(contracts.Combine(capabilities), static (ctx, pair) =>
                {
                    var (candidate, caps) = pair;
                    if (candidate is null || !caps.HasRuntimeSupport) return;

                    foreach (var diagnostic in candidate.Diagnostics)
                    {
                        ctx.ReportDiagnostic(diagnostic.ToDiagnostic());
                    }

                    if (candidate.Model is not { } model) return;

                    if (!CanEmit(caps.HasRuntimeSupport, caps.HasModuleInitializer, caps.LanguageVersion, out var blocker))
                    {
                        if (blocker is not null)
                        {
                            ctx.ReportDiagnostic(new DiagnosticInfo(
                                blocker, null, model.InterfaceFullName, MinimumLanguageVersionDisplay).ToDiagnostic());
                        }
                        return;
                    }

                    ctx.AddSource(HintName(model), Emit(model));
                });
            }
        }

        /// <summary>
        /// Whether a contract can be given a build-time proxy here, and what to say when it can't.
        /// </summary>
        /// <remarks>
        /// A missing runtime API is deliberately silent: it means either that protobuf-net.Grpc isn't
        /// referenced at all (so a <c>[ServiceContract]</c> is just a WCF contract and none of this is
        /// any of our business), or that it predates the generated-proxy API - in which case the
        /// runtime path is exactly what the consumer already had.
        /// </remarks>
        internal static bool CanEmit(bool hasRuntimeSupport, bool hasModuleInitializer, LanguageVersion languageVersion, out DiagnosticDescriptor? blocker)
        {
            blocker = null;
            if (!hasRuntimeSupport) return false;

            if (languageVersion < MinimumLanguageVersion)
            {
                blocker = LanguageVersionTooLow;
                return false;
            }

            if (!hasModuleInitializer)
            {
                blocker = ModuleInitializerUnavailable;
                return false;
            }

            return true;
        }

        private static string HintName(GrpcInterfaceModel model)
        {
            // the proxy type name is already a sanitized, collision-free-by-construction rendering of
            // the fully-qualified interface name, which makes it a safe hint name too
            var sb = new StringBuilder(model.ProxyTypeName.Length + 5);
            sb.Append(model.ProxyTypeName).Append(".g.cs");
            return sb.ToString();
        }

        private static bool HasAttribute(ISymbol symbol, string attributeMetadataName)
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
            private Capabilities(bool hasRuntimeSupport, bool hasModuleInitializer, LanguageVersion languageVersion)
            {
                HasRuntimeSupport = hasRuntimeSupport;
                HasModuleInitializer = hasModuleInitializer;
                LanguageVersion = languageVersion;
            }

            /// <summary>
            /// Whether a protobuf-net.Grpc new enough to consume generated proxies is referenced.
            /// </summary>
            /// <remarks>
            /// This generator ships in protobuf-net.BuildTools, which versions independently of
            /// protobuf-net.Grpc; emitting calls into an API that isn't there would turn a version
            /// mismatch into a wall of compiler errors in generated code, so instead we stand down
            /// and let the runtime path handle everything, exactly as it did before.
            /// </remarks>
            public bool HasRuntimeSupport { get; }

            /// <summary>
            /// Whether <c>[ModuleInitializer]</c> is available (net5.0+, or polyfilled in-source).
            /// </summary>
            public bool HasModuleInitializer { get; }

            public LanguageVersion LanguageVersion { get; }

            public static Capabilities From(Compilation compilation, ParseOptions parseOptions)
            {
                var hasRuntimeSupport =
                    compilation.GetTypeByMetadataName("ProtoBuf.Grpc.Internal.GeneratedProxyRegistry") is not null
                    && compilation.GetTypeByMetadataName("ProtoBuf.Grpc.Configuration.IServerMethodBinder`1") is not null
                    && compilation.GetTypeByMetadataName("Grpc.Core.ClientBase") is not null;

                var hasModuleInitializer =
                    compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") is not null;

                var languageVersion = parseOptions is CSharpParseOptions csharp
                    ? csharp.LanguageVersion
                    : LanguageVersion.Default;

                return new Capabilities(hasRuntimeSupport, hasModuleInitializer, languageVersion);
            }
        }

        private static GrpcContractCandidate? Parse(GeneratorAttributeSyntaxContext ctx, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ctx.TargetSymbol is not INamedTypeSymbol iface || iface.TypeKind != TypeKind.Interface) return null;

            // an explicit [Proxy(typeof(...))] is the user saying "use mine"; don't second-guess it
            if (HasAttribute(iface, ProxyAttributeName)) return null;

            return ParseContract(iface, cancellationToken);
        }

        private static ImmutableArray<DiagnosticInfo> One(DiagnosticDescriptor descriptor, Location? location, params string[] args)
            => ImmutableArray.Create(new DiagnosticInfo(descriptor, location, args));
    }
}
