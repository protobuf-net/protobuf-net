#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.Linq;

namespace ProtoBuf.BuildTools.Analyzers
{
    /// <summary>
    /// Once a compilation declares a <c>[ProtoModel]</c>, flags the call sites that still go through
    /// the <em>runtime</em> model and so will not work under native AOT.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Turning the generator on does not move any existing code onto it: every
    /// <c>Serializer.Serialize(...)</c> goes through <c>RuntimeTypeModel.Default</c>, which builds
    /// serializers by reflection. Worse, those call sites keep working on a JIT runtime, so the
    /// failure arrives at publish time or later, a long way from the change that caused it.
    /// </para>
    /// <para>
    /// Deliberately silent when there is no <c>[ProtoModel]</c> in the compilation: the runtime model
    /// is a perfectly good way to use protobuf-net, and this has nothing to say to anyone using it.
    /// </para>
    /// <para>
    /// The two diagnostics differ in whether the contract type is *knowable*. Where it is, the fix is
    /// mechanical — name the model instead. Where the API takes an <c>object</c> or a
    /// <see cref="System.Type"/>, no analyzer can tell what will be serialized, and that is worth
    /// saying out loud rather than passing over in silence: a call site nobody can resolve statically
    /// is exactly the kind that fails only once ILC has trimmed.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AotMigrationAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor UsesRuntimeModel = new(
            id: "PBN3010",
            title: "Call uses the runtime model, not the AOT model",
            messageFormat: "'{0}' serializes through the runtime model, which reflects and so does not "
                + "work under native AOT; this project declares {1}, so call it on that instead "
                + "(for example '{2}.Instance.{3}').",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnresolvableContractType = new(
            id: "PBN3011",
            title: "Call resolves its contract type at run time",
            messageFormat: "'{0}' takes the type to serialize as a value rather than a type argument, "
                + "so neither this analyzer nor the AOT generator can tell what it serializes; under "
                + "native AOT it will use the reflection path. Use a generic overload, or a generated "
                + "model, if this type needs to work when published AOT.",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor NoModelUnderAot = new(
            id: "PBN3012",
            title: "This project publishes AOT or trimmed, but has no AOT model",
            messageFormat: "This project has protobuf-net contracts and asks for {0}, but declares no "
                + "[ProtoModel]; serializers will be built by reflection, which is exactly what will "
                + "not survive. See https://docs.protobuf-net.dev/aot",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor NoModel = new(
            id: "PBN3013",
            title: "Compile-time serializers are available",
            // qualitative deliberately: the measured figure is ~3x on an ordinary build (see
            // docs/aot-findings.md), but a hard number in a diagnostic ages badly and varies by
            // workload, so the message says "several times" and the docs carry the table
            messageFormat: "This project has protobuf-net contracts and no [ProtoModel]. Compile-time "
                + "serializers are not only for AOT: they skip the metadata inspection and IL emission "
                + "the runtime model does on first use of each contract, which is typically several "
                + "times faster to first serialize. See https://docs.protobuf-net.dev/aot",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(UsesRuntimeModel, UnresolvableContractType, NoModelUnderAot, NoModel);

        /// <summary>Diagnostic property carrying the model type names, for the fixer.</summary>
        internal const string ModelsProperty = "Models";

        private const string ProtoModelAttribute = "ProtoBuf.ProtoModelAttribute";
        private const string ProtoContractAttribute = "ProtoBuf.ProtoContractAttribute";
        private const string SerializerType = "ProtoBuf.Serializer";
        private const string RuntimeTypeModelType = "ProtoBuf.Meta.RuntimeTypeModel";

        /// <summary>
        /// The operations worth flagging: everything that puts bytes on or takes them off the wire.
        /// </summary>
        /// <remarks>
        /// <c>GetSchema</c> and friends are deliberately absent — they are a build-time/diagnostic
        /// convenience, they do not run on the serialization path, and nagging about them would make
        /// this noisy for no benefit.
        /// </remarks>
        private static readonly ImmutableHashSet<string> Interesting = ImmutableHashSet.Create(
            "Serialize", "SerializeWithLengthPrefix", "Deserialize", "DeserializeWithLengthPrefix",
            "DeserializeItems", "Merge", "DeepClone", "Measure", "ChangeType");

        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(
                GeneratedCodeAnalysisFlags.None); // the generated model legitimately does all of this

            ctx.RegisterCompilationStartAction(static compilationStart =>
            {
                // first, and before any symbol work: one property lookup is the whole cost of having
                // the tooling installed but not wanted
                if (compilationStart.Options.AnalyzerConfigOptionsProvider.BuildToolsDisabled()) return;

                var models = FindModels(compilationStart.Compilation, out var firstContract);
                if (models.IsEmpty)
                {
                    // nothing to migrate *to*. Say so once, and only where there is something to
                    // migrate: contracts but no model.
                    // Reported from a *symbol* action rather than a compilation-end one, and that is
                    // load-bearing: a compilation-end diagnostic is "non-local", and Roslyn will not
                    // offer a code fix for one however good its location is. Since the whole point of
                    // anchoring this on a type was to make AddProtoModelCodeFixProvider reachable,
                    // end-action reporting defeated the exercise.
                    //
                    // Still exactly once: the anchor was chosen deterministically above, and this
                    // fires only for that symbol.
                    if (firstContract is { } anchor)
                    {
                        compilationStart.RegisterSymbolAction(
                            ctx =>
                            {
                                if (SymbolEqualityComparer.Default.Equals(ctx.Symbol, anchor)) Announce(ctx, anchor);
                            },
                            SymbolKind.NamedType);
                    }
                    return;
                }

                compilationStart.RegisterOperationAction(
                    context => Inspect(context, models), OperationKind.Invocation);
            });
        }

        /// <summary>
        /// Reported once per compilation, for a project with contracts and no model.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two severities, because they are two different statements. A project that has asked for
        /// **AOT or trimming** and has no `[ProtoModel]` has a defect: it is going to build
        /// serializers by reflection, which is the thing that will not survive the publish. That is
        /// a warning at least — a consumer who wants it to stop the build can escalate it, and the
        /// default stays a warning so that turning `PublishAot` on does not break someone's build on
        /// the spot.
        /// </para>
        /// <para>
        /// Everyone else gets `Info`, which does not appear in normal build output at all. The
        /// argument there is **cold start**, not AOT: the runtime model inspects metadata and emits
        /// IL on first use of each contract, and that cost is real enough to time builds out. It is
        /// a genuine offer rather than an advertisement, which is why it is worth making at all —
        /// and `dotnet_diagnostic.PBN3013.severity = none` dismisses it permanently.
        /// </para>
        /// <para>
        /// Location.None deliberately: this is about the project, not about any one line of it, and
        /// a squiggle on an arbitrarily-chosen contract would be worse than none.
        /// </para>
        /// </remarks>
        private static void Announce(SymbolAnalysisContext context, INamedTypeSymbol anchor)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            string? asked = null;
            foreach (var property in new[] { "PublishAot", "PublishTrimmed", "IsAotCompatible", "IsTrimmable" })
            {
                if (options.TryGetValue("build_property." + property, out var value)
                    && string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase))
                {
                    asked = property;
                    break;
                }
            }

            // Anchored on a contract rather than at Location.None, which is where this started: a
            // code fix has to attach to a document, and an actionable lightbulb offering to write the
            // model is worth more than a message in the error list that nobody can act on. The type
            // is the ordinal-first contract, so it does not wander between builds - and the message
            // says "this project", because the anchor is where the fix is offered, not the culprit.
            var at = anchor.Locations.FirstOrDefault(static x => x.IsInSource) ?? Location.None;
            context.ReportDiagnostic(asked is null
                ? Diagnostic.Create(NoModel, at)
                : Diagnostic.Create(NoModelUnderAot, at, asked));
        }

        /// <summary>Every <c>[ProtoModel]</c> type in the compilation, by display name.</summary>
        /// <remarks>
        /// The attribute is real API in protobuf-net.Core, but it is still matched by full name
        /// rather than by symbol: the unit-test harness references Core through the BuildTools
        /// assembly and stubs the attribute to dodge its <c>[Experimental]</c> gate, so symbol
        /// identity cannot be relied on there.
        /// </remarks>
        private static ImmutableArray<INamedTypeSymbol> FindModels(Compilation compilation,
            out INamedTypeSymbol? hasContracts)
        {
            var found = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            INamedTypeSymbol? firstContract = null;
            Walk(compilation.Assembly.GlobalNamespace, found, ref firstContract);
            // deterministic, so the squiggle does not wander between builds
            hasContracts = firstContract;
            return found.ToImmutable();

            static void Walk(INamespaceOrTypeSymbol scope,
                ImmutableArray<INamedTypeSymbol>.Builder found, ref INamedTypeSymbol? firstContract)
            {
                foreach (var member in scope.GetMembers())
                {
                    switch (member)
                    {
                        case INamespaceSymbol ns:
                            Walk(ns, found, ref firstContract);
                            break;
                        case INamedTypeSymbol type:
                            foreach (var attribute in type.GetAttributes())
                            {
                                var name = attribute.AttributeClass?.ToDisplayString();
                                if (name == ProtoModelAttribute) found.Add(type);
                                else if (name == ProtoContractAttribute
                                    && (firstContract is null
                                        || string.CompareOrdinal(type.ToDisplayString(),
                                            firstContract.ToDisplayString()) < 0))
                                {
                                    firstContract = type;
                                }
                            }
                            Walk(type, found, ref firstContract);
                            break;
                    }
                }
            }
        }

        private static void Inspect(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> models)
        {
            var operation = (IInvocationOperation)context.Operation;
            var method = operation.TargetMethod;
            if (!Interesting.Contains(method.Name)) return;
            if (!IsRuntimeModel(operation, method)) return;

            // a generated model does all of this legitimately, and so does anything inside it
            if (context.ContainingSymbol.ContainingType is { } containing
                && models.Any(m => SymbolEqualityComparer.Default.Equals(m, containing)))
            {
                return;
            }

            var name = method.ContainingType.Name + "." + method.Name;
            if (method.TypeArguments.Length == 0)
            {
                // object/Type based: nothing to name, and nothing a fixer could write
                context.ReportDiagnostic(Diagnostic.Create(
                    UnresolvableContractType, operation.Syntax.GetLocation(), name));
                return;
            }

            var which = models.Length == 1
                ? "the AOT model '" + models[0].Name + "'"
                : "AOT models (" + string.Join(", ", models.Select(static m => "'" + m.Name + "'")) + ")";

            // the fixer needs the model by name, and re-deriving it there would mean repeating the
            // whole scan; a diagnostic property is the supported way to carry it across
            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(ModelsProperty, string.Join(";", models.Select(static m
                    => m.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));

            // the example names the generated static accessor - Model.Instance.Serialize - which is
            // the one form guaranteed to exist and compile; a first cut camel-cased the model name
            // into an imaginary local ('protoModel.Serialize'), which existed nowhere
            context.ReportDiagnostic(Diagnostic.Create(
                UsesRuntimeModel, operation.Syntax.GetLocation(), properties, name, which,
                models[0].Name, method.Name));
        }

        /// <summary>
        /// Whether this call goes through the runtime model: the <c>Serializer</c> facade, or
        /// <c>RuntimeTypeModel.Default</c> reached directly.
        /// </summary>
        /// <remarks>
        /// A call on some *other* <c>TypeModel</c> instance is not flagged — that may well be a
        /// generated model, and telling people off for using one correctly would be worse than
        /// saying nothing. `RuntimeTypeModel.Create()` is likewise the consumer's own choice.
        /// </remarks>
        private static bool IsRuntimeModel(IInvocationOperation operation, IMethodSymbol method)
        {
            var container = method.ContainingType?.ToDisplayString();
            if (container == SerializerType) return true;
            // Serializer.NonGeneric and the other nested helpers
            if (container is not null && container.StartsWith(SerializerType + ".", System.StringComparison.Ordinal))
            {
                return true;
            }

            // an instance call whose receiver is RuntimeTypeModel.Default
            return operation.Instance is IPropertyReferenceOperation
            {
                Property: { Name: "Default", IsStatic: true } property,
            } && property.ContainingType?.ToDisplayString() == RuntimeTypeModelType;
        }

    }
}
