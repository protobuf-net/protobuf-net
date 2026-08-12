#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal.Aot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        private const string ProtoContractAttributeName = "ProtoBuf.ProtoContractAttribute";
        private const string ProtoIncludeAttributeName = "ProtoBuf.ProtoIncludeAttribute";
        private const string ProtoReservedAttributeName = "ProtoBuf.ProtoReservedAttribute";
        private const string ProtoPartialIgnoreAttributeName = "ProtoBuf.ProtoPartialIgnoreAttribute";
        private const string ProtoPartialMemberAttributeName = "ProtoBuf.ProtoPartialMemberAttribute";
        private const string ProtoSurrogateAttributeName = "ProtoBuf.ProtoSurrogateAttribute";
        private const string ProtoMapAttributeName = "ProtoBuf.ProtoMapAttribute";
        private const string NullWrappedValueAttributeName = "ProtoBuf.NullWrappedValueAttribute";
        private const string NullWrappedCollectionAttributeName = "ProtoBuf.NullWrappedCollectionAttribute";
        private const string ProtoIgnoreAttributeName = "ProtoBuf.ProtoIgnoreAttribute";
        private const string CompatibilityLevelAttributeName = "ProtoBuf.CompatibilityLevelAttribute";
        private const string ExtensibleTypeName = "ProtoBuf.Extensible";
        private const string ExtensibleInterfaceName = "ProtoBuf.IExtensible";
        private const string TypedExtensibleInterfaceName = "ProtoBuf.ITypedExtensible";
        private const string ProtoMemberAttributeName = "ProtoBuf.ProtoMemberAttribute";
        private const string DefaultValueAttributeName = "System.ComponentModel.DefaultValueAttribute";
        private const string DataContractAttributeName = "System.Runtime.Serialization.DataContractAttribute";
        private const string DataMemberAttributeName = "System.Runtime.Serialization.DataMemberAttribute";
        private const string XmlTypeAttributeName = "System.Xml.Serialization.XmlTypeAttribute";
        private const string XmlElementAttributeName = "System.Xml.Serialization.XmlElementAttribute";
        private const string XmlArrayAttributeName = "System.Xml.Serialization.XmlArrayAttribute";
        private const string XmlIgnoreAttributeName = "System.Xml.Serialization.XmlIgnoreAttribute";
        private const string NonSerializedAttributeName = "System.NonSerializedAttribute";
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
            var compilation = context.SemanticModel.Compilation;

            // TODO: nested and generic model types
            if (model.ContainingType is not null || model.IsGenericType) return null;

            var diagnostics = new List<PlanDiagnostic>();
            var surrogates = GetSurrogates(compilation, model, diagnostics);
            var parsed = new Dictionary<string, ProtoContractPlan>(StringComparer.Ordinal);
            var enums = new Dictionary<string, ProtoEnumPlan>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<INamedTypeSymbol>();
            // An auto-tuple is keyed in the model by type alone, but its *encoding* depends on the
            // compatibility level it is reached at - so the same tuple reached at two levels is not
            // expressible, and protobuf-net says so: "must use a single compatibility level". One
            // serializer per type is exactly our constraint too, so the conflict is recorded here and
            // the tuple dropped, which cascades to everything referring to it.
            var tupleLevels = new Dictionary<string, int>(StringComparer.Ordinal);
            var tupleConflicts = new HashSet<string>(StringComparer.Ordinal);

            // locations are tracked here rather than on the plan: they move whenever anything above
            // them moves, and putting them in the plan would invalidate the cached emit step
            var locations = new Dictionary<string, PlanLocation>(StringComparer.Ordinal);

            // off by default, exactly as RuntimeTypeModel.AllowParseableTypes is: it changes the wire
            // form of any member whose type qualifies, so it has to be opted into on both sides
            var allowParseableTypes = false;

            foreach (var attribute in model.GetAttributes())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attributeName = attribute.AttributeClass?.ToDisplayString();
                if (attributeName == ProtoModelAttributeName)
                {
                    foreach (var named in attribute.NamedArguments)
                    {
                        if (named.Key == "AllowParseableTypes" && named.Value.Value is true)
                        {
                            allowParseableTypes = true;
                        }
                    }
                    continue;
                }
                if (attributeName != ProtoSerializableAttributeName) continue;
                if (attribute.ConstructorArguments.Length != 1) continue;
                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol seed) pending.Enqueue(seed);
            }

            // walk the transitive closure: a seed's members can reach further contracts, and the same
            // contract is commonly reachable by several paths (or seeded *and* reachable)
            while (pending.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var type = pending.Dequeue();
                var key = Qualified(compilation, type);
                if (!visited.Add(key)) continue;

                locations[key] = PlanLocation.From(type);

                // An unresolved type - and the overwhelmingly likely cause is worth saying outright,
                // because the default explanation is actively misleading. Source generators all run
                // against the *same* input compilation and never see each other's output, so a DTO
                // produced by ProtoFileGenerator from a .proto in this same project is invisible
                // here: the seed arrives as an error symbol with a name but no attributes, and the
                // ordinary path then reports "it is not marked [ProtoContract]" about a type whose
                // generated source says exactly that. Probed, not guessed.
                if (type.TypeKind == TypeKind.Error)
                {
                    Contract(diagnostics, locations[key], key,
                        "the type could not be resolved. If it is produced by another source generator "
                        + "in this same project - a .proto compiled by protobuf-net.BuildTools, say - "
                        + "then it is not visible here: generators do not see each other's output. "
                        + "Move the generated types to a referenced project");
                    continue;
                }

                // an enum is a contract in its own right - [ProtoContract] allows it, and ref-emit
                // serves it with the same ISerializerProxy<TEnum> a repeated enum member uses,
                // rather than an ISerializer<TEnum> body of its own
                if (type.TypeKind == TypeKind.Enum)
                {
                    if (GetEnumPlan(compilation, type) is { } enumPlan) enums[key] = enumPlan;
                    else Contract(diagnostics, locations[key], key, "its underlying type is not supported");
                    continue;
                }

                var contract = ParseContract(compilation, type, diagnostics, surrogates,
                    allowParseableTypes, out var reachable, tupleLevels, tupleConflicts, cancellationToken);
                if (contract is not null) parsed.Add(key, contract);
                foreach (var next in reachable) pending.Enqueue(next);
            }

            // a tuple reached at two levels is not expressible with one serializer, and protobuf-net
            // refuses the model outright; dropping the tuple cascades to whatever referred to it
            foreach (var conflicted in tupleConflicts)
            {
                if (!parsed.Remove(conflicted)) continue;
                diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.UnsupportedContract,
                    locations.TryGetValue(conflicted, out var at) ? at : default, conflicted,
                    "it is reached at more than one compatibility level, and protobuf-net refuses that "
                    + "too: \"must use a single compatibility level ... this usually means it is being "
                    + "used in different contexts in the same model\""));
            }

            DropUnsatisfiable(parsed, locations, diagnostics);

            ProtoModelPlan? plan = null;
            if (parsed.Count != 0 || enums.Count != 0)
            {
                var contracts = parsed.Values.OrderBy(static x => x.TypeName, StringComparer.Ordinal).ToArray();
                var enumPlans = enums.Values.OrderBy(static x => x.TypeName, StringComparer.Ordinal).ToArray();
                var nameSpace = model.ContainingNamespace is { IsGlobalNamespace: false } ns
                    ? ns.ToDisplayString() : null;
                plan = new ProtoModelPlan(nameSpace, model.Name, new(contracts),
                    annotateTrimming: SupportsTrimAnnotations(compilation), enums: new(enumPlans),
                    aliases: new(DeclaredAliases(compilation).ToArray()),
                    emitInstance: CanEmitInstance(model),
                    // hiding the constructor only makes sense alongside something to use instead,
                    // and only when they have not written one themselves - a declared constructor is
                    // both the opt-out and the way to keep `new` working
                    emitConstructor: CanEmitInstance(model) && DeclaresNoConstructor(model),
                    isSealed: model.IsSealed,
                    // the nano pass is symbol-gated: it exists wherever the experimental reader is
                    // visible, which today is only this repo's own rigs
                    nanoReader: compilation.GetTypeByMetadataName("ProtoBuf.Nano.ReaderState") is not null);
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
                    // a hierarchy is all-or-nothing: every type routes through the root, and the
                    // root dispatches to each sub-type by name, so one missing link breaks the rest
                    var broken = contract.RootTypeName is { } root && !parsed.ContainsKey(root)
                        ? root
                        // a surrogate with a serializer of its own is delegated to, not inlined, so
                        // it needs no plan here
                        : contract.SurrogateSerializer is null
                            && contract.SurrogateTypeName is { } surrogate && !parsed.ContainsKey(surrogate)
                        ? surrogate
                        : contract.SubTypes.FirstOrDefault(x => !parsed.ContainsKey(x.TypeName)).TypeName;
                    if (broken is not null)
                    {
                        parsed.Remove(contract.TypeName);
                        locations.TryGetValue(contract.TypeName, out var brokenAt);
                        diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.OmittedCascade, brokenAt,
                            Simplify(contract.TypeName), Simplify(broken)));
                        removed = true;
                        continue;
                    }

                    foreach (var member in contract.Members)
                    {
                        // a map can reach a contract through either side, so both are tested
                        string? missing;
                        if (member.Kind == ProtoMemberKind.Map)
                        {
                            missing = Missing(member.Map.KeyKind, member.Map.KeyTypeName)
                                ?? Missing(member.Map.ValueKind, member.Map.ValueTypeName);
                        }
                        else
                        {
                            missing = Missing(member.Kind, member.TypeName);
                        }
                        if (missing is null) continue;

                        parsed.Remove(contract.TypeName);
                        locations.TryGetValue(contract.TypeName, out var location);
                        diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.OmittedCascade, location,
                            Simplify(contract.TypeName), Simplify(missing)));
                        removed = true;
                        break;
                    }
                }
            }
            while (removed);

            // the referenced contract, when it is one and we could not handle it
            string? Missing(ProtoMemberKind kind, string? typeName)
                => kind != ProtoMemberKind.Message || (typeName is not null && parsed.ContainsKey(typeName))
                    ? null : typeName;
        }

        private static ProtoContractPlan? ParseContract(
            Compilation compilation,
            INamedTypeSymbol type,
            List<PlanDiagnostic> diagnostics,
            Dictionary<string, SurrogateDeclaration> surrogates,
            bool allowParseableTypes,
            out List<INamedTypeSymbol> reachable,
            Dictionary<string, int> tupleLevels,
            HashSet<string> tupleConflicts,
            CancellationToken cancellationToken)
        {
            reachable = new List<INamedTypeSymbol>();

            var at = PlanLocation.From(type);
            var name = type.ToDisplayString();

            // Before anything else: can C# name this type at all? If two referenced assemblies
            // declare it and neither is aliased, there is no syntax that selects one - so emitting
            // *anything* produces CS0433 in a file the consumer did not write, which is the worst
            // error to hand someone. Refusing and naming the fix is strictly better. Every message
            // type in the model arrives here as a contract, so this one check covers them all.
            if (CannotBeNamed(compilation, type, out var ambiguity))
            {
                reachable = new List<INamedTypeSymbol>();
                return Contract(diagnostics, at, name, ambiguity);
            }

            // a model-level [ProtoSurrogate] stands in for the contract attribute entirely: the type
            // being surrogated need not - and for a BCL type, cannot - carry one, so this has to be
            // resolved before any of the "is this even a contract" checks
            surrogates.TryGetValue(Qualified(compilation, type),
                out var declaredSurrogate);

            // auto-tuple detection applies only when the type carries no contract family at all
            // (MetaType.GetContractFamily), and has to be tried before the shape checks below -
            // a tuple is commonly a closed generic, which they would otherwise reject
            if (declaredSurrogate is null && !HasContractFamily(type))
            {
                if (ParseTuple(compilation, type, diagnostics, out reachable, cancellationToken) is { } tuple) return tuple;
                reachable = new List<INamedTypeSymbol>();
                return Contract(diagnostics, at, name,
                    "it is not marked [ProtoContract], [DataContract] or [XmlType] and is not a tuple, so protobuf-net "
                    + "has no serializer for it either: \"No serializer defined for type\"");
            }

            var isValueType = type.TypeKind == TypeKind.Struct;
            var isInterface = type.TypeKind == TypeKind.Interface;
            if (!isValueType && !isInterface && type.TypeKind != TypeKind.Class)
            {
                return Contract(diagnostics, at, name, "only classes, structs and interfaces are supported");
            }
            // a *closed* generic is an ordinary contract - Roslyn hands us its members already
            // substituted, so `Wrapper<int>` and `Wrapper<string>` are simply two contracts. An open
            // one is not expressible: the services type is a single non-generic class, so there is
            // nowhere to put the type parameter
            // note `typeof(Foo<>)` gives an *unbound* symbol, whose TypeArguments are not type
            // parameters - so it needs its own test, or it falls through to a later check and is
            // refused for an unrelated-sounding reason
            if (type.IsUnboundGenericType || ContainsTypeParameter(type))
            {
                return Contract(diagnostics, at, name, "open generic types are not supported");
            }
            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                // full ref-emit compilation only reaches public API, and we match that for now
                return Contract(diagnostics, at, name, "the type is not public");
            }

            // the [ProtoInclude] links are read up-front: they decide whether inheritance is legal
            // here at all, and whether an abstract type has a reason to exist
            if (!TryGetSubTypes(type, out var subTypes))
            {
                return Option(diagnostics, at, name, "this form of [ProtoInclude]");
            }
            var linkedBases = GetLinkedBases(type);
            // legal C#, and each hierarchy works in isolation, but protobuf-net refuses the pair once
            // both are in one model - and the generator's model is always one model. Both ref-emit
            // paths refuse it, with different wording: the compiled path says "can only participate
            // in one inheritance hierarchy", the reflection path fails later with "the type cannot be
            // changed once a serializer has been generated"
            if (linkedBases.Count > 1)
            {
                return Contract(diagnostics, at, name,
                    $"it is declared as a [ProtoInclude] of both '{Simplify(linkedBases[0].ToDisplayString())}' "
                    + $"and '{Simplify(linkedBases[1].ToDisplayString())}', and protobuf-net refuses that "
                    + "too: \"can only participate in one inheritance hierarchy\"");
            }
            var linkedBase = linkedBases.Count == 1 ? linkedBases[0] : null;

            // ref-emit's rule, from TypeSerializer: the typed form is used whenever the type is in a
            // hierarchy, or when it is the only extension interface implemented. Extensible supplies
            // both, so a standalone Extensible gets the untyped overload.
            var inHierarchy = subTypes.Count != 0 || linkedBase is not null;
            var isExtensible = Implements(type, ExtensibleInterfaceName);
            var isTypedExtensible = Implements(type, TypedExtensibleInterfaceName);
            var extensible = ProtoExtensibleKind.None;
            if (isExtensible || isTypedExtensible)
            {
                if (isValueType)
                {
                    return Option(diagnostics, at, name, "IExtensible on a struct");
                }
                if (inHierarchy && !isTypedExtensible)
                {
                    // ref-emit throws while building the model rather than emitting anything
                    return Option(diagnostics, at, name,
                        "IExtensible without ITypedExtensible on a type with inheritance");
                }
                extensible = isTypedExtensible && (inHierarchy || !isExtensible)
                    ? ProtoExtensibleKind.Typed : ProtoExtensibleKind.Untyped;
            }

            // a struct is always constructible and can never have a base contract, so both of the
            // remaining checks are class-only
            if (!isValueType)
            {
                // a hierarchy is read through SubTypeState<T>, which never constructs the root
                // unless the payload actually contains one; an abstract leaf would be useless
                // an *interface* with no implementations is refused: ref-emit throws
                // "Unexpected sub-type" for any value, and there is nothing to construct. An
                // abstract *class* in the same position is emitted rather than refused - see
                // ProtoContractPlan.IsAbstract - because refusing it cascaded to referrers that
                // work perfectly well while the member stays null
                if (isInterface && subTypes.Count == 0)
                {
                    return Contract(diagnostics, at, name,
                        "an interface contract needs [ProtoInclude] for its implementations");
                }
                // note the constructor check is deferred: with a surrogate it is the *surrogate* that
                // gets constructed, which is exactly what lets an immutable type be surrogated

                // note: deriving from a type that does not [ProtoInclude] us needs *no* handling
                // here. protobuf-net binds only the type's own declared members and ignores the
                // base entirely - uniformly, whether the base is a contract or not, and whether or
                // not it includes some *other* type - so emitting the declared members is already
                // exact parity. `GetMembers()` is declared-only, so that falls out for free.
                // The silent loss is real, but it is the shipped analyzer's PBN0013 to report, and
                // it already does; refusing here only cost the contract without telling anyone more.
            }

            var surrogateType = declaredSurrogate?.Surrogate;
            INamedTypeSymbol? externalSerializer = null;
            bool? declaredScalar = null;
            string? surrogateSerializer = null;
            bool isContract = declaredSurrogate is not null;
            bool isDataContract = false, isXmlType = false, skipConstructor = false;
            bool isGroup = false, ignoreUnknownSubTypes = false, useProtoMembersOnly = false;
            List<Reservation>? reservations = null;
            HashSet<string>? partialIgnores = null;
            Dictionary<string, PartialMember>? partialMembers = null;
            var ignoreListHandling = false;
            var dataMemberOffset = 0;
            var implicitMode = 0;
            var implicitFirstTag = 1;

            // a model-surrogated type contributes nothing but its identity: the surrogate carries the
            // whole wire shape, so the type's own attributes are irrelevant - and inspecting them
            // would refuse types for decoration that has nothing to do with us. NodaTime's Instant
            // and Duration carry [XmlSchemaProvider], for instance.
            foreach (var attribute in declaredSurrogate is null
                ? (IEnumerable<AttributeData>)type.GetAttributes()
                : Array.Empty<AttributeData>())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                if (attributeName == ProtoContractAttributeName)
                {
                    if (attribute.ConstructorArguments.Length != 0)
                    {
                        return Option(diagnostics, at, name, "[ProtoContract] with constructor arguments");
                    }
                    foreach (var argument in attribute.NamedArguments)
                    {
                        // only options whose effect we reproduce are allowed through; everything
                        // else (ImplicitFields, Serializer, ...) still bails
                        switch (argument.Key)
                        {
                            case "SkipConstructor" when argument.Value.Value is bool skip:
                                skipConstructor = skip;
                                continue;
                            case "DataMemberOffset" when argument.Value.Value is int offset:
                                dataMemberOffset = offset;
                                continue;
                            // members inferred by convention rather than by attribute; the constants
                            // are ImplicitFields.AllPublic (1) and AllFields (2) - note that order,
                            // which is the opposite of the way they read
                            case "ImplicitFields" when argument.Value.Value is int mode:
                                implicitMode = mode;
                                continue;
                            case "ImplicitFirstTag" when argument.Value.Value is int first && first > 0:
                                implicitFirstTag = first;
                                continue;
                            // opts the type *out* of list handling, which is what lets a list-like
                            // contract be serialized as an ordinary message
                            case "IgnoreListHandling" when argument.Value.Value is bool ignoreList:
                                ignoreListHandling = ignoreList;
                                continue;
                            // the contract's own features carry WireTypeStartGroup rather than
                            // WireTypeString (MetaType.GetFeatures)
                            case "IsGroup" when argument.Value.Value is bool group:
                                isGroup = group;
                                continue;
                            // reaches TypeSerializer as assertKnownType: false, whose only effect is
                            // to omit ThrowUnexpectedSubtype - the same thing `sealed` does for us
                            case "IgnoreUnknownSubTypes" when argument.Value.Value is bool ignoreUnknown:
                                ignoreUnknownSubTypes = ignoreUnknown;
                                continue;
                            // narrows the attribute family to ProtoBuf only, exactly as ImplicitFields
                            // does, so [DataMember]/[XmlElement] orders stop applying
                            case "UseProtoMembersOnly" when argument.Value.Value is bool protoOnly:
                                useProtoMembersOnly = protoOnly;
                                continue;
                            // schema naming only: neither reaches the wire format
                            case "Name":
                            case "Origin":
                                continue;
                            case "Surrogate" when argument.Value.Value is INamedTypeSymbol declared:
                                surrogateType = declared;
                                continue;
                            // a hand-written serializer: we emit no body, just hand it out - but only
                            // if the generated code can actually name it. protobuf-net's own
                            // well-known types point at the internal PrimaryTypeProvider, which a
                            // consumer's assembly cannot see.
                            case "Serializer" when argument.Value.Value is INamedTypeSymbol external:
                                if (!compilation.IsSymbolAccessibleWithin(external, compilation.Assembly))
                                {
                                    return Option(diagnostics, at, name,
                                        $"[ProtoContract(Serializer = typeof({Simplify(external.ToDisplayString())}))], "
                                        + "because that serializer is not accessible here");
                                }
                                externalSerializer = external;
                                continue;
                            // the escape hatch for a serializer we cannot read the Features of; see
                            // ResolveExternalCategory
                            case "IsScalar" when argument.Value.Value is bool scalar:
                                declaredScalar = scalar;
                                continue;
                        }
                        return Option(diagnostics, at, name, $"[ProtoContract({argument.Key} = ...)]");
                    }
                    isContract = true;
                }
                // already read up-front, since it decides whether inheritance is legal here
                else if (attributeName == ProtoIncludeAttributeName) { }
                // [ProtoReserved] is *not* schema-only, despite looking it: MetaType.ValidateReservations
                // throws while building the model if a member, sub-type or enum value lands on a
                // reserved number or name. Ignoring it meant emitting contracts protobuf-net rejects
                else if (attributeName == ProtoReservedAttributeName)
                {
                    if (ParseReservation(attribute) is not { } reservation)
                    {
                        return Option(diagnostics, at, name, "this form of [ProtoReserved]");
                    }
                    (reservations ??= []).Add(reservation);
                }
                // excludes a member by name, from the type - MetaType skips it outright, before any
                // family or attribute inspection, so it wins over everything
                else if (attributeName == ProtoPartialIgnoreAttributeName)
                {
                    if (attribute.ConstructorArguments.Length != 1
                        || attribute.ConstructorArguments[0].Value is not string ignoredName)
                    {
                        return Option(diagnostics, at, name, "this form of [ProtoPartialIgnore]");
                    }
                    (partialIgnores ??= new HashSet<string>(StringComparer.Ordinal)).Add(ignoredName);
                }
                // [ProtoMember] applied to a member by name, from the type
                else if (attributeName == ProtoPartialMemberAttributeName)
                {
                    if (ParsePartialMember(diagnostics, at, name, attribute) is not { } partial)
                    {
                        return null;
                    }
                    // MetaType walks the list and takes the first entry naming this member that
                    // pins a tag, so a duplicate name is the earlier declaration winning
                    partialMembers ??= new Dictionary<string, PartialMember>(StringComparer.Ordinal);
                    if (!partialMembers.ContainsKey(partial.MemberName))
                    {
                        partialMembers.Add(partial.MemberName, partial);
                    }
                }
                // [DataContract] and [XmlType] are contract markers in their own right; their own
                // arguments (Name, Namespace) affect schema naming only
                else if (attributeName == DataContractAttributeName) isDataContract = true;
                else if (attributeName == XmlTypeAttributeName) isXmlType = true;
                else if (IsSignificantAttribute(attribute))
                {
                    return Option(diagnostics, at, name, $"[{AttributeName(attribute)}]");
                }
            }
            if (!isContract && !isDataContract && !isXmlType)
            {
                return Contract(diagnostics, at, name,
                    "it is not marked [ProtoContract], [DataContract] or [XmlType], so protobuf-net has no "
                    + "serializer for it either: \"No serializer defined for type\"");
            }

            if (InvalidDeclaredLevel(type) is { } badLevel)
            {
                return Contract(diagnostics, at, name,
                    $"it declares [CompatibilityLevel({badLevel})], which protobuf-net refuses too: "
                    + $"\"Compatiblity level '{badLevel}' is not recognized\"");
            }

            // protobuf-net serializes anything that *looks* like a list as a collection, even when it
            // carries [ProtoContract] and has members of its own - those members are simply ignored.
            // Reproducing that decision means replicating RepeatedSerializers.TryGetRepeatedProvider
            // exactly, so refuse instead: emitting a message here would silently disagree on the wire.
            // [ProtoContract(IgnoreListHandling = true)] is the documented opt-out.
            if (!ignoreListHandling && ResolveRepeated(type) is not null)
            {
                return Contract(diagnostics, at, name,
                    "protobuf-net would serialize it as a collection rather than a message, ignoring "
                    + "its members; use [ProtoContract(IgnoreListHandling = true)] if it should be a "
                    + "message");
            }

            if (skipConstructor && isValueType)
            {
                // meaningless for a struct, and we have no ref-emit reference for the combination
                return Option(diagnostics, at, name, "[ProtoContract(SkipConstructor = true)] on a struct");
            }

            // a surrogate carries the wire shape: the serializer is the *surrogate's* body with a
            // conversion at each end, so its members are the ones to parse. Nothing changes for a
            // member whose type is surrogated - that stays an ordinary sub-message.
            var memberSource = type;
            if (surrogateType is not null)
            {
                if (subTypes.Count != 0 || linkedBase is not null)
                {
                    // protobuf-net throws for this combination while building the model
                    return Option(diagnostics, at, name, "a surrogate on a type with inheritance");
                }
                if (Implements(surrogateType, "System.Collections.IEnumerable"))
                {
                    return Option(diagnostics, at, name, "a surrogate that is a collection");
                }
                // named converter methods are validated when the declaration is read; otherwise the
                // conversion has to be something C# can spell as a cast
                if (declaredSurrogate?.ToSurrogate is null
                    && (!CanConvert(compilation, type, surrogateType)
                        || !CanConvert(compilation, surrogateType, type)))
                {
                    return Option(diagnostics, at, name,
                        "a surrogate without conversion operators in both directions");
                }

                // when the surrogate has a serializer of its own there are no members to inline, so
                // the body converts and then *delegates* to it - which is what lets a well-known
                // type serve as a surrogate, as protobuf-net.NodaTime does
                surrogateSerializer = GetSubSerializer(compilation, surrogateType);
                if (surrogateSerializer == "null")
                {
                    // inbuilt: obtainable without naming the (internal) provider
                    // both arguments spelled out: the defaulted overload is ambiguous with the
                    // explicit one from a call site that supplies neither
                    surrogateSerializer = "global::ProtoBuf.Meta.TypeModel.GetInbuiltSerializer<"
                        + Qualified(compilation, surrogateType)
                        + ">(default, default)";
                }
                else if (surrogateSerializer is null)
                {
                    // an ordinary contract: it is a contract in its own right, and its members are
                    // inlined into this one's body
                    reachable.Add(surrogateType);
                    memberSource = surrogateType;
                }
                isValueType = surrogateType.IsValueType;
            }

            // whatever actually gets constructed on read: the surrogate when there is one, which is
            // what lets an immutable type be surrogated at all
            var usesConstructorAccessor = false;
            if (!isValueType && !memberSource.IsAbstract && !memberSource.InstanceConstructors.Any(
                static ctor => ctor.Parameters.Length == 0
                    && ctor.DeclaredAccessibility == Accessibility.Public))
            {
                // a *non-public* parameterless constructor is reachable through [UnsafeAccessor],
                // which is how RuntimeTypeModel behaves (it calls it by reflection); only ref-emit's
                // compiled path refuses these, exactly as it does a non-public setter
                if (SupportsUnsafeAccessor(compilation) && memberSource.InstanceConstructors.Any(
                    static ctor => ctor.Parameters.Length == 0))
                {
                    usesConstructorAccessor = true;
                }
                else if (memberSource.InstanceConstructors.Any(static ctor => ctor.Parameters.Length == 0))
                {
                    // it exists but we cannot reach it here; say so, and say what would fix it -
                    // down-level consumers should not be left guessing why a contract vanished
                    return Contract(diagnostics, at, name,
                        "its parameterless constructor is not public, which needs [UnsafeAccessor] (net8.0 or later)");
                }
                else
                {
                    // ref-emit throws "No parameterless constructor found" for this on *both* paths,
                    // so there is nothing to match; [ProtoContract(SkipConstructor = true)] is the
                    // documented way out
                    return Contract(diagnostics, at, name,
                        "there is no parameterless constructor and SkipConstructor is not set, which protobuf-net "
                        + "refuses too: \"No parameterless constructor found\"");
                }
            }

            // implicit mode numbers the members itself, which cannot be done member-by-member: the
            // tags come from sorting the whole set, so they are worked out up-front
            var implicitTags = GetImplicitTags(memberSource, implicitMode, implicitFirstTag,
                partialIgnores, partialMembers);

            // ...and it also narrows the attribute family to ProtoBuf only, so [DataMember] and
            // [XmlElement] orders stop applying (MetaType: `family &= AttributeFamily.ProtoBuf`).
            // UseProtoMembersOnly is the same narrowing by another route - GetContractFamily returns
            // AttributeFamily.ProtoBuf outright for it, without even looking at the rest
            if (implicitMode != 0 || useProtoMembersOnly) isDataContract = isXmlType = false;

            // indexed by ProtoCallbackKind; an unset entry has a null MethodName
            var callbacks = new ProtoCallbackPlan[4];

            var members = new List<ProtoMemberPlan>();
            foreach (var symbol in memberSource.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // serialization callbacks ([OnDeserialized] and friends) live on methods
                if (symbol is IMethodSymbol { IsImplicitlyDeclared: false, AssociatedSymbol: null } method)
                {
                    if (method.GetAttributes().FirstOrDefault(IsSignificantAttribute) is { } onMethod)
                    {
                        if (GetCallbackKind(onMethod) is not { } callbackKind)
                        {
                            return Option(diagnostics, PlanLocation.From(method), name,
                                $"[{AttributeName(onMethod)}] on methods");
                        }
                        if (!IsUsableCallback(method, callbackKind, out var takesContext))
                        {
                            return Option(diagnostics, PlanLocation.From(method), name,
                                $"this form of [{AttributeName(onMethod)}]");
                        }
                        callbacks[(int)callbackKind] = new ProtoCallbackPlan(method.Name, takesContext);
                    }
                    continue;
                }

                // fields are members in their own right, and behave exactly as properties do; the
                // implicit ones are auto-property backing fields, which the property itself covers
                ITypeSymbol memberType;
                switch (symbol)
                {
                    case IPropertySymbol property:
                        memberType = property.Type;
                        break;
                    // an implicit field is normally an auto-property's backing field, which the
                    // property itself covers - except under ImplicitFields.AllFields, where
                    // protobuf-net takes the backing field *instead* of the property
                    case IFieldSymbol field when !field.IsImplicitlyDeclared
                        || implicitTags.ContainsKey(field.Name):
                        memberType = field.Type;
                        break;
                    default:
                        continue;
                }

                // [ProtoPartialIgnore] excludes the member outright: MetaType tests it before any
                // family or attribute inspection, so it beats even an explicit [ProtoMember]
                if (partialIgnores is not null && partialIgnores.Contains(symbol.Name)) continue;

                var atMember = PlanLocation.From(symbol);
                int? fieldNumber = null, dataMemberOrder = null, xmlOrder = null;
                bool ignored = false, isPacked = false, overwriteList = false, isRequired = false;
                bool usesAccessor = false, isReadOnly = false;
                string? accessorField = null;
                var accessorReads = false;
                bool wrappedValue = false, wrappedValueGroup = false;
                bool wrappedCollection = false, wrappedCollectionGroup = false;
                var dataFormat = ProtoDataFormat.Default;
                var mapKeyFormat = ProtoDataFormat.Default;
                var mapValueFormat = ProtoDataFormat.Default;
                var disableMap = false;
                var hasProtoMap = false;
                AttributeData? declaredDefault = null;
                foreach (var attribute in symbol.GetAttributes())
                {
                    var attributeName = attribute.AttributeClass?.ToDisplayString();
                    if (attributeName == DefaultValueAttributeName)
                    {
                        declaredDefault = attribute;
                    }
                    else if (attributeName == DataMemberAttributeName)
                    {
                        dataMemberOrder = GetNamedInt(attribute, "Order");
                    }
                    else if (attributeName is XmlElementAttributeName or XmlArrayAttributeName)
                    {
                        xmlOrder = GetNamedInt(attribute, "Order");
                    }
                    else if (attributeName is XmlIgnoreAttributeName or NonSerializedAttributeName
                        or ProtoIgnoreAttributeName)
                    {
                        ignored = true;
                    }
                    else if (attributeName == ProtoMapAttributeName)
                    {
                        // KeyFormat/ValueFormat select the key and value wire types, which the map
                        // serializer takes as separate arguments; DisableMap drops out of map
                        // handling altogether, which is the same OptionFailOnDuplicateKey path an
                        // invalid map shape already takes
                        hasProtoMap = true;
                        foreach (var argument in attribute.NamedArguments)
                        {
                            // note GetDataFormat, not a cast: DataFormat and ProtoDataFormat do not
                            // share ordinals, so casting silently maps FixedSize onto Group
                            switch (argument.Key)
                            {
                                case "KeyFormat" when argument.Value.Value is int key:
                                    if (GetDataFormat(key) is not { } parsedKey)
                                    {
                                        return Option(diagnostics, atMember, name, "this DataFormat");
                                    }
                                    mapKeyFormat = parsedKey;
                                    continue;
                                case "ValueFormat" when argument.Value.Value is int value:
                                    if (GetDataFormat(value) is not { } parsedValue)
                                    {
                                        return Option(diagnostics, atMember, name, "this DataFormat");
                                    }
                                    mapValueFormat = parsedValue;
                                    continue;
                                case "DisableMap" when argument.Value.Value is bool disable:
                                    disableMap = disable;
                                    continue;
                            }
                            return Option(diagnostics, atMember, name, $"this form of [{AttributeName(attribute)}]");
                        }
                        // note protobuf-net reads KeyFormat/ValueFormat *only* when DisableMap is
                        // not set, so the two do not compose
                        if (disableMap)
                        {
                            mapKeyFormat = mapValueFormat = ProtoDataFormat.Default;
                        }
                    }
                    else if (attributeName is NullWrappedValueAttributeName or NullWrappedCollectionAttributeName)
                    {
                        var group = false;
                        foreach (var argument in attribute.NamedArguments)
                        {
                            if (argument.Key == "AsGroup" && argument.Value.Value is bool asGroup)
                            {
                                group = asGroup;
                                continue;
                            }
                            return Option(diagnostics, atMember, name, $"this form of [{AttributeName(attribute)}]");
                        }
                        if (attributeName == NullWrappedValueAttributeName)
                        {
                            wrappedValue = true;
                            wrappedValueGroup = group;
                        }
                        else
                        {
                            wrappedCollection = true;
                            wrappedCollectionGroup = group;
                        }
                    }
                    else if (attributeName == ProtoMemberAttributeName && attribute.NamedArguments.Length != 0
                        && attribute.NamedArguments.All(static x
                            => x.Key is "IsPacked" or "OverwriteList" or "IsRequired" or "DataFormat"
                                or "Name"))
                    {
                        foreach (var argument in attribute.NamedArguments)
                        {
                            switch (argument.Key)
                            {
                                case "IsPacked" when argument.Value.Value is bool packed:
                                    isPacked = packed;
                                    continue;
                                case "OverwriteList" when argument.Value.Value is bool overwrite:
                                    overwriteList = overwrite;
                                    continue;
                                case "IsRequired" when argument.Value.Value is bool required:
                                    isRequired = required;
                                    continue;
                                // schema naming only
                                case "Name":
                                    continue;
                                // the constant is the DataFormat enum's underlying int
                                case "DataFormat" when argument.Value.Value is int format:
                                    if (GetDataFormat(format) is not { } parsed)
                                    {
                                        return Option(diagnostics, atMember, name,
                                            "this DataFormat");
                                    }
                                    dataFormat = parsed;
                                    continue;
                            }
                            return Option(diagnostics, atMember, name, "this form of [ProtoMember]");
                        }
                        if (attribute.ConstructorArguments.Length != 1
                            || attribute.ConstructorArguments[0].Value is not int optionNumber)
                        {
                            return Option(diagnostics, atMember, name, "this form of [ProtoMember]");
                        }
                        fieldNumber = optionNumber;
                    }
                    else if (attributeName == ProtoMemberAttributeName)
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
                    else if (IsSignificantAttribute(attribute))
                    {
                        return Option(diagnostics, atMember, name, $"[{AttributeName(attribute)}]");
                    }
                }
                if (ignored) continue;

                // Neither option is refused on a non-collection member: ComposeListFeatures is only
                // reached from the repeated and map paths, so protobuf-net simply ignores them, and
                // so do we - the plan carries them but only the collection emit consults them. The
                // exception is OverwriteList on a "bytes" member, which is a scalar here yet still
                // reaches BlobSerializer's overwriteList; that one is honoured in the emit.

                // precedence, per MetaType.ApplyDefaultBehaviour: [ProtoMember] first, then the
                // type's [ProtoPartialMember] for this name, then [DataMember(Order)] - to which the
                // offset applies - then [XmlElement]/[XmlArray], to which it does not. An order below
                // 1 means "not declared" (DataMember.Order defaults to -1, and 0 is not a valid
                // protobuf field number).
                // NormalizeProtoMember only reaches the partial list when the member's own
                // [ProtoMember] did not pin a tag, and only pins from it when the tag is > 0
                if (fieldNumber is null && partialMembers is not null
                    && partialMembers.TryGetValue(symbol.Name, out var partial) && partial.FieldNumber > 0)
                {
                    fieldNumber = partial.FieldNumber;
                    isRequired = partial.IsRequired;
                    isPacked = partial.IsPacked;
                    dataFormat = partial.DataFormat;
                    overwriteList = partial.OverwriteList;
                }
                fieldNumber ??= isDataContract && dataMemberOrder >= 1
                    ? dataMemberOrder + dataMemberOffset : null;
                fieldNumber ??= isXmlType && xmlOrder >= 1 ? xmlOrder : null;
                fieldNumber ??= implicitTags.TryGetValue(symbol.Name, out var implicitTag) ? implicitTag : null;
                if (fieldNumber is null) continue;

                if (symbol is IFieldSymbol { IsConst: true })
                {
                    return Member(diagnostics, atMember, name, symbol.Name, "is a constant");
                }
                if (symbol.IsStatic) return Member(diagnostics, atMember, name, symbol.Name, "is static");
                if (symbol.DeclaredAccessibility != Accessibility.Public)
                {
                    // AllFields deliberately takes non-public fields; [UnsafeAccessor] reaches them
                    // by name, which is the same mechanism a non-public setter uses
                    if (symbol is not IFieldSymbol { IsReadOnly: false, IsConst: false })
                    {
                        return Member(diagnostics, atMember, name, symbol.Name, "is not public");
                    }
                    if (!SupportsUnsafeAccessor(compilation))
                    {
                        return Member(diagnostics, atMember, name, symbol.Name,
                            "is a non-public field, which needs [UnsafeAccessor] (net8.0 or later)");
                    }
                    accessorField = symbol.Name;
                    usesAccessor = true;
                    accessorReads = true;
                }
                switch (symbol)
                {
                    case IPropertySymbol property:
                        if (property.GetMethod is not { DeclaredAccessibility: Accessibility.Public })
                        {
                            return Member(diagnostics, atMember, name, symbol.Name, "has no public getter");
                        }
                        // anything C# will not let us assign directly goes through [UnsafeAccessor],
                        // preferring the *field* where we can name it exactly - which is the only way
                        // to reach a getter-only member at all, and is simpler than an accessor call
                        // for the rest. ref-emit's compiled path refuses all of these, apparently to
                        // stay verifiable; this is one of the few places we deliberately do better.
                        var needsAccessor = property.SetMethod is null
                            || property.SetMethod.DeclaredAccessibility != Accessibility.Public
                            || property.SetMethod.IsInitOnly;

                        // note the field is often `initonly` - that is exactly what a getter-only
                        // auto-property compiles to - and [UnsafeAccessor] hands back a plain `ref`
                        // regardless, which is the documented way to reach one
                        if (needsAccessor && SupportsUnsafeAccessor(compilation)
                            && GetBackingField(type, property, cancellationToken) is { } field)
                        {
                            accessorField = field.Name;
                            usesAccessor = true;
                        }
                        else if (property.SetMethod is null)
                        {
                            // no setter and no field we can name: the read still runs, and a
                            // collection or sub-message is populated by mutating what it already holds
                            isReadOnly = true;
                        }
                        else if (needsAccessor)
                        {
                            if (!SupportsUnsafeAccessor(compilation))
                            {
                                return Member(diagnostics, atMember, name, symbol.Name,
                                    property.SetMethod.IsInitOnly
                                        ? "has an init-only setter, which needs [UnsafeAccessor] (net8.0 or later)"
                                        : "has a non-public setter, which needs [UnsafeAccessor] (net8.0 or later)");
                            }
                            usesAccessor = true;
                        }
                        break;

                    // a readonly field cannot be assigned after construction, so it has the same
                    // problem an init-only property does
                    case IFieldSymbol { IsReadOnly: true }:
                        return Member(diagnostics, atMember, name, symbol.Name, "is read-only");
                }

                // the {Name}Specified / ShouldSerialize{Name}() conventions: matched by name, and
                // they *replace* the trivial-value write guard rather than adding to it
                var writeCondition = GetConditionalPattern(memberSource, symbol.Name, out var specifiedMember);

                if (GetMemberShape(compilation, memberType, surrogates, allowParseableTypes) is not { } shape)
                {
                    return Member(diagnostics, atMember, name, symbol.Name,
                        $"has unsupported type '{memberType.ToDisplayString()}'{WhyUnsupported()}");

                    // a bare "unsupported type" reads as our backlog even where the route is right
                    // there, so say which one it is - the sweep's member-type tail is mostly these
                    string WhyUnsupported()
                    {
                        // DateOnly/TimeOnly are recognised types whose BclHelpers methods only exist
                        // in the net6.0+ build, so the refusal is about the *reference*, not the
                        // type. Saying "protobuf-net has no serializer for it" here would be false
                        var bare = memberType is INamedTypeSymbol { IsGenericType: true } nullable
                            && nullable.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
                            ? nullable.TypeArguments[0] : memberType;
                        if (GetScalarKind(bare) is ProtoMemberKind.DateOnly or ProtoMemberKind.TimeOnly)
                        {
                            return "; protobuf-net serializes it through BclHelpers, but those methods "
                                + "are inside #if NET6_0_OR_GREATER - the referenced protobuf-net does "
                                + "not carry them";
                        }
                        // would it resolve with the model option turned on? asked rather than
                        // pattern-matched, so it stays true to ParseableSerializer.TryCreate
                        if (!allowParseableTypes
                            && GetMemberShape(compilation, memberType, surrogates, allowParseableTypes: true)
                                is not null)
                        {
                            return "; it has a ToString() and a static Parse(string), so "
                                + "[ProtoModel(AllowParseableTypes = true)] would include it - off by "
                                + "default, matching RuntimeTypeModel";
                        }
                        // System.Type is not a shortfall: ref-emit serializes it, but by round-tripping
                        // assembly-qualified names through Type.GetType, which is exactly the
                        // reflection AOT cannot do. Emitting it would compile and then fail at runtime
                        if (IsSystemType(memberType))
                        {
                            return "; System.Type is deliberately not supported, because ref-emit "
                                + "serializes it through Type.GetType, which native AOT cannot do";
                        }
                        // a type with no contract family at all is a *match*, not a shortfall:
                        // protobuf-net throws for it too, on both the reflection and persisted-dll
                        // paths. Worth saying, because it is much the largest group here and reads
                        // as our backlog otherwise. Interfaces and delegates land here as well.
                        // A collection is not itself the problem - protobuf-net serializes those
                        // perfectly well - so the question moves to its *element*, one level down.
                        // That is how List<ISomeInterface> gets an answer; a map's element is a
                        // KeyValuePair, which IsTupleCandidate excludes, so nesting stops there
                        var candidate = bare;
                        if (candidate is INamedTypeSymbol repeated
                            // a map is excluded: its key and value are separate, so there is no one
                            // "element" to name, and an enum on either side is our gap rather than
                            // a match - claiming protobuf-net cannot do it would be false
                            && ResolveRepeated(repeated) is { IsMap: false, Element: { } element })
                        {
                            candidate = element;
                        }
                        // an enum is a scalar by another route (GetScalarKind does not cover it), and
                        // needs no contract attribute, so it must not be read as "not a contract"
                        if (candidate is INamedTypeSymbol named && named.TypeKind != TypeKind.Enum
                            && !HasContractFamily(named)
                            && !IsTupleCandidate(named) && GetScalarKind(named) is null
                            && ResolveRepeated(named) is null)
                        {
                            var what = ReferenceEquals(candidate, bare)
                                ? "it" : $"its element '{named.ToDisplayString()}'";
                            return $"; {what} is not marked [ProtoContract], [DataContract] or "
                                + "[XmlType] and is not a tuple, so protobuf-net has no serializer "
                                + "for it either: \"No serializer defined for type\" - "
                                + "[ProtoSurrogate] on the model is the way to serialize a type you "
                                + "do not own";
                        }
                        // and nothing otherwise, deliberately. Plenty that land here are our own
                        // gaps - an enum-valued map, a nested map key - where a surrogate would send
                        // the reader somewhere pointless. A hint that is wrong is worse than none
                        return "";
                    }
                }
                var kind = shape.Kind;
                var message = shape.Message;

                // the null-wrapping rules, which protobuf-net enforces by *throwing* rather than by
                // ignoring the attribute - deliberately, so that widening them later is not a silent
                // behaviour change. Probed against ref-emit rather than read off the documentation:
                // a message or a compatibility-level BCL type is not a "scalar" for this purpose.
                var isCollection = shape.Repeated.Factory is not null;
                var isMap = shape.Map.Factory is not null;
                if (wrappedValue && !isCollection && !isMap)
                {
                    // these three are refusals that *match* protobuf-net rather than fall short of
                    // it: each throws while building the model, so there is no behaviour to
                    // reproduce. Worth wording as such - they are not waiting on us
                    if (kind is ProtoMemberKind.Message or ProtoMemberKind.Map
                        or ProtoMemberKind.DateTime or ProtoMemberKind.TimeSpan
                        or ProtoMemberKind.Guid or ProtoMemberKind.Decimal)
                    {
                        return Contract(diagnostics, atMember, name,
                            $"member '{symbol.Name}' has [NullWrappedValue] on a non-scalar, which protobuf-net "
                            + "refuses: \"NullWrappedValue can only be used with scalar types, or in a collection\"");
                    }
                    // a reference-type scalar is already nullable; a value type has to say so
                    if (!shape.IsNullable && kind is not (ProtoMemberKind.String or ProtoMemberKind.Bytes))
                    {
                        return Contract(diagnostics, atMember, name,
                            $"member '{symbol.Name}' has [NullWrappedValue] on a non-nullable value, which "
                            + "protobuf-net refuses: \"NullWrappedValue cannot be used with non-nullable values\"");
                    }
                    if (declaredDefault is not null)
                    {
                        return Contract(diagnostics, atMember, name,
                            $"member '{symbol.Name}' combines [NullWrappedValue] with [DefaultValue], which "
                            + "protobuf-net refuses");
                    }
                    // ...and the last three from the same run of guards in ValueMember. A lone
                    // wrapped value goes through WriteAny/ReadAny, which has nowhere to put a
                    // required flag, a packed flag or a non-default format
                    if (isRequired)
                    {
                        return Contract(diagnostics, atMember, name,
                            $"member '{symbol.Name}' combines [NullWrappedValue] with IsRequired, which "
                            + "protobuf-net refuses: \"NullWrappedValue cannot be used with required values\"");
                    }
                    if (isPacked)
                    {
                        return Contract(diagnostics, atMember, name,
                            $"member '{symbol.Name}' combines [NullWrappedValue] with IsPacked, which "
                            + "protobuf-net refuses: \"NullWrappedValue cannot be used with packed values\"");
                    }
                    if (dataFormat != ProtoDataFormat.Default)
                    {
                        return Contract(diagnostics, atMember, name,
                            $"member '{symbol.Name}' combines [NullWrappedValue] with a DataFormat, which "
                            + "protobuf-net refuses: \"NullWrappedValue can only be used with "
                            + "DataFormat.Default\"");
                    }
                }
                // a map is repeated too, and wraps exactly as a collection does
                if (wrappedCollection && !isCollection && !isMap)
                {
                    return Contract(diagnostics, atMember, name,
                        $"member '{symbol.Name}' has [NullWrappedCollection] on a non-collection, which protobuf-net "
                        + "refuses: \"NullWrappedCollection can only be used with collection types\"");
                }

                // protobuf-net reads [ProtoMap] only when the member resolved as repeated, so
                // anywhere else it is silently inert; refusing keeps the surprise visible
                if (hasProtoMap && !isMap)
                {
                    return Option(diagnostics, atMember, name, "[ProtoMap] on a non-dictionary member");
                }

                // the compatibility level chooses the encoding for the four BCL types, and nothing
                // else; resolving it for every member would be wasted work. A map counts: its key or
                // value may be one, and its own Kind is Map rather than the element's
                var compatibilityLevel = 200;
                var declaredCompatibilityLevel = 200;
                if (IsBclKind(kind) || IsBclKind(shape.Map.KeyKind) || IsBclKind(shape.Map.ValueKind))
                {
                    declaredCompatibilityLevel =
                        GetDeclaredLevel(symbol) ?? GetCompatibilityLevel(compilation, memberSource);
                    compatibilityLevel =
                        GetEffectiveCompatibilityLevel(declaredCompatibilityLevel, dataFormat);
                }
                if (IsBclKind(kind))
                {

                    // ZigZag throws while building the model; everything else selects a field-header
                    // wire type (see BclWireType), which for several combinations means "no change"
                    if (dataFormat == ProtoDataFormat.ZigZag)
                    {
                        return Option(diagnostics, atMember, name, "DataFormat.ZigZag on a BCL type");
                    }
                    if (declaredDefault is not null)
                    {
                        return Option(diagnostics, atMember, name, "[DefaultValue] on a BCL type");
                    }
                }
                if (InvalidDeclaredLevel(symbol) is { } badMemberLevel)
                {
                    return Contract(diagnostics, atMember, name,
                        $"member '{symbol.Name}' declares [CompatibilityLevel({badMemberLevel})], which "
                        + $"protobuf-net refuses too: \"Compatiblity level '{badMemberLevel}' is not "
                        + "recognized\"");
                }

                // an auto-tuple's encoding follows the level it is *reached at*, so record that here;
                // the model refuses the tuple outright if two members disagree
                foreach (var reachedTuple in TupleMessages(shape))
                {
                    var reachedLevel = GetDeclaredLevel(symbol)
                        ?? GetCompatibilityLevel(compilation, memberSource);
                    var tupleKey = Qualified(compilation, reachedTuple);
                    if (tupleLevels.TryGetValue(tupleKey, out var seen))
                    {
                        if (seen != reachedLevel) tupleConflicts.Add(tupleKey);
                    }
                    else
                    {
                        tupleLevels[tupleKey] = reachedLevel;
                    }
                }

                // Group frames a sub-message, so a collection of *scalars* has nothing for the markers
                // to wrap; protobuf-net throws while building the model, on both ref-emit paths, with
                // the notably unhelpful "Operation is not valid due to the current state of the
                // object". A map is exempt: there the group wraps the key/value entry, which is a
                // message whatever the element types are
                if (dataFormat == ProtoDataFormat.Group && shape.Repeated.Factory is not null
                    && kind != ProtoMemberKind.Message)
                {
                    return Contract(diagnostics, atMember, name,
                        $"member '{symbol.Name}' has DataFormat.Group on a collection of scalars, which "
                        + "protobuf-net refuses too: \"Operation is not valid due to the current state "
                        + "of the object\"");
                }

                // on anything else WellKnown has nothing to promote, and ref-emit simply ignores it
                // needed for the [UnsafeAccessor] signature, as the type argument to ReadAny/WriteAny,
                // and to spell out the default() an overwriting "bytes" read passes to AppendBytes
                var declaredTypeName = shape.DeclaredTypeName ?? (usesAccessor || wrappedValue
                    || (overwriteList && kind == ProtoMemberKind.Bytes)
                    ? Qualified(compilation, memberType) : null);
                var isNullable = shape.IsNullable;
                var enumTypeName = (shape.EnumType is null ? null : Qualified(compilation, shape.EnumType));

                string? defaultLiteral = null;
                // a null declared default means "no declared default", exactly as ref-emit treats it
                if (declaredDefault is not null && !IsNullDefault(declaredDefault))
                {
                    if (kind == ProtoMemberKind.Message)
                    {
                        return Option(diagnostics, atMember, name, "[DefaultValue] on a message member");
                    }
                    defaultLiteral = GetDefaultLiteral(declaredDefault, kind, enumTypeName, shape.EnumType);
                    if (defaultLiteral is null)
                    {
                        return Option(diagnostics, atMember, name, "this form of [DefaultValue]");
                    }
                }

                if (shape.Map.Factory is not null)
                {
                    // a map can reach a contract through its key *and* its value
                    foreach (var reached in shape.MapMessages!) reachable.Add(reached);

                    // note the order: validity is decided *with* the declared key format, and only
                    // then are the formats kept. MetaType applies [ProtoMap]'s KeyFormat/ValueFormat
                    // inside `if (mapEnabled && IsValidProtobufMap(...))`, so a shape that is not a
                    // valid protobuf map - a DateTime key, say - silently discards both and falls
                    // back to the level-200 form. Applying them anyway emitted element serializers
                    // ref-emit does not, which is the residual HazMaps disagreement
                    var mapPlan = WithLevelledKey(shape.Map, declaredCompatibilityLevel, mapKeyFormat);
                    if (!mapPlan.IsValidProtobufMap || disableMap)
                    {
                        mapKeyFormat = mapValueFormat = ProtoDataFormat.Default;
                    }

                    members.Add(new ProtoMemberPlan(fieldNumber.Value, symbol.Name, kind,
                        declaredTypeName: declaredTypeName, map: mapPlan,
                        isPacked: isPacked, overwriteList: overwriteList, wrappedValue: wrappedValue, wrappedValueGroup: wrappedValueGroup, wrappedCollection: wrappedCollection, wrappedCollectionGroup: wrappedCollectionGroup,
                        dataFormat: dataFormat, isRequired: isRequired, usesAccessor: usesAccessor, compatibilityLevel: compatibilityLevel, declaredCompatibilityLevel: declaredCompatibilityLevel, isReadOnly: isReadOnly, writeCondition: writeCondition, specifiedMember: specifiedMember, accessorField: accessorField, accessorReads: accessorReads, mapKeyFormat: mapKeyFormat, mapValueFormat: mapValueFormat, disableMap: disableMap));
                }
                else if (kind == ProtoMemberKind.Message)
                {
                    // enqueued even if it turns out to be unsupported, so that it reports its own
                    // reason and this contract is dropped by cascade with a message that chains
                    // an inbuilt type (see GetSubSerializer) is served by protobuf-net itself, so it
                    // is neither ours to emit nor a reason to drop anything by cascade
                    var subSerializer = GetSubSerializer(compilation, message!);
                    if (subSerializer != "null") reachable.Add(message!);

                    // a hand-written serializer may present the type as a *scalar*, which frames the
                    // member by its own wire type rather than as a sub-message. Undetermined is not a
                    // problem here: the referenced contract refuses itself for the same reason, and
                    // this member is dropped by cascade
                    var hasExternal = subSerializer is not null && subSerializer != "null";
                    var subCategory = hasExternal ? ResolveExternalScalar(compilation, message!) : null;
                    var subScalar = hasExternal && subCategory == true;
                    // the category could not be established - Features is a property, and this
                    // serializer's declaration is not in this compilation. Rather than refuse, defer
                    // the framing to WriteAny/ReadAny, which switch on it at run time
                    var subDynamic = hasExternal && subCategory is null && HasExternalSerializer(message!);
                    // A *collection* element can defer its wire type after all - RepeatedFeatures
                    // states none and WriteRepeated/ReadRepeated inherit it from the element
                    // serializer. A map cannot yet: its key and value features are separate
                    // arguments and are composed differently, so that shape is still refused.
                    if ((subScalar || subDynamic) && shape.Map.Factory is not null)
                    {
                        // The unary form defers framing to WriteAny/ReadAny, which pick it from the
                        // serializer at run time. An *element* cannot: the element's wire type goes
                        // into the collection's features, which are baked into the call, so a
                        // category that is scalar - or simply unknown - has nowhere to go. Emitting
                        // the message form regardless is what produced "Invalid wire-type String"
                        // on Issue1083's List<WrappingStruct>.
                        return Option(diagnostics, atMember, name,
                            $"member '{symbol.Name}' whose element type is served by a "
                            + (subScalar ? "CategoryScalar serializer" : "hand-written serializer "
                                + "whose category cannot be determined here")
                            + "; the unary form is emitted, but the element form needs the category "
                            + "baked into the collection's features and so cannot defer it");
                    }

                    members.Add(new ProtoMemberPlan(fieldNumber.Value, symbol.Name, kind,
                        Qualified(compilation, message!),
                        isNullable: isNullable, memberIsValueType: message!.IsValueType,
                        repeated: shape.Repeated, elementTypeName: shape.ElementTypeName,
                        declaredTypeName: declaredTypeName,
                        isPacked: isPacked, overwriteList: overwriteList, wrappedValue: wrappedValue, wrappedValueGroup: wrappedValueGroup, wrappedCollection: wrappedCollection, wrappedCollectionGroup: wrappedCollectionGroup,
                        dataFormat: dataFormat, isRequired: isRequired, usesAccessor: usesAccessor, compatibilityLevel: compatibilityLevel, declaredCompatibilityLevel: declaredCompatibilityLevel, isReadOnly: isReadOnly, writeCondition: writeCondition, specifiedMember: specifiedMember,
                        accessorField: accessorField, accessorReads: accessorReads, subSerializer: subSerializer, subSerializerIsScalar: subScalar, subSerializerDynamic: subDynamic, mapKeyFormat: mapKeyFormat, mapValueFormat: mapValueFormat, disableMap: disableMap));
                }
                else
                {
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, symbol.Name, kind,
                        defaultLiteral: defaultLiteral, isNullable: isNullable,
                        // a value type is never null, so neither side tests for it. For the struct
                        // "bytes" shapes that is not merely tidier: `!= null` does not compile
                        // against Memory<byte> or ReadOnlyMemory<byte> at all
                        memberIsValueType: kind is ProtoMemberKind.Parseable or ProtoMemberKind.Bytes
                            && memberType.IsValueType,
                        enumTypeName: enumTypeName,
                        repeated: shape.Repeated, elementTypeName: shape.ElementTypeName,
                        declaredTypeName: declaredTypeName,
                        isPacked: isPacked, overwriteList: overwriteList, wrappedValue: wrappedValue, wrappedValueGroup: wrappedValueGroup, wrappedCollection: wrappedCollection, wrappedCollectionGroup: wrappedCollectionGroup,
                        dataFormat: dataFormat, isRequired: isRequired, usesAccessor: usesAccessor, compatibilityLevel: compatibilityLevel, declaredCompatibilityLevel: declaredCompatibilityLevel, isReadOnly: isReadOnly, writeCondition: writeCondition, specifiedMember: specifiedMember, accessorField: accessorField, accessorReads: accessorReads, mapKeyFormat: mapKeyFormat, mapValueFormat: mapValueFormat, disableMap: disableMap));
                }
            }

            // note there is no "it has no members" refusal: an empty message is entirely legal
            // protobuf, and .proto-generated DTOs are full of them
            members.Sort(static (x, y) => x.FieldNumber.CompareTo(y.FieldNumber));

            // two members on one field number would emit duplicate switch labels, which does not
            // compile; protobuf-net itself throws for this, so refusing loses nothing
            for (int i = 1; i < members.Count; i++)
            {
                if (members[i].FieldNumber != members[i - 1].FieldNumber) continue;
                return Contract(diagnostics, at, name,
                    $"members '{members[i - 1].Name}' and '{members[i].Name}' share field number "
                    + members[i].FieldNumber.ToString(CultureInfo.InvariantCulture));
            }

            // a hand-written serializer replaces the body entirely: nothing we parsed above is used,
            // and the services type hands that serializer out through ISerializerProxy<T> instead
            if (externalSerializer is not null)
            {
                // ...but *how* a member of this type is framed depends on the serializer's category,
                // and assuming "message" writes a length prefix where a scalar serializer writes a
                // bare varint. The annotation wins where present, since it is the only route that
                // survives into metadata; otherwise fold the declaration if it is in this compilation
                var fromSource = ReadCategoryFromSource(compilation, externalSerializer);
                if (declaredScalar is { } stated && fromSource is { } observed && stated != observed)
                {
                    return Option(diagnostics, at, name,
                        $"[ProtoContract(IsScalar = {(stated ? "true" : "false")})], which contradicts "
                        + $"the serializer: {Simplify(externalSerializer.ToDisplayString())}.Features "
                        + $"declares Category{(observed ? "Scalar" : "Message")}");
                }
                var isScalar = declaredScalar ?? fromSource;
                // An undetermined category used to drop the contract. It no longer needs to: this
                // type emits no serializer body of its own - the services type just hands the
                // hand-written one out - and the only thing the category decided was how a *member*
                // of this type is framed, which WriteAny/ReadAny now decide at run time from the
                // serializer's real Features. Members set SubSerializerDynamic for that.
                return new ProtoContractPlan(
                    Qualified(compilation, type),
                    default, isValueType,
                    externalSerializerTypeName: Qualified(compilation, externalSerializer),
                    externalSerializerIsScalar: isScalar == true,
                    externalSerializerCategoryKnown: isScalar is not null);
            }

            // the whole hierarchy has to be in the model, since every type in it routes through the
            // root; walking both ways gets there from whichever end was seeded
            string? rootTypeName = null;
            // MetaType.ValidateReservations, which throws while building the model - so a contract
            // that trips it does not work in protobuf-net at all, and emitting one is worse than
            // dropping it. Checked here because it needs the members *and* the sub-types
            if (reservations is not null)
            {
                foreach (var reservation in reservations)
                {
                    if (reservation.From != 0)
                    {
                        foreach (var member in members)
                        {
                            if (member.FieldNumber < reservation.From || member.FieldNumber > reservation.To) continue;
                            return Contract(diagnostics, at, name,
                                $"Field {member.FieldNumber} is reserved and cannot be used for data "
                                + $"member '{member.Name}'{reservation.Suffix}, which protobuf-net refuses too");
                        }
                        foreach (var subType in subTypes)
                        {
                            if (subType.Tag < reservation.From || subType.Tag > reservation.To) continue;
                            return Contract(diagnostics, at, name,
                                $"Field {subType.Tag} is reserved and cannot be used for sub-type "
                                + $"'{Simplify(subType.Type.ToDisplayString())}'{reservation.Suffix}, "
                                + "which protobuf-net refuses too");
                        }
                    }
                    else
                    {
                        foreach (var member in members)
                        {
                            if (member.Name != reservation.ReservedName) continue;
                            return Contract(diagnostics, at, name,
                                $"Field '{member.Name}' is reserved and cannot be used for data member "
                                + $"{member.FieldNumber}{reservation.Suffix}, which protobuf-net refuses too");
                        }
                        foreach (var subType in subTypes)
                        {
                            if (subType.Type.Name != reservation.ReservedName) continue;
                            return Contract(diagnostics, at, name,
                                $"Field '{reservation.ReservedName}' is reserved and cannot be used for "
                                + $"sub-type {subType.Tag}{reservation.Suffix}, which protobuf-net refuses too");
                        }
                    }
                }
            }

            var subTypePlans = new ProtoSubTypePlan[subTypes.Count];
            if (subTypes.Count != 0 || linkedBase is not null)
            {
                // members and sub-type tags share one switch in ReadSubType, so a collision between
                // them is a duplicate label just as much as two members would be
                foreach (var subType in subTypes)
                {
                    foreach (var member in members)
                    {
                        if (member.FieldNumber != subType.Tag) continue;
                        return Contract(diagnostics, at, name,
                            $"member '{member.Name}' and [ProtoInclude] share field number "
                            + subType.Tag.ToString(CultureInfo.InvariantCulture));
                    }
                }

                // every hierarchy API is constrained to reference types - ISubTypeSerializer<T>,
                // WriteSubType, ReadSubType, SubTypeState<T> - so a value-type sub-type does not
                // merely misbehave, it does not compile. Only reachable through an interface, since
                // a struct cannot derive from a class. protobuf-net refuses it too, at runtime:
                // "Unexpected sub-type", on both the reflection and compiled paths
                foreach (var subType in subTypes)
                {
                    if (!subType.Type.IsValueType) continue;
                    return Contract(diagnostics, at, name,
                        $"'{Simplify(subType.Type.ToDisplayString())}' is declared as a [ProtoInclude] "
                        + "but is a value type, and protobuf-net refuses that too: \"Unexpected sub-type\"");
                }

                rootTypeName = GetHierarchyRoot(type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (linkedBase is not null) reachable.Add(linkedBase);
                for (int i = 0; i < subTypes.Count; i++)
                {
                    subTypePlans[i] = new ProtoSubTypePlan(subTypes[i].Tag,
                        Qualified(compilation, subTypes[i].Type),
                        subTypes[i].IsGroup);
                    reachable.Add(subTypes[i].Type);
                }
            }

            return new ProtoContractPlan(
                Qualified(compilation, type),
                new(members.ToArray()), isValueType, skipConstructor, isSealed: memberSource.IsSealed,
                rootTypeName: rootTypeName, subTypes: new(subTypePlans), extensible: extensible,
                surrogateTypeName: (surrogateType is null ? null : Qualified(compilation, surrogateType)),
                toSurrogate: declaredSurrogate?.ToSurrogate, toUnderlying: declaredSurrogate?.ToUnderlying,
                surrogateSerializer: surrogateSerializer,
                usesConstructorAccessor: usesConstructorAccessor,
                callbacks: new(callbacks),
                isAbstract: type.IsAbstract && subTypes.Count == 0,
                isGroup: isGroup, ignoreUnknownSubTypes: ignoreUnknownSubTypes);
        }

        /// <summary>
        /// The auto-tuple types a member shape reaches — directly, as a collection element, or as
        /// either side of a map.
        /// </summary>
        private static IEnumerable<INamedTypeSymbol> TupleMessages(MemberShape shape)
        {
            if (shape.Message is { } message && IsTupleCandidate(message)) yield return message;
            if (shape.MapMessages is null) yield break;
            foreach (var reached in shape.MapMessages)
            {
                if (IsTupleCandidate(reached)) yield return reached;
            }
        }

        /// <summary>
        /// Is a hand-written serializer's category <c>CategoryScalar</c>? Null when it cannot be
        /// established, in which case the contract is refused rather than guessed at.
        /// </summary>
        /// <remarks>
        /// This is the one thing about an external serializer that changes the emitted <em>shape</em>
        /// and that a generator cannot simply look up: the category lives in the serializer's
        /// <c>Features</c> property, which ref-emit obtains by instantiating it. Two routes, in this
        /// order:
        /// <list type="number">
        /// <item><c>[ProtoContract(IsScalar = …)]</c>, which is an attribute <em>argument</em> and so
        /// survives into metadata — the only route that works for a serializer in a compiled
        /// reference.</item>
        /// <item>the <c>Features</c> declaration itself, when the serializer is in this compilation:
        /// it is almost always a constant expression (<c>CategoryScalar | WireTypeVarint</c>), which
        /// Roslyn will fold.</item>
        /// </list>
        /// Where both are available and disagree, the caller reports it: a stale annotation would
        /// otherwise silently change the framing on the wire.
        /// </remarks>
        /// <summary>
        /// Is the type's hand-written serializer a scalar one? The same two routes the contract's own
        /// parse uses, for a type reached as a <em>member</em>.
        /// </summary>
        private static bool? ResolveExternalScalar(Compilation compilation, INamedTypeSymbol type)
        {
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoContractAttributeName) continue;
                INamedTypeSymbol? serializer = null;
                bool? stated = null;
                foreach (var argument in attribute.NamedArguments)
                {
                    if (argument.Key == "Serializer" && argument.Value.Value is INamedTypeSymbol s) serializer = s;
                    else if (argument.Key == "IsScalar" && argument.Value.Value is bool b) stated = b;
                }
                if (serializer is null) continue;
                return stated ?? ReadCategoryFromSource(compilation, serializer);
            }
            return null;
        }

        /// <summary>
        /// Whether the shared <c>Instance</c> accessor can be emitted onto the consumer's model.
        /// </summary>
        /// <remarks>
        /// Two ways it cannot: they already declare a member of that name, which would be CS0102 in
        /// their own build; or the type has constructors but no accessible parameterless one, so
        /// there is nothing for the initialiser to call. Both are the consumer's code, so the answer
        /// is to emit nothing rather than to complain — the model works perfectly well without it.
        /// </remarks>
        private static bool CanEmitInstance(INamedTypeSymbol model)
        {
            if (model.GetMembers("Instance").Any()) return false;
            var constructors = model.InstanceConstructors
                .Where(static x => x.DeclaredAccessibility != Accessibility.Private).ToList();
            return constructors.Count == 0 || constructors.Any(static x => x.Parameters.Length == 0);
        }

        /// <summary>Whether the consumer wrote no constructor of their own.</summary>
        /// <remarks>
        /// A class with none has exactly one, implicitly declared; anything else means they have
        /// expressed an intent about construction and we should not override it.
        /// </remarks>
        private static bool DeclaresNoConstructor(INamedTypeSymbol model)
            => model.InstanceConstructors.All(static x => x.IsImplicitlyDeclared);

        /// <summary>Whether the type declares a hand-written serializer at all.</summary>
        /// <remarks>
        /// <see cref="ResolveExternalScalar"/> returns null both for "no such serializer" and for
        /// "there is one, but its category cannot be determined here", and those need opposite
        /// treatment - the first is not our business, the second is what dynamic framing exists for.
        /// </remarks>
        private static bool HasExternalSerializer(INamedTypeSymbol type)
        {
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoContractAttributeName) continue;
                foreach (var argument in attribute.NamedArguments)
                {
                    if (argument.Key == "Serializer" && argument.Value.Value is INamedTypeSymbol) return true;
                }
            }
            return false;
        }

        private static bool? ReadCategoryFromSource(Compilation compilation, INamedTypeSymbol serializer)
        {
            // SerializerFeatures: CategoryScalar = 1 << 5, CategoryMessage = 1 << 6, and the category
            // is those two bits (CategoryMask). Taken from the enum, not assumed - the first guess
            // here was 1 and 2, which would have classified everything as a message.
            const int CategoryScalar = 1 << 5, CategoryMessage = 1 << 6;
            const int CategoryMask = CategoryScalar | CategoryMessage;

            foreach (var member in serializer.GetMembers())
            {
                // an explicit interface implementation is named ProtoBuf.Serializers.ISerializer<T>.Features
                if (member is not IPropertySymbol property) continue;
                if (property.Name != "Features"
                    && !property.Name.EndsWith(".Features", StringComparison.Ordinal))
                {
                    continue;
                }
                foreach (var reference in property.DeclaringSyntaxReferences)
                {
                    // only an expression body is foldable; a block body could be anything
                    if (reference.GetSyntax() is not PropertyDeclarationSyntax
                        { ExpressionBody.Expression: { } expression })
                    {
                        continue;
                    }
                    if (!compilation.ContainsSyntaxTree(reference.SyntaxTree)) continue;
                    var semantic = compilation.GetSemanticModel(reference.SyntaxTree);
                    if (semantic.GetConstantValue(expression) is not { HasValue: true, Value: int features })
                    {
                        continue;
                    }
                    return (features & CategoryMask) == CategoryScalar;
                }
            }
            return null;
        }

        /// <summary>
        /// A <c>[ProtoReserved]</c> declaration: either a number range or a name, never both —
        /// <c>MetaType.ValidateReservations</c> switches on <c>From != 0</c>.
        /// </summary>
        private readonly struct Reservation
        {
            public Reservation(int from, int to, string? reservedName, string? comment)
            {
                From = from;
                To = to;
                ReservedName = reservedName;
                Comment = comment;
            }

            public int From { get; }
            public int To { get; }
            public string? ReservedName { get; }
            public string? Comment { get; }

            /// <summary>Rendered as protobuf-net renders it, so the diagnostic quotes it exactly.</summary>
            public string Suffix => string.IsNullOrWhiteSpace(Comment) ? "" : $" ({Comment})";
        }

        /// <summary>
        /// Read a <c>[ProtoReserved]</c>. The three constructors are <c>(int)</c>, <c>(int, int)</c>
        /// and <c>(string)</c>, each with an optional trailing comment.
        /// </summary>
        private static Reservation? ParseReservation(AttributeData attribute)
        {
            var arguments = attribute.ConstructorArguments;
            // named arguments would be setting From/To/Name directly, which the attribute does not allow
            if (attribute.NamedArguments.Length != 0) return null;
            switch (arguments.Length)
            {
                case 1 when arguments[0].Value is int single:
                    return new Reservation(single, single, null, null);
                case 1 when arguments[0].Value is string named:
                    return new Reservation(0, 0, named, null);
                case 2 when arguments[0].Value is int single2 && arguments[1].Value is string or null:
                    return new Reservation(single2, single2, null, arguments[1].Value as string);
                case 2 when arguments[0].Value is int from && arguments[1].Value is int to:
                    return new Reservation(from, to, null, null);
                case 2 when arguments[0].Value is string named2 && arguments[1].Value is string or null:
                    return new Reservation(0, 0, named2, arguments[1].Value as string);
                case 3 when arguments[0].Value is int from2 && arguments[1].Value is int to2
                    && arguments[2].Value is string or null:
                    return new Reservation(from2, to2, null, arguments[2].Value as string);
                default:
                    return null;
            }
        }

        /// <summary>
        /// A <c>[ProtoPartialMember(tag, "Name")]</c> declared on the type: a <c>[ProtoMember]</c>
        /// applied to a member by name.
        /// </summary>
        private readonly struct PartialMember
        {
            public PartialMember(string memberName, int fieldNumber, bool isRequired, bool isPacked,
                ProtoDataFormat dataFormat, bool overwriteList)
            {
                MemberName = memberName;
                FieldNumber = fieldNumber;
                IsRequired = isRequired;
                IsPacked = isPacked;
                DataFormat = dataFormat;
                OverwriteList = overwriteList;
            }

            public string MemberName { get; }
            public int FieldNumber { get; }
            public bool IsRequired { get; }
            public bool IsPacked { get; }
            public ProtoDataFormat DataFormat { get; }
            public bool OverwriteList { get; }
        }

        /// <summary>
        /// Read a <c>[ProtoPartialMember]</c>, which takes the same named arguments as
        /// <c>[ProtoMember]</c> — with one exception, noted below.
        /// </summary>
        private static PartialMember? ParsePartialMember(List<PlanDiagnostic> diagnostics,
            PlanLocation at, string type, AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length != 2
                || attribute.ConstructorArguments[0].Value is not int fieldNumber
                || attribute.ConstructorArguments[1].Value is not string memberName)
            {
                Option(diagnostics, at, type, "this form of [ProtoPartialMember]");
                return null;
            }
            bool isRequired = false, isPacked = false, overwriteList = false;
            var dataFormat = ProtoDataFormat.Default;
            foreach (var argument in attribute.NamedArguments)
            {
                switch (argument.Key)
                {
                    case "IsRequired" when argument.Value.Value is bool required:
                        isRequired = required;
                        continue;
                    case "IsPacked" when argument.Value.Value is bool packed:
                        isPacked = packed;
                        continue;
                    // the constant is the DataFormat enum's underlying int
                    case "DataFormat" when argument.Value.Value is int format:
                        if (GetDataFormat(format) is not { } parsed)
                        {
                            Option(diagnostics, at, type, "this DataFormat");
                            return null;
                        }
                        dataFormat = parsed;
                        continue;
                    // schema naming only
                    case "Name":
                        continue;
                    // OverwriteList used to be refused here, because MetaType's partial-member branch
                    // read it from `attrib` - the member's own [ProtoMember], necessarily null when
                    // that branch runs - rather than from `ppma`, so protobuf-net silently ignored it
                    // and honouring it would have made our reads merge differently from ref-emit's.
                    // That was a one-token bug in MetaType, now fixed, so it is honoured on both paths.
                    case "OverwriteList" when argument.Value.Value is bool partialOverwrite:
                        overwriteList = partialOverwrite;
                        continue;
                }
                Option(diagnostics, at, type, $"[ProtoPartialMember({argument.Key} = ...)]");
                return null;
            }
            return new PartialMember(memberName, fieldNumber, isRequired, isPacked, dataFormat,
                overwriteList);
        }

        /// <summary><c>System.Type</c>, or an array or collection of it.</summary>
        private static bool IsSystemType(ITypeSymbol type)
            => type switch
            {
                IArrayTypeSymbol array => IsSystemType(array.ElementType),
                INamedTypeSymbol { TypeArguments.Length: 1 } generic => IsSystemType(generic.TypeArguments[0]),
                _ => type.ToDisplayString() == "System.Type",
            };

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

        /// <summary>
        /// The <c>{Name}Specified</c> / <c>ShouldSerialize{Name}()</c> conventions, if present.
        /// </summary>
        /// <remarks>
        /// Inherited from the System.ComponentModel / XmlSerializer conventions, and matched by name
        /// rather than by attribute (see <c>MetaType.ApplyDefaultBehaviour</c>) - so no amount of
        /// attribute inspection would find them. They change both the write guard and the read path,
        /// so a member using one cannot be emitted from the member alone.
        /// </remarks>
        /// <param name="specifiedMember">
        /// Set when the <c>{Name}Specified</c> convention is in use, since that one is also assigned
        /// on read; <c>ShouldSerialize{Name}()</c> affects the write only.
        /// </param>
        /// <remarks>
        /// When a member has both, <c>Specified</c> wins - probed against ref-emit, not assumed.
        /// Both are matched by *name*, so no amount of attribute inspection would find them.
        /// </remarks>
        private static string? GetConditionalPattern(INamedTypeSymbol type, string memberName,
            out string? specifiedMember)
        {
            specifiedMember = null;
            var specified = memberName + "Specified";
            var shouldSerialize = "ShouldSerialize" + memberName;
            string? fallback = null;

            foreach (var symbol in type.GetMembers())
            {
                switch (symbol)
                {
                    // it is assigned on read, so it has to be settable from here
                    case IPropertySymbol property when property.Name == specified
                        && property.Type.SpecialType == SpecialType.System_Boolean
                        && property.DeclaredAccessibility == Accessibility.Public
                        && property.GetMethod is { DeclaredAccessibility: Accessibility.Public }
                        && property.SetMethod is { DeclaredAccessibility: Accessibility.Public }:
                        specifiedMember = specified;
                        return specified;

                    case IMethodSymbol method when method.Name == shouldSerialize
                        && !method.IsStatic && method.Parameters.Length == 0
                        && method.DeclaredAccessibility == Accessibility.Public
                        && method.ReturnType.SpecialType == SpecialType.System_Boolean:
                        fallback = shouldSerialize + "()";
                        continue;
                }
            }
            return fallback;
        }

        private static string AttributeName(AttributeData attribute)
        {
            var name = attribute.AttributeClass?.Name ?? "?";
            const string Suffix = "Attribute";
            return name.Length > Suffix.Length && name.EndsWith(Suffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - Suffix.Length) : name;
        }

        /// <summary>
        /// Does this attribute change how protobuf-net treats the thing it is applied to?
        /// </summary>
        /// <remarks>
        /// Not just the ProtoBuf namespace: <c>MetaType.ApplyDefaultBehaviour</c> also honours
        /// <c>[DataContract]</c>/<c>[DataMember]</c>, the <c>[OnDeserialized]</c> family of
        /// callbacks, the <c>System.Xml.Serialization</c> attributes, <c>[NonSerialized]</c> and
        /// <c>[DefaultValue]</c> - each of which can change the bytes we would emit.
        /// </remarks>
        /// <summary>
        /// Is <c>[DynamicallyAccessedMembers]</c> available to the consumer?
        /// </summary>
        /// <remarks>
        /// Probing for the type rather than assuming a TFM: it ships in the BCL from net5 onwards,
        /// and protobuf-net's own down-level copy is internal, so it would not be usable from the
        /// consumer's assembly even where it exists.
        /// </remarks>
        private static bool SupportsTrimAnnotations(Compilation compilation)
        {
            var type = compilation.GetTypeByMetadataName(
                "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");
            return type is not null && compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);
        }

        /// <summary>
        /// The contracts a type declares as directly-derived, via <c>[ProtoInclude]</c>.
        /// </summary>
        /// <returns>
        /// False if any of them uses a form we cannot reproduce, in which case the caller must
        /// refuse the contract rather than emit a partial hierarchy.
        /// </returns>
        private static bool TryGetSubTypes(INamedTypeSymbol type,
            out List<(int Tag, INamedTypeSymbol Type, bool IsGroup)> subTypes)
        {
            subTypes = new List<(int, INamedTypeSymbol, bool)>();
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoIncludeAttributeName) continue;

                // DataFormat is the one named argument that reaches the wire, and Group is the only
                // value that changes anything - a sub-type is a sub-message, so FixedSize and ZigZag
                // have nothing to select and ref-emit ignores them, as it does elsewhere
                var isGroup = false;
                foreach (var argument in attribute.NamedArguments)
                {
                    if (argument.Key != "DataFormat" || argument.Value.Value is not int format) return false;
                    if (GetDataFormat(format) is not { } parsed) return false;
                    isGroup = parsed == ProtoDataFormat.Group;
                }

                // the (int, string) form defers to runtime type resolution
                if (attribute.ConstructorArguments.Length != 2) return false;
                if (attribute.ConstructorArguments[0].Value is not int tag) return false;
                if (attribute.ConstructorArguments[1].Value is not INamedTypeSymbol derived) return false;

                // An include that does not actually derive from this type is *filtered*, not refused -
                // which is what MetaType.ApplyDefaultBehaviour does (`if (IsValidSubType(knownType))`).
                // Refusing looked equivalent and is not, because the same attribute list is shared by
                // every closed construction of a generic base: `ResourceNode<T>` declaring includes
                // for both `ShipResource : ResourceNode<Ship>` and `SomeResource : ResourceNode<SomeType>`
                // is legal and unambiguous, since each construction sees exactly one of them. Refusing
                // made both of them unlinked, and an unlinked contract is emitted standalone - so the
                // whole enclosing hierarchy silently vanished from the wire rather than failing loudly.
                // Filtering also still yields a compilable set: everything left really does derive.
                if (!DerivesFrom(derived, type)) continue;

                subTypes.Add((tag, derived, isGroup));
            }
            return true;
        }

        /// <summary>
        /// The compatibility level in force for a type: its own attribute, then any inherited from a
        /// base type, then the module's, then the assembly's â€” a port of
        /// <c>TypeCompatibilityHelper.GetTypeCompatibilityLevel</c>. Anything below 200 means 200.
        /// </summary>
        private static int GetCompatibilityLevel(Compilation compilation, INamedTypeSymbol type)
        {
            // Attribute.GetCustomAttribute(type, ..., inherit: true) walks the base types
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (GetDeclaredLevel(current) is { } declared) return declared;
            }
            if (GetDeclaredLevel(compilation.SourceModule) is { } fromModule) return fromModule;
            if (GetDeclaredLevel(compilation.Assembly) is { } fromAssembly) return fromAssembly;
            return 200;
        }

        /// <summary>
        /// The level a symbol declares directly, if any; <c>NotSpecified</c> (zero) counts as absent.
        /// </summary>
        /// <summary>
        /// The value of a <c>[CompatibilityLevel]</c> that protobuf-net would reject, or null when
        /// there is none or it is valid.
        /// </summary>
        /// <remarks>
        /// <c>CompatibilityLevelAttribute.AssertValid</c> admits only <c>NotSpecified</c>, 200, 240
        /// and 300; anything else throws <c>ArgumentOutOfRangeException</c> while building the model.
        /// The attribute takes the enum, so a consumer has to cast to get here — but casting to an
        /// enum is legal C# and the corpus does it, so it is worth checking rather than assuming.
        /// </remarks>
        private static int? InvalidDeclaredLevel(ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != CompatibilityLevelAttributeName) continue;
                if (attribute.ConstructorArguments.Length != 1
                    || attribute.ConstructorArguments[0].Value is not int level)
                {
                    continue;
                }
                if (level is not (0 or 200 or 240 or 300)) return level;
            }
            return null;
        }

        private static int? GetDeclaredLevel(ISymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != CompatibilityLevelAttributeName) continue;
                if (attribute.ConstructorArguments.Length == 1
                    && attribute.ConstructorArguments[0].Value is int level && level > 0)
                {
                    return level;
                }
            }
            return null;
        }

        /// <summary>
        /// <c>ValueMember.GetEffectiveCompatibilityLevel</c>: at or below level 200,
        /// <c>DataFormat.WellKnown</c> promotes the member to 240; above that it means nothing.
        /// </summary>
        private static int GetEffectiveCompatibilityLevel(int level, ProtoDataFormat dataFormat)
            => level > 200 ? level : dataFormat == ProtoDataFormat.WellKnown ? 240 : 200;

        private static bool IsBclKind(ProtoMemberKind kind)
            => kind is ProtoMemberKind.DateTime or ProtoMemberKind.TimeSpan
                or ProtoMemberKind.Guid or ProtoMemberKind.Decimal;

        /// <summary>
        /// Can C# spell this conversion as a cast? protobuf-net resolves <c>op_Implicit</c> or
        /// <c>op_Explicit</c> on either type; asking Roslyn covers exactly that, and rules out the
        /// <c>[ProtoConverter]</c>-method form, which no cast can express.
        /// </summary>
        private static bool CanConvert(Compilation compilation, ITypeSymbol from, ITypeSymbol to)
        {
            var conversion = compilation.ClassifyConversion(from, to);
            return conversion.Exists && (conversion.IsUserDefined || conversion.IsIdentity);
        }

        /// <summary>
        /// A model-level <c>[ProtoSurrogate]</c> declaration: how to serialize a type that cannot
        /// carry <c>[ProtoContract(Surrogate = ...)]</c> itself, such as a BCL type.
        /// </summary>
        private sealed class SurrogateDeclaration
        {
            public SurrogateDeclaration(INamedTypeSymbol surrogate, string? toSurrogate, string? toUnderlying)
            {
                Surrogate = surrogate;
                ToSurrogate = toSurrogate;
                ToUnderlying = toUnderlying;
            }

            public INamedTypeSymbol Surrogate { get; }

            /// <summary>Null when the conversion is a plain cast.</summary>
            public string? ToSurrogate { get; }

            public string? ToUnderlying { get; }
        }

        /// <summary>
        /// The model's <c>[ProtoSurrogate]</c> declarations, keyed by the type being surrogated.
        /// </summary>
        /// <remarks>
        /// Gathered from least to most specific, so the more specific overwrites: a referenced
        /// library's assembly-level offer, then this assembly's, then the model's own. That is what
        /// lets a package ship surrogates for the types it supports - scanning *assembly* attributes
        /// is cheap and bounded, where scanning every type in every reference would not be.
        /// </remarks>
        private static Dictionary<string, SurrogateDeclaration> GetSurrogates(
            Compilation compilation, INamedTypeSymbol model, List<PlanDiagnostic> diagnostics)
        {
            var result = new Dictionary<string, SurrogateDeclaration>(StringComparer.Ordinal);
            foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                Collect(reference.GetAttributes());
            }
            Collect(compilation.Assembly.GetAttributes());
            Collect(model.GetAttributes());
            return result;

            void Collect(IEnumerable<AttributeData> attributes)
            {
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoSurrogateAttributeName) continue;
                if (attribute.ConstructorArguments.Length != 2) continue;
                if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol underlying) continue;
                if (attribute.ConstructorArguments[1].Value is not INamedTypeSymbol surrogate) continue;

                INamedTypeSymbol? converter = null;
                string? toSurrogate = null, toUnderlying = null;
                foreach (var argument in attribute.NamedArguments)
                {
                    switch (argument.Key)
                    {
                        case "Converter" when argument.Value.Value is INamedTypeSymbol declaring:
                            converter = declaring;
                            continue;
                        case "ToSurrogate" when argument.Value.Value is string to:
                            toSurrogate = to;
                            continue;
                        case "ToType" when argument.Value.Value is string from:
                            toUnderlying = from;
                            continue;
                    }
                }

                var at = PlanLocation.From(model);
                var name = Simplify(Qualified(compilation, underlying));
                if ((converter is null) != (toSurrogate is null && toUnderlying is null))
                {
                    Contract(diagnostics, at, name,
                        "[ProtoSurrogate] needs either no converter at all, or a Converter with both "
                        + "ToSurrogate and ToType");
                    continue;
                }

                string? toName = null, fromName = null;
                if (converter is not null)
                {
                    toName = FindConverter(compilation, converter, toSurrogate, underlying, surrogate);
                    fromName = FindConverter(compilation, converter, toUnderlying, surrogate, underlying);
                    if (toName is null || fromName is null)
                    {
                        Contract(diagnostics, at, name,
                            "[ProtoSurrogate] names a converter method that does not exist, is not "
                            + "public and static, or has the wrong signature");
                        continue;
                    }
                }

                result[Qualified(compilation, underlying)]
                    = new SurrogateDeclaration(surrogate, toName, fromName);
            }
            }
        }

        /// <summary>The fully-qualified call, if a matching public static one-argument method exists.</summary>
        private static string? FindConverter(Compilation compilation, INamedTypeSymbol converter, string? methodName,
            ITypeSymbol from, ITypeSymbol to)
        {
            if (methodName is null) return null;
            foreach (var candidate in converter.GetMembers(methodName))
            {
                if (candidate is not IMethodSymbol
                    {
                        IsStatic: true,
                        DeclaredAccessibility: Accessibility.Public,
                        Parameters.Length: 1,
                    } method)
                {
                    continue;
                }
                if (!SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, from)) continue;
                if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, to)) continue;

                return Qualified(compilation, converter) + "." + methodName;
            }
            return null;
        }

        /// <summary>
        /// The expression a *member* of this contract's type must pass as its sub-serializer: null
        /// for the usual case (<c>this</c>), or the hand-written serializer when the contract
        /// declares one, since we never implement <c>ISerializer&lt;T&gt;</c> for those.
        /// </summary>
        private static string? GetSubSerializer(Compilation compilation, INamedTypeSymbol type)
        {
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoContractAttributeName) continue;
                foreach (var argument in attribute.NamedArguments)
                {
                    if (argument.Key == "Serializer" && argument.Value.Value is INamedTypeSymbol external)
                    {
                        // an inaccessible serializer means an inbuilt type - protobuf-net's own
                        // well-known types point at the internal PrimaryTypeProvider. Passing null
                        // lets TypeModel.GetSerializer<T> find it, which is how it resolves anyway.
                        if (!compilation.IsSymbolAccessibleWithin(external, compilation.Assembly))
                        {
                            return "null";
                        }
                        return $"global::ProtoBuf.Serializers.SerializerCache.Get<"
                            + Qualified(compilation, external) + ", "
                            + Qualified(compilation, type) + ">()";
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// The field behind a property, when it can be identified exactly: the compiler-generated
        /// backing field of an auto-property, or the single field a trivial getter returns.
        /// </summary>
        /// <remarks>
        /// This is what lets <c>[UnsafeAccessor]</c> reach a member C# will not let us assign - a
        /// getter-only or <c>init</c> property - by going to the field instead of the accessor.
        /// Anything less than trivial returns null and falls back to the property accessor, since
        /// guessing a field name would silently write to the wrong place.
        /// </remarks>
        private static IFieldSymbol? GetBackingField(INamedTypeSymbol type, IPropertySymbol property,
            CancellationToken cancellationToken)
        {
            // an auto-property: Roslyn hands us the field, so there is nothing to infer
            foreach (var member in type.GetMembers())
            {
                if (member is IFieldSymbol { IsImplicitlyDeclared: true } backing
                    && SymbolEqualityComparer.Default.Equals(backing.AssociatedSymbol, property))
                {
                    return backing;
                }
            }

            // ... otherwise accept only `Foo => _foo;` and `get { return _foo; }`
            var name = GetTrivialGetterField(property, cancellationToken);
            if (name is null) return null;

            foreach (var member in type.GetMembers(name))
            {
                if (member is IFieldSymbol { IsStatic: false, IsConst: false } field
                    && SymbolEqualityComparer.Default.Equals(field.Type, property.Type))
                {
                    return field;
                }
            }
            return null;
        }

        /// <summary>The identifier a trivial getter returns, if that is all it does.</summary>
        private static string? GetTrivialGetterField(IPropertySymbol property, CancellationToken cancellationToken)
        {
            foreach (var reference in property.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax declaration) continue;

                // Foo => _foo;
                if (declaration.ExpressionBody is { Expression: IdentifierNameSyntax arrow })
                {
                    return arrow.Identifier.ValueText;
                }
                if (declaration.AccessorList is not { } accessors) continue;

                foreach (var accessor in accessors.Accessors)
                {
                    if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration)) continue;

                    // get => _foo;
                    if (accessor.ExpressionBody is { Expression: IdentifierNameSyntax shorthand })
                    {
                        return shorthand.Identifier.ValueText;
                    }
                    // get { return _foo; }
                    if (accessor.Body is { Statements: { Count: 1 } statements }
                        && statements[0] is ReturnStatementSyntax { Expression: IdentifierNameSyntax returned })
                    {
                        return returned.Identifier.ValueText;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Is any part of this type still an unsubstituted type parameter?
        /// </summary>
        /// <remarks>
        /// The distinction that matters is open versus closed, not generic versus not:
        /// <c>Wrapper&lt;int&gt;</c> is a perfectly ordinary contract, while <c>Wrapper&lt;T&gt;</c>
        /// cannot be named by the services type. Note this has to recurse — <c>Wrapper&lt;List&lt;
        /// T&gt;&gt;</c> is open too, and an unbound <c>Wrapper&lt;&gt;</c> reports its own type
        /// parameters as its arguments, so it is caught by the same test.
        /// </remarks>
        private static bool ContainsTypeParameter(ITypeSymbol type)
        {
            switch (type)
            {
                case ITypeParameterSymbol:
                    return true;
                case IArrayTypeSymbol array:
                    return ContainsTypeParameter(array.ElementType);
                case INamedTypeSymbol named:
                    for (var current = named; current is not null; current = current.ContainingType)
                    {
                        foreach (var argument in current.TypeArguments)
                        {
                            if (ContainsTypeParameter(argument)) return true;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static bool DerivesFrom(INamedTypeSymbol derived, INamedTypeSymbol baseType)
        {
            for (var current = derived.BaseType; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType)) return true;
            }

            // an interface root is linked by *implementing* it, which is the same relationship as
            // far as [ProtoInclude] is concerned. AllInterfaces, not Interfaces: the implementation
            // may reach the contract interface through another one
            if (baseType.TypeKind == TypeKind.Interface)
            {
                foreach (var candidate in derived.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(candidate, baseType)) return true;
                }
            }
            return false;
        }

        private static bool Implements(INamedTypeSymbol type, string interfaceName)
        {
            foreach (var iface in type.AllInterfaces)
            {
                if (iface.ToDisplayString() == interfaceName) return true;
            }
            return false;
        }

        /// <summary>
        /// The base contract that declares this type as a sub-type, if any. Inheritance without that
        /// declaration is not a hierarchy â€” protobuf-net treats the derived type as its own contract.
        /// </summary>
        // note: no list pattern here - netstandard2.0 has no System.Index, which one would require
        private static INamedTypeSymbol? GetLinkedBase(INamedTypeSymbol type)
        {
            var bases = GetLinkedBases(type);
            return bases.Count == 0 ? null : bases[0];
        }

        /// <summary>
        /// Every base or interface that declares this type as a sub-type. More than one is legal C#
        /// and legal to <em>write</em>, but protobuf-net refuses it — see <see cref="GetLinkedBase"/>'s
        /// callers, which treat a count above one as a dropped contract.
        /// </summary>
        private static List<INamedTypeSymbol> GetLinkedBases(INamedTypeSymbol type)
        {
            var found = new List<INamedTypeSymbol>();
            if (Links(type.BaseType)) found.Add(type.BaseType!);

            // an interface is an inheritance root exactly as a base class is, so *implementing* one
            // that declares [ProtoInclude] for us is the same link. Only the interface that names us
            // counts: a type commonly implements several, and the rest are nothing to do with this
            foreach (var candidate in type.Interfaces)
            {
                if (Links(candidate)) found.Add(candidate);
            }
            return found;

            bool Links(INamedTypeSymbol? baseType)
            {
                if (baseType is not { SpecialType: not SpecialType.System_Object }) return false;
                if (!TryGetSubTypes(baseType, out var subTypes)) return false;

                foreach (var candidate in subTypes)
                {
                    if (SymbolEqualityComparer.Default.Equals(candidate.Type, type)) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// The top of a type's hierarchy: every type in one reads and writes through the root's
        /// <c>ISubTypeSerializer</c>.
        /// </summary>
        private static INamedTypeSymbol GetHierarchyRoot(INamedTypeSymbol type)
        {
            while (GetLinkedBase(type) is { } linked) type = linked;
            return type;
        }

        /// <summary>
        /// Is <c>[UnsafeAccessor]</c> available? It is net8.0 and up, and is the only way generated
        /// code can assign an <c>init</c>-only property.
        /// </summary>
        private static bool SupportsUnsafeAccessor(Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.UnsafeAccessorAttribute")
                is { } type && compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);

        /// <summary>
        /// Is a given <c>RepeatedSerializer</c> factory present in the library being compiled
        /// against? <c>CreateReadOnySet</c> is net6.0-and-up only.
        /// </summary>
        private static bool HasFactory(Compilation compilation, string name)
            => compilation.GetTypeByMetadataName("ProtoBuf.Serializers.RepeatedSerializer")
                is { } repeated && !repeated.GetMembers(name).IsEmpty;

        /// <summary>Map the DataFormat enum's underlying value onto what we can emit.</summary>
        private static ProtoDataFormat? GetDataFormat(int value) => value switch
        {
            // Default and TwosComplement produce byte-identical output for the types we handle
            0 or 2 => ProtoDataFormat.Default,
            1 => ProtoDataFormat.ZigZag,
            3 => ProtoDataFormat.FixedSize,
            4 => ProtoDataFormat.Group,
            5 => ProtoDataFormat.WellKnown,
            _ => null, // anything added later
        };

        private static int? GetNamedInt(AttributeData attribute, string name)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is int value) return value;
            }
            return null;
        }

        private static bool IsSignificantAttribute(AttributeData attribute)
        {
            var type = attribute.AttributeClass;
            if (type is null) return false;

            // these we understand and act on; anything else in the same namespaces still bails,
            // notably the [OnDeserialized] callback family
            switch (type.ToDisplayString())
            {
                // read directly wherever it is relevant, rather than acted on here
                case CompatibilityLevelAttributeName:
                case DataContractAttributeName:
                case DataMemberAttributeName:
                case XmlTypeAttributeName:
                case XmlElementAttributeName:
                case XmlArrayAttributeName:
                case XmlIgnoreAttributeName:
                case NonSerializedAttributeName:
                case ProtoMapAttributeName:
                    return false;
            }

            switch (type.ContainingNamespace?.ToDisplayString())
            {
                case ProtoBufNamespace:
                case "System.Runtime.Serialization":
                case "System.Xml.Serialization":
                    return true;
            }

            return type.ToDisplayString() is "System.NonSerializedAttribute";
        }

        /// <summary>
        /// Render a <c>[DefaultValue]</c> argument as a C# literal of the member's own type, or null
        /// if we cannot do so faithfully.
        /// </summary>
        /// <remarks>
        /// Only the single-argument form is handled: <c>DefaultValue(Type, string)</c> defers to a
        /// TypeConverter at runtime, which we cannot evaluate here.
        /// </remarks>
        private static bool IsNullDefault(AttributeData attribute)
            => attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is null;

        private static string? GetDefaultLiteral(AttributeData attribute, ProtoMemberKind kind,
            string? enumTypeName, INamedTypeSymbol? enumType)
        {
            // the (Type, string) form: DefaultValueAttribute's own constructor runs the string
            // through TypeDescriptor.GetConverter(type).ConvertFromInvariantString and stores the
            // *converted* result in Value. Roslyn does not run constructors, so we see the raw
            // string and do the conversion here - invariant, matching the BCL, which is why "1.5"
            // is unambiguous
            var arguments = attribute.ConstructorArguments;
            var raw = arguments.Length switch
            {
                1 => arguments[0].Value,
                2 when arguments[0].Value is INamedTypeSymbol && arguments[1].Value is string text => text,
                _ => null,
            };
            if (raw is null) return null;

            // for an enum the constant is its underlying integral value, so render the underlying
            // literal and cast it back; parentheses matter for negatives
            if (enumTypeName is not null)
            {
                // ...except that a *string* is parsed by name, not converted: ValueMember's
                // ParseDefaultValue calls Enum.Parse(type, s, ignoreCase: true) before it reaches
                // any numeric conversion. Resolve it to the member's constant and carry on
                if (raw is string byName)
                {
                    if (enumType?.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x
                        => x.HasConstantValue
                        && string.Equals(x.Name, byName, StringComparison.OrdinalIgnoreCase))
                        is not { } named)
                    {
                        return null; // no such name - Enum.Parse would throw, so refuse
                    }
                    raw = named.ConstantValue;
                    if (raw is null) return null;
                }
                var underlying = RenderLiteral(raw, kind);
                return underlying is null ? null : $"({enumTypeName})({underlying})";
            }
            return RenderLiteral(raw, kind);
        }

        /// <summary>
        /// Render a <c>[DefaultValue]</c> constant as a C# literal of the member's own kind, or null
        /// if it has no literal form.
        /// </summary>
        private static string? RenderLiteral(object raw, ProtoMemberKind kind)
        {
            try
            {
                var culture = CultureInfo.InvariantCulture;
                switch (kind)
                {
                    case ProtoMemberKind.Bool: return Convert.ToBoolean(raw, culture) ? "true" : "false";
                    case ProtoMemberKind.SByte: return Convert.ToSByte(raw, culture).ToString(culture);
                    case ProtoMemberKind.Byte: return Convert.ToByte(raw, culture).ToString(culture);
                    case ProtoMemberKind.Int16: return Convert.ToInt16(raw, culture).ToString(culture);
                    case ProtoMemberKind.UInt16: return Convert.ToUInt16(raw, culture).ToString(culture);
                    case ProtoMemberKind.Int32: return Convert.ToInt32(raw, culture).ToString(culture);
                    case ProtoMemberKind.UInt32: return Convert.ToUInt32(raw, culture).ToString(culture) + "U";
                    case ProtoMemberKind.Int64: return Convert.ToInt64(raw, culture).ToString(culture) + "L";
                    case ProtoMemberKind.UInt64: return Convert.ToUInt64(raw, culture).ToString(culture) + "UL";

                    case ProtoMemberKind.Single:
                        var single = Convert.ToSingle(raw, culture);
                        // NaN/infinity have no literal form, and NaN != NaN would break the guard
                        if (float.IsNaN(single) || float.IsInfinity(single)) return null;
                        return single.ToString("R", culture) + "F";

                    case ProtoMemberKind.Double:
                        var @double = Convert.ToDouble(raw, culture);
                        if (double.IsNaN(@double) || double.IsInfinity(@double)) return null;
                        return @double.ToString("R", culture) + "D";

                    case ProtoMemberKind.String:
                        return raw is string text ? SymbolDisplay.FormatLiteral(text, quote: true) : null;

                    // ParseDefaultValue takes s[0] from a string and demands exactly one character,
                    // throwing a FormatException otherwise - so a longer string is a refusal here
                    case ProtoMemberKind.Char:
                        var c = raw switch
                        {
                            string { Length: 1 } oneChar => oneChar[0],
                            string => (char?)null,
                            _ => Convert.ToChar(raw, culture),
                        };
                        return c is null ? null : SymbolDisplay.FormatLiteral(c.Value, quote: true);

                    // nint/nuint: the literal needs the cast, since there is no suffix for them
                    case ProtoMemberKind.IntPtr:
                        return $"(nint)({Convert.ToInt64(raw, culture).ToString(culture)}L)";
                    case ProtoMemberKind.UIntPtr:
                        return $"(nuint)({Convert.ToUInt64(raw, culture).ToString(culture)}UL)";
                }
            }
            catch (Exception)
            {
                return null; // out of range, not convertible, ...
            }
            return null;
        }

        private readonly struct MemberShape
        {
            public MemberShape(ProtoMemberKind kind, bool isNullable = false,
                INamedTypeSymbol? message = null, INamedTypeSymbol? enumType = null,
                ProtoRepeatedPlan repeated = default, string? elementTypeName = null,
                string? declaredTypeName = null, ProtoMapPlan map = default,
                IEnumerable<INamedTypeSymbol>? mapMessages = null)
            {
                DeclaredTypeName = declaredTypeName;
                Kind = kind;
                IsNullable = isNullable;
                Message = message;
                EnumType = enumType;
                Repeated = repeated;
                Map = map;
                MapMessages = mapMessages;
                ElementTypeName = elementTypeName;
            }

            public ProtoMemberKind Kind { get; }
            public bool IsNullable { get; }
            public INamedTypeSymbol? Message { get; }
            public INamedTypeSymbol? EnumType { get; }
            public ProtoRepeatedPlan Repeated { get; }
            public ProtoMapPlan Map { get; }

            /// <summary>
            /// Contracts reached through a map's key or value; the closure has to walk to these, and
            /// unlike every other shape there can be two of them.
            /// </summary>
            public IEnumerable<INamedTypeSymbol>? MapMessages { get; }

            public string? ElementTypeName { get; }
            public string? DeclaredTypeName { get; }
        }

        private static MemberShape? GetMemberShape(Compilation compilation, ITypeSymbol type,
            Dictionary<string, SurrogateDeclaration>? surrogates = null, bool allowParseableTypes = false)
        {
            var isNullable = false;
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            {
                isNullable = true;
                type = nullable.TypeArguments[0];
            }

            if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol { EnumUnderlyingType: { } underlying } enumType)
            {
                // an enum is its underlying scalar plus a cast; [Flags] makes no difference, and
                // [ProtoEnum] only renames for schema purposes - neither affects the wire form
                var kind = GetScalarKind(underlying);

                // the CLR permits char-backed enums even though C# cannot declare one; we have no way
                // to test that shape, so refuse it rather than emit something unverified
                if (kind is null or ProtoMemberKind.Char) return null;
                return new MemberShape(kind.Value, isNullable, enumType: enumType);
            }

            if (GetScalarKind(type) is { } scalar)
            {
                // DateOnly/TimeOnly reach BclHelpers methods that only exist in the net6.0+ build of
                // the library, so the *reference* has to be checked, not just the type's presence -
                // the same reason CreateReadOnySet is probed for
                if (scalar is ProtoMemberKind.DateOnly or ProtoMemberKind.TimeOnly
                    && !SupportsDateOnly(compilation))
                {
                    return null;
                }
                return new MemberShape(scalar, isNullable);
            }

            // ValueMember's order, and it is not the obvious one: the parseable test sits *after* the
            // built-in scalars but *before* contracts, so a [ProtoContract] type that happens to
            // carry a Parse(string) goes on the wire as a string rather than as a message. Placing
            // this later - the tidier-looking option - silently disagrees with ref-emit.
            if (allowParseableTypes && IsParseable(type))
            {
                return new MemberShape(ProtoMemberKind.Parseable, isNullable,
                    declaredTypeName: Qualified(compilation, type));
            }

            // erase tuple element names before anything looks at the type: they are decoration, and
            // leaving them on would key the same shape twice and emit a duplicate ISerializer<>
            type = EraseTupleNames(compilation, type);

            // a struct contract can be Nullable<T>; a reference-type one cannot
            if (GetMessageKind(compilation, type, surrogates, out var message) is { } messageKind)
            {
                if (isNullable && message is not { IsValueType: true }) return null;
                return new MemberShape(messageKind, isNullable, message: message);
            }

            // "bytes" is a built-in scalar, so it has to be settled *before* the tuple test rather
            // than after it. ArraySegment<byte> is the one that shows why: it has a
            // (T[], int, int) constructor and matching read-only Array/Offset/Count properties, so it
            // satisfies the auto-tuple predicate exactly - and was being emitted as a three-member
            // message, writing Offset and Count unconditionally as an auto-tuple does. Note the rank
            // check inside IsBytesLike, since byte[,] is not bytes.
            if (!isNullable && IsBytesLike(type))
            {
                return new MemberShape(ProtoMemberKind.Bytes);
            }

            // a tuple-typed member is a sub-message too, even though it carries no contract attribute
            if (type is INamedTypeSymbol candidate && IsTupleCandidate(candidate))
            {
                if (isNullable && !candidate.IsValueType) return null;
                return new MemberShape(ProtoMemberKind.Message, isNullable, message: candidate);
            }

            if (isNullable) return null; // nothing else below here is a value type

            // collections: the element is analysed exactly as a standalone member would be, and the
            // resulting shape *describes the element* - Repeated says how it is stored
            if (ResolveRepeated(type) is { Factory: not null, Element: not null } repeated)
            {
                if (repeated.IsMap) return AsMap(compilation, repeated, type, surrogates, allowParseableTypes);

                // IReadOnlySet<T> support is conditional on the runtime the library was built for
                if (repeated.Factory == "CreateReadOnySet" && !HasFactory(compilation, "CreateReadOnySet")) return null;

                return AsRepeated(compilation, repeated.Element, repeated.AsRepeatedPlan(), type, surrogates, allowParseableTypes);
            }
            return null;
        }

        /// <summary>
        /// One entry from the provider table in <c>RepeatedSerializers</c>' static constructor.
        /// </summary>
        private readonly struct RepeatedProvider
        {
            public RepeatedProvider(string metadata, string? factory, bool exactOnly,
                bool takesCollectionType, bool dropsCollectionTypeWhenExact = false, bool isMap = false)
            {
                Metadata = metadata;
                Factory = factory;
                ExactOnly = exactOnly;
                TakesCollectionType = takesCollectionType;
                DropsCollectionTypeWhenExact = dropsCollectionTypeWhenExact;
                IsMap = isMap;
            }

            /// <summary>A <c>MapSerializer</c> factory rather than a <c>RepeatedSerializer</c> one.</summary>
            public bool IsMap { get; }

            /// <summary>e.g. <c>System.Collections.Generic.List`1</c>.</summary>
            public string Metadata { get; }

            /// <summary>The <c>RepeatedSerializer</c> factory; null for the map shapes, which we refuse.</summary>
            public string? Factory { get; }

            /// <summary>Applies only to the member's own type, not to something it inherits or implements.</summary>
            public bool ExactOnly { get; }

            /// <summary><c>Create{X}&lt;TCollection, TElement&gt;()</c> rather than <c>Create{X}&lt;TElement&gt;()</c>.</summary>
            public bool TakesCollectionType { get; }

            /// <summary><c>List&lt;T&gt;</c> alone gets the one-arg factory; anything derived from it does not.</summary>
            public bool DropsCollectionTypeWhenExact { get; }
        }

        /// <summary>
        /// The provider table from <c>RepeatedSerializers</c>' static constructor, in registration
        /// order - order <em>is</em> priority, lower wins.
        /// </summary>
        /// <remarks>
        /// Reproduced rather than approximated. Both the ordering and the exact-only flags are
        /// load-bearing: the immutable family is registered ahead of the mutable lookalikes so that
        /// it wins on types that implement both, and <c>SortedSet&lt;T&gt;</c> lands on
        /// <c>CreateEnumerable</c> - not <c>CreateSet</c> - precisely because the
        /// <c>ISet&lt;T&gt;</c> registration is exact-only and so does not apply through an interface.
        /// </remarks>
        private static readonly RepeatedProvider[] s_repeatedProviders =
        {
            new("System.Collections.Generic.List`1", "CreateList", false, true, true),

            // the immutable set, deliberately ahead of everything that looks like it
            new("System.Collections.Immutable.ImmutableArray`1", "CreateImmutableArray", true, false),
            new("System.Collections.Immutable.ImmutableDictionary`2", "CreateImmutableDictionary", true, false, isMap: true),
            new("System.Collections.Immutable.ImmutableSortedDictionary`2", "CreateImmutableSortedDictionary", true, false, isMap: true),
            new("System.Collections.Immutable.IImmutableDictionary`2", "CreateIImmutableDictionary", true, false, isMap: true),
            new("System.Collections.Immutable.ImmutableList`1", "CreateImmutableList", true, false),
            new("System.Collections.Immutable.IImmutableList`1", "CreateImmutableIList", true, false),
            new("System.Collections.Immutable.ImmutableHashSet`1", "CreateImmutableHashSet", true, false),
            new("System.Collections.Immutable.ImmutableSortedSet`1", "CreateImmutableSortedSet", true, false),
            new("System.Collections.Immutable.IImmutableSet`1", "CreateImmutableISet", true, false),
            new("System.Collections.Immutable.ImmutableQueue`1", "CreateImmutableQueue", true, false),
            new("System.Collections.Immutable.IImmutableQueue`1", "CreateImmutableIQueue", true, false),
            new("System.Collections.Immutable.ImmutableStack`1", "CreateImmutableStack", true, false),
            new("System.Collections.Immutable.IImmutableStack`1", "CreateImmutableIStack", true, false),

            // the concurrent set
            new("System.Collections.Concurrent.ConcurrentDictionary`2", "CreateConcurrentDictionary", false, true, isMap: true),
            new("System.Collections.Concurrent.ConcurrentBag`1", "CreateConcurrentBag", false, true),
            new("System.Collections.Concurrent.ConcurrentQueue`1", "CreateConcurrentQueue", false, true),
            new("System.Collections.Concurrent.ConcurrentStack`1", "CreateConcurrentStack", false, true),
            new("System.Collections.Concurrent.IProducerConsumerCollection`1", "CreateIProducerConsumerCollection", false, true),

            // pretty normal stuff
            new("System.Collections.Generic.Dictionary`2", "CreateDictionary", false, true, dropsCollectionTypeWhenExact: true, isMap: true),
            new("System.Collections.Generic.IDictionary`2", "CreateDictionary", false, true, isMap: true),
            new("System.Collections.Generic.IReadOnlyDictionary`2", "CreateIReadOnlyDictionary", true, false, isMap: true),
            new("System.Collections.Generic.Queue`1", "CreateQueue", false, true),
            new("System.Collections.Generic.Stack`1", "CreateStack", false, true),
            new("System.Collections.Generic.HashSet`1", "CreateSet", true, true),
            new("System.Collections.Generic.ISet`1", "CreateSet", true, true),
            new("System.Collections.Generic.IReadOnlySet`1", "CreateReadOnySet", true, false),

            // the fallback, which is why nearly anything enumerable is a collection
            new("System.Collections.Generic.IEnumerable`1", "CreateEnumerable", false, true),
        };

        /// <summary>
        /// Shapes that resolve to a serializer which throws at runtime; we refuse them up-front.
        /// </summary>
        private static readonly string[] s_notSupportedFlavors =
        {
            "System.Span`1", "System.ReadOnlySpan`1", "System.Buffers.ReadOnlySequence`1",
            "System.ReadOnlyMemory`1", "System.Memory`1", "System.ArraySegment`1",
            "System.Buffers.IMemoryOwner`1",
        };

        /// <summary>What <c>TryGetRepeatedProvider</c> found for a type.</summary>
        private readonly struct RepeatedMatch
        {
            public RepeatedMatch(string? factory, bool takesCollectionType, bool isValueType,
                ITypeSymbol? element, ITypeSymbol? value = null)
            {
                Factory = factory;
                TakesCollectionType = takesCollectionType;
                IsValueType = isValueType;
                Element = element;
                Value = value;
            }

            public string? Factory { get; }

            public bool TakesCollectionType { get; }

            public bool IsValueType { get; }

            /// <summary>The element type; for a map, the *key*.</summary>
            public ITypeSymbol? Element { get; }

            /// <summary>A map's value type; null for everything else, which is what marks a map.</summary>
            public ITypeSymbol? Value { get; }

            public bool IsMap => Value is not null;

            public ProtoRepeatedPlan AsRepeatedPlan() => new(Factory, TakesCollectionType, IsValueType);
        }

        /// <summary>
        /// A port of <c>RepeatedSerializers.TryGetRepeatedProvider</c>: walk the base-type chain and
        /// then the interfaces, keeping the lowest-priority match, and treat a tie between two
        /// different resolutions as "not a collection at all".
        /// </summary>
        /// <remarks>
        /// This is the single decision behind both questions we need to answer - which factory a
        /// member's collection uses, and whether a contract is list-like - so they cannot drift apart.
        /// </remarks>
        private static RepeatedMatch? ResolveRepeated(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String) return null;
            if (IsBytesLike(type)) return null; // "bytes", not a repeated byte

            if (type is IArrayTypeSymbol array)
            {
                // vectors only: byte[] is handled above, and byte[,] has no vector type to match
                return array.Rank == 1
                    ? new RepeatedMatch("CreateVector", false, false, array.ElementType)
                    : null;
            }
            if (type is not INamedTypeSymbol root) return null;
            if (root.IsGenericType && Array.IndexOf(s_notSupportedFlavors, MetadataName(root)) >= 0) return null;

            int bestPriority = int.MaxValue, best = -1;
            ITypeSymbol? bestElement = null, bestValue = null;
            bool ambiguous = false;

            for (var current = root; current is not null && current.SpecialType != SpecialType.System_Object;
                current = current.BaseType)
            {
                Consider(current);
            }
            foreach (var iface in root.AllInterfaces) Consider(iface);

            if (ambiguous || best < 0) return null;

            var found = s_repeatedProviders[best];
            // List<T> alone gets the one-arg factory; a type *derived* from it still needs both args
            var exact = MetadataName(root) == found.Metadata;
            var takesCollectionType = found.TakesCollectionType && !(found.DropsCollectionTypeWhenExact && exact);
            return new RepeatedMatch(found.Factory, takesCollectionType, root.IsValueType, bestElement, bestValue);

            void Consider(INamedTypeSymbol current)
            {
                if (!current.IsGenericType) return;
                var metadata = MetadataName(current);
                for (int i = 0; i < s_repeatedProviders.Length; i++)
                {
                    var candidate = s_repeatedProviders[i];
                    if (candidate.Metadata != metadata) continue;
                    if (i > bestPriority) return;
                    if (candidate.ExactOnly && !SymbolEqualityComparer.Default.Equals(root, current)) return;

                    // a map's "element" is its key, with the value alongside
                    var arguments = current.TypeArguments;
                    var element = arguments.Length == 0 ? null : arguments[0];
                    var value = candidate.IsMap && arguments.Length == 2 ? arguments[1] : null;
                    if (i < bestPriority)
                    {
                        bestPriority = i;
                        best = i;
                        bestElement = element;
                        bestValue = value;
                        ambiguous = false;
                    }
                    // the same registration reached twice - IEnumerable<int> *and* IEnumerable<string>,
                    // say - resolves to two different serializers, which ref-emit treats as no match
                    else if (!SymbolEqualityComparer.Default.Equals(element, bestElement)
                        || !SymbolEqualityComparer.Default.Equals(value, bestValue))
                    {
                        ambiguous = true;
                    }
                    return;
                }
            }

        }

        /// <summary>e.g. <c>System.Collections.Generic.List`1</c>, matching the runtime table's keys.</summary>
        private static string MetadataName(INamedTypeSymbol type)
        {
            var definition = type.OriginalDefinition;
            var ns = definition.ContainingNamespace;
            return ns is null or { IsGlobalNamespace: true }
                ? definition.MetadataName
                : ns.ToDisplayString() + "." + definition.MetadataName;
        }

        private static bool IsBytesLike(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte }) return true;
            return type is INamedTypeSymbol { TypeArguments.Length: 1 } named
                && named.TypeArguments[0].SpecialType == SpecialType.System_Byte
                && MetadataName(named) is "System.Memory`1" or "System.ReadOnlyMemory`1" or "System.ArraySegment`1";
        }

        /// <summary>
        /// A dictionary member: the same merge shape as a collection, but with two element types and
        /// their wire types passed alongside.
        /// </summary>
        private static MemberShape? AsMap(Compilation compilation, RepeatedMatch match, ITypeSymbol declared,
            Dictionary<string, SurrogateDeclaration>? surrogates, bool allowParseableTypes)
        {
            var key = match.Element!;
            var value = match.Value!;
            if (GetMemberShape(compilation, key, surrogates, allowParseableTypes) is not { } keyShape) return null;
            if (GetMemberShape(compilation, value, surrogates, allowParseableTypes) is not { } valueShape) return null;

            // An enum on either side resolves an ISerializer<TEnum> *from the model* rather than
            // taking one inline, exactly as a repeated enum does - so it needs the same
            // ISerializerProxy<TEnum> on the services type, and the plan carries the enum's name to
            // ask for one. The wire type is the underlying scalar's, which KeyKind/ValueKind already
            // hold, and the serializer argument stays absent: passing nothing lands on
            // `serializer ??= TypeModel.GetSerializer<T>(Model)`, which finds the proxy.

            // a nested collection is legal on a dictionary specifically (ref-emit's
            // TestIfNestedNotSupported exempts maps), and is served by an ISerializer<TCollection>
            // resolved from the model - so the *value* may be one, and the services type exposes a
            // proxy for it. A nested *key* has no reference, so it stays refused.
            if (keyShape.Repeated.Factory is not null || keyShape.Map.Factory is not null) return null;

            // a nested value - repeated or map - is served by an ISerializer<TCollection> resolved
            // from the model, so all the plan needs is the factory that produces one. Both
            // RepeatedSerializer and MapSerializer implement IRepeatedSerializer<TCollection>,
            // which is an ISerializer<TCollection>, so the two cases differ only in the rendering
            var valueTypeName = Qualified(compilation, value);
            string? valueFactory = null;
            if (valueShape.Repeated.Factory is not null)
            {
                valueFactory = RepeatedFactory(valueShape.Repeated, valueTypeName, valueShape.ElementTypeName!);
            }
            else if (valueShape.Map.Factory is not null)
            {
                valueFactory = MapFactory(valueShape.Map, valueTypeName);
            }

            // the key is part of the wire identity, so a nullable one has no meaning
            if (keyShape.IsNullable) return null;

            var messages = new List<INamedTypeSymbol>();
            if (keyShape.Message is { } keyMessage) messages.Add(keyMessage);
            if (valueShape.Message is { } valueMessage) messages.Add(valueMessage);

            var map = new ProtoMapPlan(match.Factory!, match.TakesCollectionType,
                keyShape.Kind, Qualified(compilation, key),
                valueShape.Kind, Qualified(compilation, value),
                IsValidProtobufMap(keyShape, valueShape), valueFactory,
                keyEnumTypeName: keyShape.EnumType is null ? null : Qualified(compilation, keyShape.EnumType),
                valueEnumTypeName: valueShape.EnumType is null ? null : Qualified(compilation, valueShape.EnumType));

            return new MemberShape(ProtoMemberKind.Map, map: map, mapMessages: messages,
                declaredTypeName: Qualified(compilation, declared));
        }

        /// <summary>
        /// Is this expressible as a protobuf <c>map</c>? A port of
        /// <c>RepeatedSerializerStub.IsValidProtobufMap</c>: the key must be an integral, string or
        /// enum type, and the value must not itself be repeated. When it is not, protobuf-net adds
        /// <c>OptionFailOnDuplicateKey</c>.
        /// </summary>
        /// <summary>
        /// Re-decide <see cref="ProtoMapPlan.IsValidProtobufMap"/> now that the compatibility level
        /// and the key's own format are known — neither is available where the shape is resolved.
        /// </summary>
        /// <remarks>
        /// <c>IsValidKey</c> takes both: from level 300 a <c>Guid</c> key is valid, because it goes
        /// on the wire as a string — but not under <c>DataFormat.FixedSize</c>, which selects the
        /// 16-byte form. Getting this wrong adds <c>OptionFailOnDuplicateKey</c>, which changes
        /// reading from <c>SetValues</c> to <c>AddRange</c>, so it is a real behavioural difference
        /// rather than a cosmetic flag.
        /// </remarks>
        private static ProtoMapPlan WithLevelledKey(ProtoMapPlan map, int level, ProtoDataFormat keyFormat)
        {
            if (map.IsValidProtobufMap || map.KeyKind != ProtoMemberKind.Guid) return map;
            if (level < 300 || keyFormat == ProtoDataFormat.FixedSize) return map;
            return new ProtoMapPlan(map.Factory!, map.TakesCollectionType,
                map.KeyKind, map.KeyTypeName!, map.ValueKind, map.ValueTypeName!,
                isValidProtobufMap: true, map.ValueSerializerFactory,
                // carried through: this rebuild only fires for a Guid key, but the *value* may still
                // be an enum, and dropping its name here would silently lose the proxy
                map.KeyEnumTypeName, map.ValueEnumTypeName);
        }

        private static bool IsValidProtobufMap(MemberShape key, MemberShape value)
        {
            // a Guid key is *also* valid from level 300, but neither the level nor the key's format
            // is known here - WithLevelledKey re-decides it once they are. Note that bool, char and
            // the floating-point types are not in this list at any level
            var validKey = key.EnumType is not null || (!key.IsNullable && key.Kind switch
            {
                ProtoMemberKind.String or ProtoMemberKind.SByte or ProtoMemberKind.Int16
                    or ProtoMemberKind.Int32 or ProtoMemberKind.Int64 or ProtoMemberKind.Byte
                    or ProtoMemberKind.UInt16 or ProtoMemberKind.UInt32 or ProtoMemberKind.UInt64 => true,
                _ => false,
            });
            return validKey && value.Repeated.Factory is null && value.Map.Factory is null;
        }

        /// <summary>
        /// The <c>RepeatedSerializer</c> factory call for a collection, rendered here because a map
        /// with a repeated value needs it as the value serializer rather than at a member site.
        /// </summary>
        private static string RepeatedFactory(ProtoRepeatedPlan repeated, string declared, string element)
            => $"global::ProtoBuf.Serializers.RepeatedSerializer.{repeated.Factory}"
                + (repeated.TakesCollectionType ? $"<{declared}, {element}>()" : $"<{element}>()");

        /// <summary>The <c>MapSerializer</c> equivalent, for a dictionary nested as a map value.</summary>
        private static string MapFactory(ProtoMapPlan map, string declared)
            => $"global::ProtoBuf.Serializers.MapSerializer.{map.Factory}"
                + (map.TakesCollectionType
                    ? $"<{declared}, {map.KeyTypeName}, {map.ValueTypeName}>()"
                    : $"<{map.KeyTypeName}, {map.ValueTypeName}>()");

        private static MemberShape? AsRepeated(Compilation compilation, ITypeSymbol element,
            ProtoRepeatedPlan repeated, ITypeSymbol declared,
            Dictionary<string, SurrogateDeclaration>? surrogates, bool allowParseableTypes)
        {
            // nested collections would need a repeated-of-repeated shape, which the plan cannot carry
            if (GetMemberShape(compilation, element, surrogates, allowParseableTypes) is not { Repeated.Factory: null } shape) return null;

            // a nullable element is an ordinary element as far as the encoding goes - it only throws
            // at runtime if a null actually turns up, unless [NullWrappedValue] is on the member,
            // which is what makes a null expressible. That holds for every element kind: a nullable
            // enum resolves its serializer through the model's ISerializerProxy<TEnum?> exactly as a
            // non-nullable one does, and a nullable BCL element needs no sub-serializer at all.

            // an enum element is *not* written inline: RepeatedSerializer resolves an
            // ISerializer<TEnum> from the model, so the services type exposes one via
            // ISerializerProxy<TEnum> (see EmitEnumProxies)
            return new MemberShape(shape.Kind, message: shape.Message, enumType: shape.EnumType,
                repeated: repeated,
                elementTypeName: Qualified(compilation, element),
                declaredTypeName: Qualified(compilation, declared));
        }

        private static ProtoMemberKind? GetMessageKind(Compilation compilation, ITypeSymbol type,
            Dictionary<string, SurrogateDeclaration>? surrogates, out INamedTypeSymbol? message)
        {
            message = null;
            if (type is not INamedTypeSymbol named) return null;

            // anything carrying a contract attribute counts as reachable, supported or not; whether
            // it can actually be handled is decided when the closure gets to it.
            // Note this is the *family*, not just [ProtoContract]: MetaType.GetContractFamily treats
            // [DataContract] and [XmlType] as contract markers in their own right, and the seeded
            // path here already does too - so recognising only [ProtoContract] made the same type
            // emittable as a seed and unsupported as a member. Probed on both ref-emit paths
            if (HasContractFamily(named))
            {
                message = named;
                return ProtoMemberKind.Message;
            }

            // ... and so does a type the *model* surrogates, which is how something like System.Uri
            // becomes serializable despite never being able to carry the attribute itself
            if (surrogates is not null
                && surrogates.ContainsKey(Qualified(compilation, named)))
            {
                message = named;
                return ProtoMemberKind.Message;
            }
            return null;
        }

        /// <summary>
        /// A type protobuf-net can round-trip as a string: <c>ToString()</c> out, <c>Parse</c> back.
        /// </summary>
        /// <remarks>
        /// A port of <c>ParseableSerializer.TryCreate</c>, and the details are load-bearing. It wants
        /// <c>Parse</c> and <b>not</b> <c>TryParse</c>; declared on the type itself, so an inherited
        /// one does not count; taking exactly one <c>string</c> and returning the type. A <b>value
        /// type</b> additionally needs its own <c>ToString()</c> override — a struct that inherits
        /// <c>object.ToString()</c> would round-trip its type name, which is the "fools" case guarded
        /// against there.
        /// </remarks>
        private static bool IsParseable(ITypeSymbol type)
        {
            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct)) return false;

            var found = false;
            foreach (var member in type.GetMembers("Parse"))
            {
                // note: no list pattern here - netstandard2.0 has no System.Index for it to lower to
                if (member is IMethodSymbol { IsStatic: true, DeclaredAccessibility: Accessibility.Public } method
                    && method.Parameters.Length == 1
                    && method.Parameters[0].Type.SpecialType == SpecialType.System_String
                    && SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
            if (!type.IsValueType) return true;

            foreach (var member in type.GetMembers("ToString"))
            {
                if (member is IMethodSymbol
                    {
                        IsStatic: false, DeclaredAccessibility: Accessibility.Public, Parameters.IsEmpty: true,
                        ReturnType.SpecialType: SpecialType.System_String,
                    })
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Does the referenced protobuf-net carry the <c>DateOnly</c>/<c>TimeOnly</c> helpers?
        /// </summary>
        /// <remarks>
        /// <c>BclHelpers.ReadDateOnly</c> is inside <c>#if NET6_0_OR_GREATER</c>, so a consumer on a
        /// lower TFM has the language type but not the method to call. Note the golden tests compile
        /// against the <b>netstandard2.0</b> BuildTools assembly, so this is false there — which is
        /// why the fixture for these lives beside the differential suite rather than the goldens.
        /// </remarks>
        private static bool SupportsDateOnly(Compilation compilation)
            => compilation.GetTypeByMetadataName("ProtoBuf.BclHelpers")
                ?.GetMembers("ReadDateOnly").Length > 0;

        /// <summary>
        /// The tags <c>[ProtoContract(ImplicitFields = …)]</c> assigns, by member name.
        /// </summary>
        /// <remarks>
        /// A port of the numbering in <c>MetaType.ApplyDefaultBehaviour</c>, and it cannot be done
        /// member-by-member because the tags come from sorting the whole set. The rules, confirmed
        /// against ref-emit rather than inferred: <c>AllPublic</c> takes any public member (a
        /// property counts when its <em>getter</em> is public, whatever the setter is),
        /// <c>AllFields</c> takes any field public or not; members sort by <b>ordinal name</b>, not
        /// declaration order; numbering runs from <c>ImplicitFirstTag</c>; and a member carrying an
        /// explicit <c>[ProtoMember]</c> keeps its pinned tag and does <b>not</b> consume a
        /// sequential number — nor is that number avoided, so 5 pinned alongside 1, 2 is normal.
        /// </remarks>
        /// <remarks>
        /// The type-level exclusions and pins have to be applied <em>here</em>, not only in the
        /// member loop: tags come from sorting the whole candidate set, so a name wrongly left in it
        /// does not merely serialize itself — it shifts every unpinned tag after it.
        /// </remarks>
        private static Dictionary<string, int> GetImplicitTags(
            INamedTypeSymbol type, int implicitMode, int implicitFirstTag,
            HashSet<string>? partialIgnores, Dictionary<string, PartialMember>? partialMembers)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (implicitMode == 0) return result;

            const int AllPublic = 1, AllFields = 2;
            var candidates = new List<(int Tag, string Name)>();

            foreach (var symbol in type.GetMembers())
            {
                if (symbol.IsStatic) continue;

                // AllFields means *any* field, including an auto-property's compiler-generated
                // backing field - so `Ignored { get; set; }` is serialized as
                // `<Ignored>k__BackingField`, which sorts before every ordinary name because '<'
                // precedes letters. Surprising, but it is what RuntimeTypeModel does.
                if (symbol.IsImplicitlyDeclared && !(implicitMode == 2 && symbol is IFieldSymbol)) continue;

                // [ProtoPartialIgnore] excludes by name from the type, and excluding it only from the
                // read/write loop leaves it consuming a tag here
                if (partialIgnores is not null && partialIgnores.Contains(symbol.Name)) continue;

                var pinned = 0;
                var ignored = false;
                foreach (var attribute in symbol.GetAttributes())
                {
                    switch (attribute.AttributeClass?.ToDisplayString())
                    {
                        case ProtoIgnoreAttributeName:
                            ignored = true;
                            break;
                        case ProtoMemberAttributeName when attribute.ConstructorArguments.Length >= 1
                            && attribute.ConstructorArguments[0].Value is int tag:
                            pinned = tag;
                            break;
                    }
                }
                if (ignored) continue;

                // ...and [ProtoPartialMember] pins from the type exactly as [ProtoMember] pins from
                // the member, so it too is kept out of the sequential run
                if (pinned <= 0 && partialMembers is not null
                    && partialMembers.TryGetValue(symbol.Name, out var partial) && partial.FieldNumber > 0)
                {
                    pinned = partial.FieldNumber;
                }

                var forced = symbol switch
                {
                    IFieldSymbol { IsConst: false } field => implicitMode == AllFields
                        || (implicitMode == AllPublic && field.DeclaredAccessibility == Accessibility.Public),
                    IPropertySymbol property => implicitMode == AllPublic
                        && property.GetMethod is { DeclaredAccessibility: Accessibility.Public },
                    _ => false,
                };
                if (forced || pinned > 0) candidates.Add((pinned, symbol.Name));
            }

            candidates.Sort(static (x, y) =>
            {
                var byTag = x.Tag.CompareTo(y.Tag);
                return byTag != 0 ? byTag : string.CompareOrdinal(x.Name, y.Name);
            });

            var next = implicitFirstTag;
            foreach (var candidate in candidates)
            {
                if (candidate.Tag > 0) continue; // pinned by [ProtoMember], and left alone
                result[candidate.Name] = next++;
            }
            return result;
        }

        /// <summary>
        /// Which serialization callback an attribute denotes, if any.
        /// </summary>
        /// <remarks>
        /// The two families are honoured identically by <c>MetaType</c>; they differ only in that the
        /// <c>System.Runtime.Serialization</c> spelling takes a <c>StreamingContext</c>.
        /// </remarks>
        private static ProtoCallbackKind? GetCallbackKind(AttributeData attribute)
            => attribute.AttributeClass?.ToDisplayString() switch
            {
                "ProtoBuf.ProtoBeforeSerializationAttribute"
                    or "System.Runtime.Serialization.OnSerializingAttribute" => ProtoCallbackKind.BeforeSerialize,
                "ProtoBuf.ProtoAfterSerializationAttribute"
                    or "System.Runtime.Serialization.OnSerializedAttribute" => ProtoCallbackKind.AfterSerialize,
                "ProtoBuf.ProtoBeforeDeserializationAttribute"
                    or "System.Runtime.Serialization.OnDeserializingAttribute" => ProtoCallbackKind.BeforeDeserialize,
                "ProtoBuf.ProtoAfterDeserializationAttribute"
                    or "System.Runtime.Serialization.OnDeserializedAttribute" => ProtoCallbackKind.AfterDeserialize,
                _ => null,
            };

        /// <summary>
        /// Can generated code call this callback directly? It must be a public, non-static, void
        /// method taking either nothing or a <c>StreamingContext</c>.
        /// </summary>
        /// <remarks>
        /// <c>MetaType</c> accepts a wider set of signatures (and reaches non-public ones by
        /// reflection); anything outside this subset is refused rather than mis-called.
        /// </remarks>
        private static bool IsUsableCallback(IMethodSymbol method, ProtoCallbackKind kind, out bool takesContext)
        {
            takesContext = false;
            if (method.IsStatic || method.DeclaredAccessibility != Accessibility.Public) return false;
            if (!method.ReturnsVoid || method.IsGenericMethod) return false;

            switch (method.Parameters.Length)
            {
                case 0:
                    return true;
                case 1 when method.Parameters[0].Type.ToDisplayString()
                    == "System.Runtime.Serialization.StreamingContext":
                    takesContext = true;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// An enum seeded as a contract: all that is needed is its underlying scalar, which picks
        /// the <c>EnumSerializer.Create{X}</c> overload.
        /// </summary>
        private static ProtoEnumPlan? GetEnumPlan(Compilation compilation, INamedTypeSymbol type)
        {
            if (type.EnumUnderlyingType is not { } underlying) return null;
            if (GetScalarKind(underlying) is not { } kind) return null;

            // the CLR permits char-backed enums even though C# cannot declare one; there is no way
            // to test that shape from C#, so it is refused here as it is for a member
            if (kind == ProtoMemberKind.Char) return null;

            return new ProtoEnumPlan(Qualified(compilation, type), kind);
        }

        private static ProtoMemberKind? GetScalarKind(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean: return ProtoMemberKind.Bool;
                case SpecialType.System_SByte: return ProtoMemberKind.SByte;
                case SpecialType.System_Byte: return ProtoMemberKind.Byte;
                case SpecialType.System_Int16: return ProtoMemberKind.Int16;
                case SpecialType.System_UInt16: return ProtoMemberKind.UInt16;
                case SpecialType.System_Int32: return ProtoMemberKind.Int32;
                case SpecialType.System_UInt32: return ProtoMemberKind.UInt32;
                case SpecialType.System_Int64: return ProtoMemberKind.Int64;
                case SpecialType.System_UInt64: return ProtoMemberKind.UInt64;
                case SpecialType.System_Single: return ProtoMemberKind.Single;
                case SpecialType.System_Double: return ProtoMemberKind.Double;
                case SpecialType.System_Char: return ProtoMemberKind.Char;
                case SpecialType.System_String: return ProtoMemberKind.String;
                case SpecialType.System_DateTime: return ProtoMemberKind.DateTime;
                case SpecialType.System_Decimal: return ProtoMemberKind.Decimal;
                case SpecialType.System_IntPtr: return ProtoMemberKind.IntPtr;
                case SpecialType.System_UIntPtr: return ProtoMemberKind.UIntPtr;
            }

            // the rest have no SpecialType, so they go by name. DateOnly and TimeOnly exist only on
            // net6.0+, which needs no probing here: a consumer below that has no such type to match
            return type.ToDisplayString() switch
            {
                "System.TimeSpan" => ProtoMemberKind.TimeSpan,
                "System.Uri" => ProtoMemberKind.Uri,
                "System.Guid" => ProtoMemberKind.Guid,
                "System.DateOnly" => ProtoMemberKind.DateOnly,
                "System.TimeOnly" => ProtoMemberKind.TimeOnly,
                _ => (ProtoMemberKind?)null,
            };
        }
    }
}
