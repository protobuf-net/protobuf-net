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
        private const string ProtoContractAttributeName = "ProtoBuf.ProtoContractAttribute";
        private const string ProtoMemberAttributeName = "ProtoBuf.ProtoMemberAttribute";
        private const string ProtoBufNamespace = "ProtoBuf";

        /// <summary>
        /// Project a <c>[ProtoModel]</c> declaration onto a plan, plus the diagnostics explaining
        /// anything that had to be left out.
        /// </summary>
        /// <remarks>
        /// Deliberately conservative: anything whose semantics this generator does not yet fully
        /// reproduce causes the contract to be dropped, because emitting a subtly wrong serializer
        /// is far worse than emitting none. Every drop must say why - a silently missing serializer
        /// surfaces much later as an unexplained "no serializer for type" throw.
        /// </remarks>
        private static ProtoParseResult? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.TargetSymbol is not INamedTypeSymbol model) return null;

            // TODO: nested and generic model types
            if (model.ContainingType is not null || model.IsGenericType) return null;

            var diagnostics = new List<PlanDiagnostic>();
            var parsed = new Dictionary<string, ProtoContractPlan>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<INamedTypeSymbol>();

            // locations are tracked here rather than on the plan: they move whenever anything above
            // them moves, and putting them in the plan would invalidate the cached emit step
            var locations = new Dictionary<string, PlanLocation>(StringComparer.Ordinal);

            foreach (var attribute in model.GetAttributes())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (attribute.AttributeClass?.ToDisplayString() != ProtoSerializableAttributeName) continue;
                if (attribute.ConstructorArguments.Length != 1) continue;
                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol seed) pending.Enqueue(seed);
            }

            // walk the transitive closure: a seed's members can reach further contracts, and the same
            // contract is commonly reachable by several paths (or seeded *and* reachable)
            while (pending.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var type = pending.Dequeue();
                var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!visited.Add(key)) continue;

                locations[key] = PlanLocation.From(type);

                var contract = ParseContract(type, diagnostics, out var reachable, cancellationToken);
                if (contract is not null) parsed.Add(key, contract);
                foreach (var next in reachable) pending.Enqueue(next);
            }

            DropUnsatisfiable(parsed, locations, diagnostics);

            ProtoModelPlan? plan = null;
            if (parsed.Count != 0)
            {
                var contracts = parsed.Values.OrderBy(static x => x.TypeName, StringComparer.Ordinal).ToArray();
                var nameSpace = model.ContainingNamespace is { IsGlobalNamespace: false } ns
                    ? ns.ToDisplayString() : null;
                plan = new ProtoModelPlan(nameSpace, model.Name, new(contracts));
            }

            return new ProtoParseResult(plan, new(diagnostics.ToArray()));
        }

        /// <summary>
        /// Remove any contract that references a contract we could not handle, to a fixed point.
        /// </summary>
        /// <remarks>
        /// Without this the emitted services type would call <c>ReadMessage&lt;T&gt;(..., this)</c>
        /// for a <c>T</c> it does not implement <c>ISerializer&lt;T&gt;</c> for, which would not
        /// compile. Dropping a contract can strand its referrers, hence the loop.
        /// </remarks>
        private static void DropUnsatisfiable(
            Dictionary<string, ProtoContractPlan> parsed,
            Dictionary<string, PlanLocation> locations,
            List<PlanDiagnostic> diagnostics)
        {
            bool removed;
            do
            {
                removed = false;
                foreach (var contract in parsed.Values.ToList())
                {
                    foreach (var member in contract.Members)
                    {
                        if (member.Kind != ProtoMemberKind.Message) continue;
                        if (member.TypeName is not null && parsed.ContainsKey(member.TypeName)) continue;

                        parsed.Remove(contract.TypeName);
                        locations.TryGetValue(contract.TypeName, out var location);
                        diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.OmittedCascade, location,
                            Simplify(contract.TypeName), Simplify(member.TypeName)));
                        removed = true;
                        break;
                    }
                }
            }
            while (removed);
        }

        private static ProtoContractPlan? ParseContract(
            INamedTypeSymbol type,
            List<PlanDiagnostic> diagnostics,
            out List<INamedTypeSymbol> reachable,
            CancellationToken cancellationToken)
        {
            reachable = new List<INamedTypeSymbol>();

            var at = PlanLocation.From(type);
            var name = type.ToDisplayString();

            if (type.TypeKind != TypeKind.Class) return Contract(diagnostics, at, name, "only classes are supported");
            if (type.IsAbstract) return Contract(diagnostics, at, name, "abstract types are not supported");
            if (type.IsGenericType) return Contract(diagnostics, at, name, "generic types are not supported");
            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                // full ref-emit compilation only reaches public API, and we match that for now
                return Contract(diagnostics, at, name, "the type is not public");
            }
            if (!type.InstanceConstructors.Any(static ctor
                => ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public))
            {
                return Contract(diagnostics, at, name, "there is no public parameterless constructor");
            }
            if (type.BaseType is { SpecialType: not SpecialType.System_Object })
            {
                return Contract(diagnostics, at, name, "inheritance is not supported");
            }

            var isContract = false;
            foreach (var attribute in type.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                if (attributeName == ProtoContractAttributeName)
                {
                    if (attribute.NamedArguments.Length != 0 || attribute.ConstructorArguments.Length != 0)
                    {
                        return Option(diagnostics, at, name, "[ProtoContract] with explicit options");
                    }
                    isContract = true;
                }
                else if (IsProtoBufAttribute(attribute))
                {
                    return Option(diagnostics, at, name, $"[{attribute.AttributeClass?.Name}]");
                }
            }
            if (!isContract) return Contract(diagnostics, at, name, "the type is not marked [ProtoContract]");

            var members = new List<ProtoMemberPlan>();
            foreach (var symbol in type.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (symbol is IFieldSymbol field && !field.IsImplicitlyDeclared
                    && field.GetAttributes().Any(IsProtoBufAttribute))
                {
                    return Option(diagnostics, PlanLocation.From(field), name, "[ProtoMember] on fields");
                }

                if (symbol is not IPropertySymbol property) continue;

                var atMember = PlanLocation.From(property);
                int? fieldNumber = null;
                foreach (var attribute in property.GetAttributes())
                {
                    var attributeName = attribute.AttributeClass?.ToDisplayString();
                    if (attributeName == ProtoMemberAttributeName)
                    {
                        // DataFormat, IsRequired, AsReference, ... all change the wire format
                        if (attribute.NamedArguments.Length != 0)
                        {
                            return Option(diagnostics, atMember, name, "[ProtoMember] with named arguments");
                        }
                        if (attribute.ConstructorArguments.Length != 1
                            || attribute.ConstructorArguments[0].Value is not int number)
                        {
                            return Option(diagnostics, atMember, name, "this form of [ProtoMember]");
                        }
                        fieldNumber = number;
                    }
                    else if (IsProtoBufAttribute(attribute))
                    {
                        return Option(diagnostics, atMember, name, $"[{attribute.AttributeClass?.Name}]");
                    }
                }
                if (fieldNumber is null) continue;

                if (property.IsStatic) return Member(diagnostics, atMember, name, property.Name, "is static");
                if (property.DeclaredAccessibility != Accessibility.Public)
                {
                    return Member(diagnostics, atMember, name, property.Name, "is not public");
                }
                if (property.GetMethod is not { DeclaredAccessibility: Accessibility.Public })
                {
                    return Member(diagnostics, atMember, name, property.Name, "has no public getter");
                }
                if (property.SetMethod is not { DeclaredAccessibility: Accessibility.Public })
                {
                    return Member(diagnostics, atMember, name, property.Name, "has no public setter");
                }
                if (property.SetMethod.IsInitOnly)
                {
                    return Member(diagnostics, atMember, name, property.Name, "has an init-only setter");
                }

                var kind = GetMemberKind(property.Type, out var message);
                if (kind is null)
                {
                    return Member(diagnostics, atMember, name, property.Name,
                        $"has unsupported type '{property.Type.ToDisplayString()}'");
                }

                if (kind == ProtoMemberKind.Message)
                {
                    // enqueued even if it turns out to be unsupported, so that it reports its own
                    // reason and this contract is dropped by cascade with a message that chains
                    reachable.Add(message!);
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, property.Name, kind.Value,
                        message!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                }
                else
                {
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, property.Name, kind.Value));
                }
            }

            if (members.Count == 0)
            {
                return Contract(diagnostics, at, name, "no [ProtoMember] properties were found");
            }
            members.Sort(static (x, y) => x.FieldNumber.CompareTo(y.FieldNumber));

            return new ProtoContractPlan(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                new(members.ToArray()));
        }

        private static ProtoContractPlan? Contract(List<PlanDiagnostic> diagnostics, PlanLocation at, string type, string reason)
        {
            diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.UnsupportedContract, at, type, reason));
            return null;
        }

        private static ProtoContractPlan? Option(List<PlanDiagnostic> diagnostics, PlanLocation at, string type, string what)
        {
            diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.UnsupportedOption, at, type, what));
            return null;
        }

        private static ProtoContractPlan? Member(List<PlanDiagnostic> diagnostics, PlanLocation at, string type, string member, string reason)
        {
            diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.UnsupportedMember, at, type, member, reason));
            return null;
        }

        /// <summary>Strip the <c>global::</c> prefix, for use in messages.</summary>
        private static string Simplify(string? typeName)
            => typeName is null ? "?"
            : typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName.Substring("global::".Length)
            : typeName;

        private static bool IsProtoBufAttribute(AttributeData attribute)
            => attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == ProtoBufNamespace;

        private static ProtoMemberKind? GetMemberKind(ITypeSymbol type, out INamedTypeSymbol? message)
        {
            message = null;
            switch (type.SpecialType)
            {
                case SpecialType.System_Int32: return ProtoMemberKind.Int32;
                case SpecialType.System_String: return ProtoMemberKind.String;
            }

            // anything marked [ProtoContract] counts as reachable, supported or not; whether it can
            // actually be handled is decided when the closure gets to it
            if (type is INamedTypeSymbol named && named.GetAttributes().Any(static a
                    => a.AttributeClass?.ToDisplayString() == ProtoContractAttributeName))
            {
                message = named;
                return ProtoMemberKind.Message;
            }
            return null;
        }
    }
}
