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
    /// The trigger attributes are generator-owned for now (emitted per consuming assembly as
    /// <c>internal</c> from <c>RegisterPostInitializationOutput</c>), which is how <c>[ProtoModel]</c>
    /// started. They must be post-init rather than ordinary output because
    /// <c>ForAttributeWithMetadataName</c> can only see a generator's own post-init sources. When
    /// they move into protobuf-net.Grpc proper, drop the emission: an <c>internal</c> copy in the
    /// consumer's own assembly wins name resolution over a <c>public</c> one in a referenced
    /// assembly, so the transition is not a breaking change.
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
            // The trigger attributes have to exist before anything can be marked with them. Note this
            // runs unconditionally - post-init has no access to the compilation or to analyzer config,
            // so ProtoBufDisableBuildTools cannot be honoured here. Emitting two small internal
            // attributes is the entire cost, and every step that does real work checks the switch.
            context.RegisterPostInitializationOutput(static ctx
                => ctx.AddSource("ProtoBuf.Grpc.TriggerAttributes.g.cs", TriggerAttributesSource));

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

        /// <summary>
        /// The generator-owned trigger attributes, one <c>internal</c> copy per consuming assembly.
        /// </summary>
        private const string TriggerAttributesSource = @"// <auto-generated/>
#nullable enable
namespace ProtoBuf.Grpc.Configuration
{
    /// <summary>
    /// Marks a partial class deriving from <c>ClientFactory</c> as the compile-time home for gRPC
    /// client proxies and server bindings.
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal sealed class ProtoGrpcAttribute : global::System.Attribute
    {
        /// <summary>
        /// The <c>[ProtoModel]</c>-generated <c>TypeModel</c> to marshal payloads through. Without
        /// this the payloads go through <c>RuntimeTypeModel.Default</c>, which reflects.
        /// </summary>
        public global::System.Type? Model { get; set; }

        /// <summary>
        /// The name of the generated <c>IServiceCollection</c> extension method; defaults to
        /// <c>Add</c> plus the declaring type's name.
        /// </summary>
        public string? RegistrationMethodName { get; set; }
    }

    /// <summary>
    /// Names a service contract to generate for, and optionally the implementation to bind on the
    /// server. Repeat for each contract.
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal sealed class ProtoServiceAttribute : global::System.Attribute
    {
        public ProtoServiceAttribute(global::System.Type contract) => Contract = contract;

        public ProtoServiceAttribute(global::System.Type contract, global::System.Type implementation)
        {
            Contract = contract;
            Implementation = implementation;
        }

        /// <summary>The service contract interface.</summary>
        public global::System.Type Contract { get; }

        /// <summary>
        /// The implementation to bind on the server, if this project hosts the service. Naming it is
        /// what lets the server bindings close their generics at compile time.
        /// </summary>
        public global::System.Type? Implementation { get; }
    }
}
";
    }
}
