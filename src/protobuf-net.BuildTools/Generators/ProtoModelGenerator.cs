#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal;
using System;
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
        internal const string ProtoSchemaAttributeName = "ProtoBuf.ProtoSchemaAttribute";

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
            // the trigger attributes are real API in protobuf-net.Core, marked [Experimental]; they
            // were generator-owned while the shape was moving, but [ProtoSurrogate] has to cross
            // assembly boundaries so a library can offer surrogates to its consumers, and a generated
            // model already references Core for TypeModel anyway
            var parsed = context.SyntaxProvider.ForAttributeWithMetadataName(
                ProtoModelAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax cls
                    && cls.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: static (ctx, cancellationToken) => Parse(ctx, cancellationToken));

            // the schemas a [ProtoSchema] can name. This is a SEPARATE input from the syntax parse
            // because a generator cannot see another generator's output: the DTOs do not exist while
            // this runs, so the model is derived from the same schema they are, and the compiler
            // joins the two afterwards (docs/aot-schema-model.md). Costs nothing in a project with
            // no .proto files, where the collection is a stable empty array.
            var schemas = context.AdditionalTextsProvider
                .Where(static text => text.Path.EndsWith(".proto", StringComparison.OrdinalIgnoreCase))
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select(static (pair, cancellationToken) =>
                {
                    var (text, options) = pair;
                    // the same per-file switch the DTO generator honours: a schema excluded from
                    // output produces no DTOs, so a model over it would name types that do not
                    // exist. Carried on the value rather than filtered here, since a schema named
                    // EXPLICITLY deserves a diagnostic rather than silence
                    var includeInOutput = true;
                    if (options.GetOptions(text).TryGetValue(
                            Literals.AdditionalFileMetadataPrefix + "IncludeInOutput", out var raw)
                        && bool.TryParse(raw, out var parsed))
                    {
                        includeInOutput = parsed;
                    }
                    return new SchemaText(text.Path, text.GetText(cancellationToken)?.ToString() ?? "",
                        includeInOutput);
                })
                .Collect();

            var resolved = parsed.Combine(schemas)
                .Select(static (pair, cancellationToken) => AddSchemas(pair.Left, pair.Right, cancellationToken));

            // split the plan from its diagnostics: diagnostics carry locations, which shift whenever
            // anything above them moves, whereas the plan does not - so emission stays cached across
            // edits that only move code around
            var models = resolved.Select(static (result, _) => result?.Plan).WithTrackingName(ModelTrackingName);
            var diagnostics = resolved
                .Select(static (result, _) => result?.Diagnostics ?? default)
                .WithTrackingName(DiagnosticTrackingName);

            // the opt-out. Note the parse above is already near-free when unwanted -
            // ForAttributeWithMetadataName only fires for a type carrying [ProtoModel] - so this is
            // about honouring the switch completely rather than about cost
            var disabled = context.AnalyzerConfigOptionsProvider.Select(static (options, _)
                => options.BuildToolsDisabled());

            context.RegisterSourceOutput(diagnostics.Combine(disabled), static (ctx, pair) =>
            {
                if (pair.Right) return;
                foreach (var item in pair.Left) ctx.ReportDiagnostic(ToDiagnostic(item));
            });

            var languageVersion = context.ParseOptionsProvider.Select(static (options, _)
                => options is CSharpParseOptions cs
                    ? cs.LanguageVersion.MapSpecifiedToEffectiveVersion()
                    : LanguageVersion.Default);

            context.RegisterSourceOutput(models.Combine(languageVersion).Combine(disabled), static (ctx, outer) =>
            {
                if (outer.Right) return;
                var (plan, languageVersion) = outer.Left;
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

        // note all three trigger attributes are real API in protobuf-net.Core, which every consumer
        // of this generator already references; [ProtoSurrogate] is the one that demanded it, since
        // it has to cross assembly boundaries so a library can offer surrogates to its consumers
    }
}
