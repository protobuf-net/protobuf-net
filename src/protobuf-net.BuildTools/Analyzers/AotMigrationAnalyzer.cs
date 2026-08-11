#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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
            id: "PBN2010",
            title: "Call uses the runtime model, not the AOT model",
            messageFormat: "'{0}' serializes through the runtime model, which reflects and so does not "
                + "work under native AOT; this project declares {1}, so call it on that instead "
                + "(for example '{2}.{3}').",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnresolvableContractType = new(
            id: "PBN2011",
            title: "Call resolves its contract type at run time",
            messageFormat: "'{0}' takes the type to serialize as a value rather than a type argument, "
                + "so neither this analyzer nor the AOT generator can tell what it serializes; under "
                + "native AOT it will use the reflection path. Use a generic overload, or a generated "
                + "model, if this type needs to work when published AOT.",
            category: "ProtoBuf",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(UsesRuntimeModel, UnresolvableContractType);

        private const string ProtoModelAttribute = "ProtoBuf.ProtoModelAttribute";
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
                var models = FindModels(compilationStart.Compilation);
                if (models.IsEmpty) return; // not an AOT project; nothing to say

                compilationStart.RegisterOperationAction(
                    context => Inspect(context, models), OperationKind.Invocation);
            });
        }

        /// <summary>Every <c>[ProtoModel]</c> type in the compilation, by display name.</summary>
        /// <remarks>
        /// The attribute is generator-owned — emitted from <c>RegisterPostInitializationOutput</c> —
        /// so it is matched by full name rather than by symbol, exactly as the surrogate hand-off is;
        /// each assembly compiles its own copy. Post-init sources are part of the compilation the
        /// analyzer sees, so this works without the generator having to run first.
        /// </remarks>
        private static ImmutableArray<INamedTypeSymbol> FindModels(Compilation compilation)
        {
            var found = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            Walk(compilation.Assembly.GlobalNamespace, found);
            return found.ToImmutable();

            static void Walk(INamespaceOrTypeSymbol scope, ImmutableArray<INamedTypeSymbol>.Builder found)
            {
                foreach (var member in scope.GetMembers())
                {
                    switch (member)
                    {
                        case INamespaceSymbol ns:
                            Walk(ns, found);
                            break;
                        case INamedTypeSymbol type:
                            if (type.GetAttributes().Any(static a
                                => a.AttributeClass?.ToDisplayString() == ProtoModelAttribute))
                            {
                                found.Add(type);
                            }
                            Walk(type, found);
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
            context.ReportDiagnostic(Diagnostic.Create(
                UsesRuntimeModel, operation.Syntax.GetLocation(), name, which,
                models[0].Name.Length == 0 ? "model" : ToCamel(models[0].Name), method.Name));
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

        private static string ToCamel(string name)
            => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
