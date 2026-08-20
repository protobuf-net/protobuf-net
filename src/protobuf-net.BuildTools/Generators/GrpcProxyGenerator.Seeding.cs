#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Grpc;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    public sealed partial class GrpcProxyGenerator
    {
        /// <summary>
        /// The payload types a <c>[ProtoGrpc]</c> declaration needs marshallers for, if that declaration
        /// names <paramref name="model"/> as its serializer model.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is what makes seeding honest: it runs the <em>same</em> <see cref="ParseContract"/> the
        /// proxy emitter runs, and takes the payload symbols out of the sink. Reimplementing "what types
        /// does this contract need" would work perfectly until the two drifted, and the failure mode is
        /// silent - the proxy calls <c>SetMarshaller&lt;T&gt;</c> for a <c>T</c> the model does not have,
        /// the build stays green, and the marshaller quietly goes reflective.
        /// </para>
        /// <para>
        /// A contract the proxy generator <em>refuses</em> contributes nothing, and that falls out for
        /// free rather than being a special case: no proxy is emitted for it, so no marshaller is asked
        /// for, so the model does not need its payloads. Diagnostics are discarded here - the proxy
        /// generator reports them, and reporting them twice from two generators would be worse than not
        /// reporting them at all.
        /// </para>
        /// </remarks>
        internal static void CollectPayloadsForModel(Compilation compilation, INamedTypeSymbol model,
            List<ITypeSymbol> payloads, CancellationToken cancellationToken)
        {
            // Free opt-out for everyone not using protobuf-net.Grpc, which is most consumers: without
            // the attribute type in the compilation there can be no [ProtoGrpc] declaration to find, so
            // the type walk below never happens. Same spirit as Utils.BuildToolsDisabled().
            if (compilation.GetTypeByMetadataName(ProtoGrpcAttributeName) is null) return;

            foreach (var candidate in EnumerateTypes(compilation.Assembly.GlobalNamespace, cancellationToken))
            {
                foreach (var attribute in candidate.GetAttributes())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (attribute.AttributeClass?.ToDisplayString() != ProtoGrpcAttributeName) continue;
                    if (!NamesModel(attribute, model)) continue;

                    foreach (var contract in GetContracts(candidate))
                    {
                        ParseContract(contract, null, cancellationToken, payloads);
                    }
                }
            }
        }

        internal const string ProtoModelAttributeName = "ProtoBuf.ProtoModelAttribute";
        private const string SerializerInterfaceName = "ProtoBuf.Serializers.ISerializer";
        private const string SerializerProxyInterfaceName = "ProtoBuf.Serializers.ISerializerProxy";

        /// <summary>
        /// The other half of seeding: if a consumer says "use this model", check that we think it is
        /// going to work.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Naming a model is a claim about another file, possibly in another assembly, and the failure it
        /// produces is the one this whole feature exists to prevent: a build that succeeds, a JIT run that
        /// succeeds, and a native publish or first call that does not. What can be checked depends
        /// entirely on where the model lives.
        /// </para>
        /// <para>
        /// <b>In this compilation</b> - it should carry <c>[ProtoModel]</c>. If it does, seeding covers
        /// the payloads and there is nothing more to say.
        /// </para>
        /// <para>
        /// <b>In a referenced assembly</b> - nothing can be added to it, so the payload set is verified
        /// against what the model can actually serialize. A model with no <c>[ProtoModel]</c> is left
        /// alone: a hand-written <c>TypeModel</c> cannot be inspected this way and is not ours to judge.
        /// </para>
        /// </remarks>
        private static void CheckModelLink(INamedTypeSymbol declaration, INamedTypeSymbol model,
            List<ITypeSymbol> payloads, ImmutableArray<DiagnosticInfo>.Builder diagnostics)
        {
            var isProtoModel = HasAttribute(model, ProtoModelAttributeName);

            if (!model.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            {
                if (!isProtoModel)
                {
                    diagnostics.Add(new DiagnosticInfo(GrpcDiagnosticKind.ModelIsNotAProtoModel,
                        Where(declaration), declaration.Name, model.ToDisplayString()));
                }
                return; // seeding handles the payloads from here
            }

            if (!isProtoModel) return; // a hand-written TypeModel; nothing we can usefully say

            var serializable = GetSerializableTypes(model);
            var reported = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var payload in payloads)
            {
                var name = Display(payload);
                if (serializable.Contains(name) || !reported.Add(name)) continue;

                diagnostics.Add(new DiagnosticInfo(GrpcDiagnosticKind.ModelCannotSerializePayload,
                    Where(declaration), declaration.Name, payload.ToDisplayString(), model.ToDisplayString()));
            }
        }

        /// <summary>
        /// Every type a generated model has a serializer for, read off the emitted interfaces.
        /// </summary>
        /// <remarks>
        /// The serializers live on a nested <c>private sealed class ProtoBufGeneratedServices</c>, and
        /// Roslyn sees private nested types through metadata - so this works across an assembly boundary,
        /// which is the only reason the check is possible at all. Matched by <em>interface</em> rather
        /// than by that type's name, which is a private implementation detail and not a contract.
        /// <c>ISerializerProxy&lt;T&gt;</c> counts too: it is what enums and hand-written serializers get
        /// instead of a body.
        /// </remarks>
        private static HashSet<string> GetSerializableTypes(INamedTypeSymbol model)
        {
            var found = new HashSet<string>(System.StringComparer.Ordinal);
            Collect(model);
            foreach (var nested in model.GetTypeMembers()) Collect(nested);
            return found;

            void Collect(INamedTypeSymbol type)
            {
                foreach (var iface in type.AllInterfaces)
                {
                    if (iface.TypeArguments.Length != 1) continue;
                    var name = iface.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (name.StartsWith("global::", System.StringComparison.Ordinal)) name = name.Substring(8);
                    var open = name.IndexOf('<');
                    if (open > 0) name = name.Substring(0, open);

                    if (name is SerializerInterfaceName or SerializerProxyInterfaceName)
                    {
                        found.Add(Display(iface.TypeArguments[0]));
                    }
                }
            }
        }

        /// <summary>Whether a <c>[ProtoGrpc]</c> attribute names this model.</summary>
        private static bool NamesModel(AttributeData attribute, INamedTypeSymbol model)
        {
            foreach (var named in attribute.NamedArguments)
            {
                if (named.Key == "Model" && named.Value.Value is INamedTypeSymbol declared)
                {
                    return SymbolEqualityComparer.Default.Equals(declared, model);
                }
            }
            return false;
        }

        /// <summary>The contract interfaces named by <c>[ProtoService]</c> on a declaration.</summary>
        private static IEnumerable<INamedTypeSymbol> GetContracts(INamedTypeSymbol declaration)
        {
            foreach (var attribute in declaration.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoServiceAttributeName) continue;
                if (attribute.ConstructorArguments.Length == 0) continue;
                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol contract
                    && contract.TypeKind != TypeKind.Error)
                {
                    yield return contract;
                }
            }
        }

        /// <summary>
        /// Every type declared in this assembly, nested types included.
        /// </summary>
        /// <remarks>
        /// Only <em>this</em> assembly: a <c>[ProtoGrpc]</c> in a referenced one names its own model, and
        /// has nothing to say about this one. That also keeps the walk bounded by the project rather than
        /// by its dependency graph.
        /// </remarks>
        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceOrTypeSymbol root,
            CancellationToken cancellationToken)
        {
            foreach (var member in root.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (member)
                {
                    case INamespaceSymbol ns:
                        foreach (var nested in EnumerateTypes(ns, cancellationToken)) yield return nested;
                        break;
                    case INamedTypeSymbol type:
                        yield return type;
                        foreach (var nested in EnumerateTypes(type, cancellationToken)) yield return nested;
                        break;
                }
            }
        }
    }
}
