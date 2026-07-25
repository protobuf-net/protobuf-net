#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace ProtoBuf.BuildTools.Generators
{
    /// <summary>
    /// Generates compile-time serializers for the contracts reachable from a user-declared
    /// <c>[ProtoModel]</c> partial <see cref="ProtoBuf.Meta.TypeModel"/> subclass.
    /// </summary>
    /// <remarks>
    /// The model is closed: it describes exactly what is visible at compile-time, and never
    /// consults the runtime (ref-emit) model. Contracts that cannot be handled are reported
    /// as diagnostics and omitted, in which case the inherited <c>TypeModel</c> behaviour
    /// (a "no serializer for type" throw) applies if they are used.
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed partial class ProtoModelGenerator : IIncrementalGenerator
    {
        internal const string ProtoModelAttributeName = "ProtoBuf.ProtoModelAttribute";
        internal const string ProtoSerializableAttributeName = "ProtoBuf.ProtoSerializableAttribute";

        /// <summary>
        /// The lowest C# version this generator emits for.
        /// </summary>
        /// <remarks>
        /// Supporting multiple language versions means multiplying every emitted construct by the
        /// size of the matrix, for no benefit to anyone actually doing AOT; a single enforced floor
        /// with a clear diagnostic is cheaper for everyone. Note that netstandard2.0/net4x projects
        /// default to C# 7.3, so those consumers must set LangVersion explicitly.
        /// </remarks>
        /// <remarks>
        /// Spelled numerically because this analyzer compiles against Roslyn 4.3.1, which predates
        /// <c>LanguageVersion.CSharp12</c>. The numeric values are stable, and at runtime we bind to
        /// whatever Roslyn the host supplies - which, for anyone actually on C# 12, is 4.8+.
        /// </remarks>
        internal const LanguageVersion MinimumLanguageVersion = (LanguageVersion)1200; // C# 12.0

        internal const string MinimumLanguageVersionDisplay = "12.0";

        /// <summary>
        /// Names the model-building step, so tests can assert that its results are actually cached
        /// between runs - caching failures are otherwise silent.
        /// </summary>
        internal const string ModelTrackingName = "ProtoModelPlans";

        /// <summary>
        /// Names the diagnostic-projection step; separate from the model so each can be asserted on.
        /// </summary>
        internal const string DiagnosticTrackingName = "ProtoModelDiagnostics";

        internal static readonly DiagnosticDescriptor LanguageVersionTooLow = new(
            id: "PBN2000",
            title: "Language version too low",
            messageFormat: "The protobuf-net AOT generator requires C# {0} or later, but this project uses C# {1}; set <LangVersion> to at least {0}.",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            // the trigger attributes are generator-owned rather than living in protobuf-net.Core, so that
            // the shape can move without pinning a runtime-package version; promote them once they settle
            context.RegisterPostInitializationOutput(static ctx
                => ctx.AddSource("ProtoModelAttributes.g.cs", SourceText.From(AttributeSource, Encoding.UTF8)));

            var parsed = context.SyntaxProvider.ForAttributeWithMetadataName(
                ProtoModelAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax cls
                    && cls.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: static (ctx, cancellationToken) => Parse(ctx, cancellationToken));

            // split the plan from its diagnostics: diagnostics carry locations, which shift whenever
            // anything above them moves, whereas the plan does not - so emission stays cached across
            // edits that only move code around
            var models = parsed.Select(static (result, _) => result?.Plan).WithTrackingName(ModelTrackingName);
            var diagnostics = parsed
                .Select(static (result, _) => result?.Diagnostics ?? default)
                .WithTrackingName(DiagnosticTrackingName);

            context.RegisterSourceOutput(diagnostics, static (ctx, items) =>
            {
                foreach (var item in items) ctx.ReportDiagnostic(ToDiagnostic(item));
            });

            var languageVersion = context.ParseOptionsProvider.Select(static (options, _)
                => options is CSharpParseOptions cs
                    ? cs.LanguageVersion.MapSpecifiedToEffectiveVersion()
                    : LanguageVersion.Default);

            context.RegisterSourceOutput(models.Combine(languageVersion), static (ctx, pair) =>
            {
                var (plan, languageVersion) = pair;
                if (plan is null) return;

                if (languageVersion < MinimumLanguageVersion)
                {
                    // emit nothing: one clear diagnostic beats a pile of errors in code they didn't write
                    // TODO: report against the model declaration once the plan carries an equatable location
                    ctx.ReportDiagnostic(Diagnostic.Create(LanguageVersionTooLow, Location.None,
                        MinimumLanguageVersionDisplay, languageVersion.ToDisplayString()));
                    return;
                }

                ctx.AddSource(plan.HintName, SourceText.From(Emit(plan), Encoding.UTF8));
            });
        }

        private const string AttributeSource = """
            // <auto-generated/>
            #nullable enable
            namespace ProtoBuf
            {
                /// <summary>
                /// Marks a partial <see cref="global::ProtoBuf.Meta.TypeModel"/> subclass as a compile-time
                /// serialization model, to be populated by the protobuf-net generator.
                /// </summary>
                [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                internal sealed class ProtoModelAttribute : global::System.Attribute
                {
                }

                /// <summary>
                /// Declares a root type that the associated model can serialize; every contract reachable
                /// from a root is included in the model automatically.
                /// </summary>
                [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
                internal sealed class ProtoSerializableAttribute : global::System.Attribute
                {
                    /// <summary>
                    /// Create a new instance.
                    /// </summary>
                    public ProtoSerializableAttribute(global::System.Type type) => Type = type;

                    /// <summary>
                    /// The root type to include in the model.
                    /// </summary>
                    public global::System.Type Type { get; }
                }

                /// <summary>
                /// Declares that the associated model serializes one type by way of another, for types
                /// that cannot carry <c>[ProtoContract(Surrogate = ...)]</c> themselves — a BCL type,
                /// or anything else you do not own.
                /// </summary>
                /// <remarks>
                /// <para>
                /// This is the compile-time equivalent of <c>RuntimeTypeModel.SetSurrogate</c>. The
                /// conversion is a cast in each direction unless <see cref="Converter"/> names a type
                /// supplying static conversion methods, which is how a type with no usable operators —
                /// <c>NodaTime.Duration</c>, say — is hooked up.
                /// </para>
                /// <para>
                /// Apply it to a model to configure that model alone, or to an <b>assembly</b> to offer
                /// the pairing to every model that references it — which is how a library can ship
                /// surrogates for types it supports, without each consumer restating them. A model's
                /// own declaration wins over one it merely references.
                /// </para>
                /// </remarks>
                [global::System.AttributeUsage(
                    global::System.AttributeTargets.Class | global::System.AttributeTargets.Assembly,
                    AllowMultiple = true, Inherited = false)]
                internal sealed class ProtoSurrogateAttribute : global::System.Attribute
                {
                    /// <summary>
                    /// Create a new instance.
                    /// </summary>
                    public ProtoSurrogateAttribute(global::System.Type type, global::System.Type surrogate)
                    {
                        Type = type;
                        Surrogate = surrogate;
                    }

                    /// <summary>
                    /// The type being serialized.
                    /// </summary>
                    public global::System.Type Type { get; }

                    /// <summary>
                    /// The type that carries its wire shape.
                    /// </summary>
                    public global::System.Type Surrogate { get; }

                    /// <summary>
                    /// A type declaring the static conversion methods named by <see cref="ToSurrogate"/>
                    /// and <see cref="ToType"/>; when omitted, a cast is used in both directions.
                    /// </summary>
                    public global::System.Type? Converter { get; set; }

                    /// <summary>
                    /// The <see cref="Converter"/> method converting the type to its surrogate.
                    /// </summary>
                    public string? ToSurrogate { get; set; }

                    /// <summary>
                    /// The <see cref="Converter"/> method converting a surrogate back to the type.
                    /// </summary>
                    public string? ToType { get; set; }
                }
            }
            """;
    }
}
