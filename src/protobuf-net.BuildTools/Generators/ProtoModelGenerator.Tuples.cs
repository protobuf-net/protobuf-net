#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Aot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        /// <summary>
        /// Rewrite a tuple type without its element names, recursively.
        /// </summary>
        /// <remarks>
        /// Element names are identity-convertible decoration, not part of the type, and erasing them
        /// fixes three things at once. Detection: a *named* tuple reports four public fields
        /// (<c>Item1, Id, Item2, Name</c>), which fails the constructor-arity match, whereas the
        /// erased form reports the two we want. De-duplication: <c>SymbolEqualityComparer.Default</c>
        /// and <c>ToDisplayString</c> both distinguish the two spellings, so the same shape named two
        /// ways would emit <c>ISerializer&lt;(int, string)&gt;</c> twice and fail to compile.
        /// Emission: the erased spelling is what ref-emit produces, so we stay aligned with it.
        /// Consumers may still *write* names - assignment between the two is an identity conversion.
        /// </remarks>
        private static ITypeSymbol EraseTupleNames(Compilation compilation, ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named) return type;

            if (named.IsTupleType)
            {
                var elements = named.TupleElements;
                var erased = new ITypeSymbol[elements.Length];
                for (int i = 0; i < elements.Length; i++)
                {
                    erased[i] = EraseTupleNames(compilation, elements[i].Type); // nested tuples too
                }
                return compilation.CreateTupleTypeSymbol(ImmutableArray.Create(erased));
            }

            // a tuple can also hide inside another generic, e.g. KeyValuePair<int, (int A, int B)>
            if (named.IsGenericType && !named.TypeArguments.IsDefaultOrEmpty)
            {
                var arguments = named.TypeArguments;
                ITypeSymbol[]? rewritten = null;
                for (int i = 0; i < arguments.Length; i++)
                {
                    var erased = EraseTupleNames(compilation, arguments[i]);
                    if (SymbolEqualityComparer.Default.Equals(erased, arguments[i])) continue;

                    rewritten ??= arguments.ToArray();
                    rewritten[i] = erased;
                }
                if (rewritten is not null) return named.ConstructedFrom.Construct(rewritten);
            }
            return type;
        }

        /// <summary>
        /// Would this type be handled as an auto-tuple? Used to decide whether a *member* of this
        /// type should enter the closure as a message.
        /// </summary>
        private static bool IsTupleCandidate(ITypeSymbol type)
            => type is INamedTypeSymbol named && !HasContractFamily(named)
                && TryResolveTuple(named, out _, out _);

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
        /// <summary>
        /// The qualification half, with no diagnostics, so it can also answer "is this a tuple?" when
        /// deciding whether a member's type should enter the closure.
        /// </summary>
        private static bool TryResolveTuple(
            INamedTypeSymbol type,
            out List<ISymbol> ordered,
            out IMethodSymbol? constructor)
        {
            ordered = new List<ISymbol>();
            constructor = null;

            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsAbstract) return false;
            if (type.DeclaredAccessibility != Accessibility.Public) return false;

            // a closed constructed generic is fine - KeyValuePair<int, string> is the common case -
            // but an open one is not something we could emit for
            if (type.IsUnboundGenericType || type.TypeArguments.Any(static x => x.TypeKind == TypeKind.TypeParameter))
            {
                return false;
            }

            var constructors = type.InstanceConstructors
                .Where(static ctor => ctor.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            // "need to have an interesting constructor to bother even checking this stuff"
            if (constructors.Count == 0
                || (constructors.Count == 1 && constructors[0].Parameters.Length == 0))
            {
                return false;
            }

            // "if you smell so much like a Tuple that it is *in your name*, we'll let you past" the
            // read-only requirement - which is the only reason ValueTuple qualifies at all
            var demandReadOnly = type.Name.IndexOf("Tuple", StringComparison.OrdinalIgnoreCase) < 0;

            var candidates = new List<ISymbol>();
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public) continue;

                switch (member)
                {
                    case IPropertySymbol property when property.Parameters.Length == 0:
                        if (property.GetMethod is null) return false; // no use if it cannot be read
                        // a non-public setter is tolerated (this is what lets Mono's KeyValuePair
                        // through); an init-only one likewise
                        if (demandReadOnly
                            && property.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false })
                        {
                            return false;
                        }
                        candidates.Add(property);
                        break;

                    // note: no IsImplicitlyDeclared filter. A tuple's Item1/Item2 *are* implicitly
                    // declared, and excluding them left ValueTuple with no members at all; the
                    // public-accessibility test above already excludes auto-property backing fields
                    case IFieldSymbol field when !field.IsConst:
                        if (demandReadOnly && !field.IsReadOnly) return false;
                        candidates.Add(field);
                        break;
                }
            }
            if (candidates.Count == 0) return false;

            // exactly one constructor must map every parameter onto a distinct member, by name
            // (case-insensitive) and exact type; ambiguity means "not a tuple"
            foreach (var candidate in constructors)
            {
                if (candidate.Parameters.Length != candidates.Count) continue;
                if (MapConstructor(candidate, candidates) is not { } mapped) continue;
                if (constructor is not null) return false; // ambiguous
                constructor = candidate;
                ordered = mapped;
            }
            return constructor is not null;
        }

        private static ProtoContractPlan? ParseTuple(
            Compilation compilation,
            INamedTypeSymbol type,
            List<PlanDiagnostic> diagnostics,
            out List<INamedTypeSymbol> reachable,
            CancellationToken cancellationToken)
        {
            reachable = new List<INamedTypeSymbol>();
            cancellationToken.ThrowIfCancellationRequested();

            var at = PlanLocation.From(type);
            var name = type.ToDisplayString();

            if (!TryResolveTuple(type, out var ordered, out _)) return null;

            // field numbers are 1..n in constructor-parameter order
            var members = new List<ProtoMemberPlan>();
            for (int i = 0; i < ordered.Count; i++)
            {
                var member = ordered[i];
                var memberType = member is IPropertySymbol property ? property.Type : ((IFieldSymbol)member).Type;
                if (GetMemberShape(compilation, memberType) is not { } shape)
                {
                    return Member(diagnostics, PlanLocation.From(member), name, member.Name,
                        $"has unsupported type '{memberType.ToDisplayString()}'");
                }
                if (shape.Message is not null) reachable.Add(shape.Message);

                members.Add(new ProtoMemberPlan(i + 1, member.Name, shape.Kind,
                    (shape.Message is null ? null : Qualified(compilation, shape.Message)),
                    isNullable: shape.IsNullable,
                    enumTypeName: (shape.EnumType is null ? null : Qualified(compilation, shape.EnumType)),
                    memberIsValueType: shape.Message?.IsValueType ?? false,
                    declaredTypeName: Qualified(compilation, memberType)));
            }

            _ = at;
            return new ProtoContractPlan(
                Qualified(compilation, type),
                new(members.ToArray()), type.IsValueType, skipConstructor: false, isTuple: true,
                isTupleLiteral: type.IsTupleType,
                // no surrogate here, so the declared answer is simply the type's own
                declaredIsValueType: type.IsValueType);
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
