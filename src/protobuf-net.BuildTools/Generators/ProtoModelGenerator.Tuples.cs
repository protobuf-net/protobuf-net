#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Aot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        private static bool HasContractFamily(INamedTypeSymbol type)
        {
            foreach (var attribute in type.GetAttributes())
            {
                switch (attribute.AttributeClass?.ToDisplayString())
                {
                    case ProtoContractAttributeName:
                    case DataContractAttributeName:
                    case XmlTypeAttributeName:
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Recognise an "auto-tuple": an immutable-ish type reconstructed through a constructor.
        /// </summary>
        /// <remarks>
        /// This mirrors <c>MetaType.ResolveTupleConstructor</c> and must not drift from it, or we
        /// would disagree with the runtime model about which constructor to call. Note that tuple
        /// mode only applies when the type carries **no** contract attribute family at all
        /// (<c>MetaType.GetContractFamily</c>); a <c>[ProtoContract]</c> on an immutable type
        /// defeats detection and yields a serializer that cannot construct anything.
        /// </remarks>
        private static ProtoContractPlan? ParseTuple(
            INamedTypeSymbol type,
            List<PlanDiagnostic> diagnostics,
            out List<INamedTypeSymbol> reachable,
            CancellationToken cancellationToken)
        {
            reachable = new List<INamedTypeSymbol>();

            var at = PlanLocation.From(type);
            var name = type.ToDisplayString();

            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsAbstract) return null;
            if (type.DeclaredAccessibility != Accessibility.Public) return null;

            // a closed constructed generic is fine - KeyValuePair<int, string> is the common case -
            // but an open one is not something we could emit for
            if (type.IsUnboundGenericType || type.TypeArguments.Any(static x => x.TypeKind == TypeKind.TypeParameter))
            {
                return null;
            }

            var constructors = type.InstanceConstructors
                .Where(static ctor => ctor.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            // "need to have an interesting constructor to bother even checking this stuff"
            if (constructors.Count == 0
                || (constructors.Count == 1 && constructors[0].Parameters.Length == 0))
            {
                return null;
            }

            // "if you smell so much like a Tuple that it is *in your name*, we'll let you past" the
            // read-only requirement - which is the only reason ValueTuple qualifies at all
            var demandReadOnly = type.Name.IndexOf("Tuple", StringComparison.OrdinalIgnoreCase) < 0;

            var candidates = new List<ISymbol>();
            foreach (var member in type.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public) continue;

                switch (member)
                {
                    case IPropertySymbol property when property.Parameters.Length == 0:
                        if (property.GetMethod is null) return null; // no use if it cannot be read
                        // a non-public setter is tolerated (this is what lets Mono's KeyValuePair
                        // through); an init-only one likewise
                        if (demandReadOnly
                            && property.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false })
                        {
                            return null;
                        }
                        candidates.Add(property);
                        break;

                    // note: no IsImplicitlyDeclared filter. A tuple's Item1/Item2 *are* implicitly
                    // declared, and excluding them left ValueTuple with no members at all; the
                    // public-accessibility test above already excludes auto-property backing fields
                    case IFieldSymbol field when !field.IsConst:
                        if (demandReadOnly && !field.IsReadOnly) return null;
                        candidates.Add(field);
                        break;
                }
            }
            if (candidates.Count == 0) return null;

            // exactly one constructor must map every parameter onto a distinct member, by name
            // (case-insensitive) and exact type; ambiguity means "not a tuple"
            IMethodSymbol? match = null;
            List<ISymbol>? ordered = null;
            foreach (var constructor in constructors)
            {
                if (constructor.Parameters.Length != candidates.Count) continue;
                if (MapConstructor(constructor, candidates) is not { } mapped) continue;
                if (match is not null) return null; // ambiguous
                match = constructor;
                ordered = mapped;
            }
            if (match is null || ordered is null) return null;

            // field numbers are 1..n in constructor-parameter order
            var members = new List<ProtoMemberPlan>();
            for (int i = 0; i < ordered.Count; i++)
            {
                var member = ordered[i];
                var memberType = member is IPropertySymbol property ? property.Type : ((IFieldSymbol)member).Type;
                if (GetMemberShape(memberType) is not { } shape)
                {
                    return Member(diagnostics, PlanLocation.From(member), name, member.Name,
                        $"has unsupported type '{memberType.ToDisplayString()}'");
                }
                if (shape.Message is not null) reachable.Add(shape.Message);

                members.Add(new ProtoMemberPlan(i + 1, member.Name, shape.Kind,
                    shape.Message?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    isNullable: shape.IsNullable,
                    enumTypeName: shape.EnumType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    messageIsValueType: shape.Message?.IsValueType ?? false,
                    declaredTypeName: memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }

            _ = at;
            return new ProtoContractPlan(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                new(members.ToArray()), type.IsValueType, skipConstructor: false, isTuple: true,
                isTupleLiteral: type.IsTupleType);
        }

        /// <summary>
        /// Order the members to match the constructor's parameters, or null if they do not all map.
        /// </summary>
        private static List<ISymbol>? MapConstructor(IMethodSymbol constructor, List<ISymbol> candidates)
        {
            var ordered = new List<ISymbol>(constructor.Parameters.Length);
            foreach (var parameter in constructor.Parameters)
            {
                ISymbol? found = null;
                foreach (var candidate in candidates)
                {
                    if (!string.Equals(parameter.Name, candidate.Name, StringComparison.OrdinalIgnoreCase)) continue;

                    var candidateType = candidate is IPropertySymbol property
                        ? property.Type : ((IFieldSymbol)candidate).Type;
                    if (!SymbolEqualityComparer.Default.Equals(candidateType, parameter.Type)) continue;

                    found = candidate;
                    break;
                }
                if (found is null) return null;
                ordered.Add(found);
            }
            return ordered;
        }
    }
}
