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
            // notes/aot/findings.md), but a hard number in a diagnostic ages badly and varies by
            // workload, so the message says "several times" and the docs carry the table
            messageFormat: "This project has protobuf-net contracts and no [ProtoModel]. Compile-time "
                + "serializers are not only for AOT: they skip the metadata inspection and IL emission "
                + "the runtime model does on first use of each contract, which is typically several "
                + "times faster to first serialize. See https://docs.protobuf-net.dev/aot",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ExtensionNeedsModel = new(
            id: "PBN3014",
            title: "Extension accessor needs a model for this value type",
            messageFormat: "'{0}' reads or writes {1} extension value, which needs a model to "
                + "resolve a serializer; without one it goes through the default runtime model and "
                + "throws \"no serializer could be resolved\". Pass {2}, and make sure '{3}' is "
                + "reachable from it - an extension value is invisible to the generator, so a type "
                + "used only here needs its own [ProtoSerializable].",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Warning, // escalated to Error where AOT is asked for
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(UsesRuntimeModel, UnresolvableContractType, NoModelUnderAot, NoModel,
                ExtensionNeedsModel);

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
            var asked = context.Options.AnalyzerConfigOptionsProvider.AsksForAot();

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

            // gap B49: a `.proto` extension accessor, which has an arbitrary name and so cannot be
            // matched by the Interesting set below. Checked first, and it returns whether or not it
            // reported - either way the call is not one of the runtime-model APIs.
            if (IsExtensionAccessor(method, out var valueType, out var modelParameter))
            {
                InspectExtensionAccessor(context, models, operation, method, valueType, modelParameter);
                return;
            }

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
        /// Recognises a `.proto` extension accessor by SHAPE rather than by name: a static method on
        /// a static class which either takes a <c>TypeModel</c> as a trailing optional parameter, or
        /// has a sibling overload that does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Shape rather than name because protogen lets the class be renamed
        /// (<c>ExtensionTypeName</c>) and the accessor names come from the field names — there is
        /// nothing stable to match on. The <c>TypeModel</c>-overload pair is the actual signal, and
        /// it is what the generator emits precisely so that a model CAN be passed.
        /// </para>
        /// <para>
        /// <paramref name="valueType"/> is what the extension carries: the <c>value</c> parameter
        /// for a setter, the return type for a getter, unwrapped through
        /// <c>IEnumerable&lt;T&gt;</c> for a repeated one.
        /// </para>
        /// </remarks>
        private static bool IsExtensionAccessor(IMethodSymbol method, out ITypeSymbol? valueType, out IParameterSymbol? modelParameter)
        {
            valueType = null;
            modelParameter = null;
            if (!method.IsStatic) return false;

            var container = method.ContainingType;
            if (container is null || !container.IsStatic || container.TypeKind != TypeKind.Class) return false;

            // the model parameter on this method, if it has one
            foreach (var parameter in method.Parameters)
            {
                if (IsTypeModel(parameter.Type)) { modelParameter = parameter; break; }
            }

            if (modelParameter is null)
            {
                // ...or a sibling overload that has one; that is the legacy accessor, kept for
                // binary compatibility, and calling it explicitly bypasses the model entirely
                var hasModelSibling = false;
                foreach (var candidate in container.GetMembers(method.Name))
                {
                    if (candidate is IMethodSymbol sibling && !SymbolEqualityComparer.Default.Equals(sibling, method))
                    {
                        foreach (var parameter in sibling.Parameters)
                        {
                            if (IsTypeModel(parameter.Type)) { hasModelSibling = true; break; }
                        }
                    }
                    if (hasModelSibling) break;
                }
                if (!hasModelSibling) return false;
            }

            // what does it carry? a setter's `value`, or a getter's return
            if (method.ReturnsVoid)
            {
                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Name == "value") { valueType = parameter.Type; break; }
                }
            }
            else
            {
                valueType = Unwrap(method.ReturnType);
            }
            return valueType is not null;

            static ITypeSymbol Unwrap(ITypeSymbol type)
                => type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } named
                    && named.ConstructedFrom?.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"
                    ? named.TypeArguments[0] : type;
        }

        private static bool IsTypeModel(ITypeSymbol type)
            => type?.ToDisplayString() == "ProtoBuf.Meta.TypeModel";

        /// <summary>
        /// Reports where an extension value needs a model and none was passed (gap B49).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Only MESSAGE and ENUM values are reported</b>, and that was measured rather than
        /// assumed. A scalar, string, bytes or repeated-scalar extension works with no model at all:
        /// <c>ExtensibleUtil</c> tries a typed path first, which resolves an inbuilt serializer and
        /// never consults a model. An enum has no inbuilt serializer and behaves exactly like a
        /// message — a first probe suggested otherwise and was wrong, because the test model had not
        /// been seeded with the enum.
        /// </para>
        /// <para>
        /// <b>Error where the project asks for AOT</b>, warning elsewhere. The failure is not
        /// AOT-specific — it throws on an ordinary JIT run too, whenever nothing has touched
        /// <c>RuntimeTypeModel.Default</c>, which is exactly the shape of a generated-model app —
        /// but a project that has asked for AOT has no working configuration at all, so it is a
        /// defect rather than a risk.
        /// </para>
        /// </remarks>
        private static void InspectExtensionAccessor(OperationAnalysisContext context,
            ImmutableArray<INamedTypeSymbol> models, IInvocationOperation operation, IMethodSymbol method,
            ITypeSymbol? valueType, IParameterSymbol? modelParameter)
        {
            if (valueType is null) return;
            if (!NeedsModel(valueType)) return;

            // a model WAS supplied - unless the argument is defaulted, or an explicit null
            if (modelParameter is not null)
            {
                foreach (var argument in operation.Arguments)
                {
                    if (!SymbolEqualityComparer.Default.Equals(argument.Parameter, modelParameter)) continue;
                    var supplied = argument.ArgumentKind == ArgumentKind.Explicit
                        && argument.Value.ConstantValue is not { HasValue: true, Value: null };
                    if (supplied) return;
                }
            }

            // the generated model does this legitimately, and so does anything inside it
            if (context.ContainingSymbol.ContainingType is { } containing
                && models.Any(m => SymbolEqualityComparer.Default.Equals(m, containing)))
            {
                return;
            }

            // ...and so does the ACCESSOR CLASS ITSELF. protogen's legacy overload forwards to the
            // model-aware one with a literal null - `GetFooExt(obj) => GetFooExt(obj, null);` - which
            // is exactly the shape reported above, so without this every generated file warns about
            // its own body, in a place the consumer cannot fix by regenerating. Narrow deliberately:
            // a same-named sibling in the same static class, i.e. the forwarding pair and nothing else.
            if (SymbolEqualityComparer.Default.Equals(context.ContainingSymbol.ContainingType, method.ContainingType)
                && context.ContainingSymbol.Name == method.Name)
            {
                return;
            }

            var which = models.Length == 1
                ? "'" + models[0].Name + ".Instance'"
                : "a model (" + string.Join(", ", models.Select(static m => "'" + m.Name + "'")) + ")";

            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(ModelsProperty, string.Join(";", models.Select(static m
                    => m.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));

            var severity = context.Options.AnalyzerConfigOptionsProvider.AsksForAot() is null
                ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;

            context.ReportDiagnostic(Diagnostic.Create(
                ExtensionNeedsModel, operation.Syntax.GetLocation(), severity,
                additionalLocations: null, properties: properties,
                method.ContainingType.Name + "." + method.Name,
                valueType.TypeKind == TypeKind.Enum ? "an enum" : "a message",
                which,
                valueType.Name));
        }

        /// <summary>An extension value that cannot be serialized without a model: a message or an enum.</summary>
        private static bool NeedsModel(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Enum) return true;
            foreach (var attribute in type.GetAttributes())
            {
                switch (attribute.AttributeClass?.ToDisplayString())
                {
                    case ProtoContractAttribute:
                    case "System.Runtime.Serialization.DataContractAttribute":
                    case "System.Xml.Serialization.XmlTypeAttribute":
                        return true;
                }
            }
            return false;
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
