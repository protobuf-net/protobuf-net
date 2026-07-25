#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        private const string ProtoSurrogateAttributeName = "ProtoBuf.ProtoSurrogateAttribute";
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
            var surrogates = GetSurrogates(model, diagnostics);
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

                var contract = ParseContract(compilation, type, diagnostics, surrogates, out var reachable, cancellationToken);
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
                plan = new ProtoModelPlan(nameSpace, model.Name, new(contracts),
                    annotateTrimming: SupportsTrimAnnotations(compilation));
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
                        : contract.SurrogateTypeName is { } surrogate && !parsed.ContainsKey(surrogate)
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
            out List<INamedTypeSymbol> reachable,
            CancellationToken cancellationToken)
        {
            reachable = new List<INamedTypeSymbol>();

            var at = PlanLocation.From(type);
            var name = type.ToDisplayString();

            // a model-level [ProtoSurrogate] stands in for the contract attribute entirely: the type
            // being surrogated need not - and for a BCL type, cannot - carry one, so this has to be
            // resolved before any of the "is this even a contract" checks
            surrogates.TryGetValue(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                out var declaredSurrogate);

            // auto-tuple detection applies only when the type carries no contract family at all
            // (MetaType.GetContractFamily), and has to be tried before the shape checks below -
            // a tuple is commonly a closed generic, which they would otherwise reject
            if (declaredSurrogate is null && !HasContractFamily(type))
            {
                if (ParseTuple(compilation, type, diagnostics, out reachable, cancellationToken) is { } tuple) return tuple;
                reachable = new List<INamedTypeSymbol>();
                return Contract(diagnostics, at, name,
                    "the type is not marked [ProtoContract], [DataContract] or [XmlType], and is not a tuple");
            }

            var isValueType = type.TypeKind == TypeKind.Struct;
            if (!isValueType && type.TypeKind != TypeKind.Class)
            {
                return Contract(diagnostics, at, name, "only classes and structs are supported");
            }
            if (type.IsGenericType) return Contract(diagnostics, at, name, "generic types are not supported");
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
            var linkedBase = GetLinkedBase(type);

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
                if (type.IsAbstract && subTypes.Count == 0)
                {
                    return Contract(diagnostics, at, name, "abstract types are not supported");
                }
                // note the constructor check is deferred: with a surrogate it is the *surrogate* that
                // gets constructed, which is exactly what lets an immutable type be surrogated

                // Extensible is the documented way to get the extension interfaces, and declares no
                // serializable members of its own, so it is not the silent-loss case below
                if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType
                    && linkedBase is null && baseType.ToDisplayString() != ExtensibleTypeName)
                {
                    // protobuf-net would treat this as a standalone contract that silently ignores
                    // its inherited members; refusing is the safer half of that surprise
                    return Contract(diagnostics, at, name,
                        "it derives from a type that does not declare [ProtoInclude] for it");
                }
            }

            var surrogateType = declaredSurrogate?.Surrogate;
            bool isContract = declaredSurrogate is not null;
            bool isDataContract = false, isXmlType = false, skipConstructor = false;
            var ignoreListHandling = false;
            var dataMemberOffset = 0;
            foreach (var attribute in type.GetAttributes())
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
                            // opts the type *out* of list handling, which is what lets a list-like
                            // contract be serialized as an ordinary message
                            case "IgnoreListHandling" when argument.Value.Value is bool ignoreList:
                                ignoreListHandling = ignoreList;
                                continue;
                            // schema naming only: neither reaches the wire format
                            case "Name":
                            case "Origin":
                                continue;
                            case "Surrogate" when argument.Value.Value is INamedTypeSymbol declared:
                                surrogateType = declared;
                                continue;
                        }
                        return Option(diagnostics, at, name, $"[ProtoContract({argument.Key} = ...)]");
                    }
                    isContract = true;
                }
                // already read up-front, since it decides whether inheritance is legal here
                else if (attributeName == ProtoIncludeAttributeName) { }
                // reserved field ranges exist to shape the generated .proto; nothing on the wire
                else if (attributeName == ProtoReservedAttributeName) { }
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
                    "the type is not marked [ProtoContract], [DataContract] or [XmlType]");
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

                // it is a contract in its own right, and ref-emit emits a serializer for it too
                reachable.Add(surrogateType);
                memberSource = surrogateType;
                isValueType = surrogateType.IsValueType;
            }

            // whatever actually gets constructed on read: the surrogate when there is one, which is
            // what lets an immutable type be surrogated at all
            if (!isValueType && !memberSource.IsAbstract && !memberSource.InstanceConstructors.Any(
                static ctor => ctor.Parameters.Length == 0
                    && ctor.DeclaredAccessibility == Accessibility.Public))
            {
                return Contract(diagnostics, at, name, "there is no public parameterless constructor");
            }

            var members = new List<ProtoMemberPlan>();
            foreach (var symbol in memberSource.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // serialization callbacks ([OnDeserialized] and friends) live on methods
                if (symbol is IMethodSymbol { IsImplicitlyDeclared: false, AssociatedSymbol: null } method)
                {
                    if (method.GetAttributes().FirstOrDefault(IsSignificantAttribute) is { } onMethod)
                    {
                        return Option(diagnostics, PlanLocation.From(method), name,
                            $"[{AttributeName(onMethod)}] on methods");
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
                    case IFieldSymbol { IsImplicitlyDeclared: false } field:
                        memberType = field.Type;
                        break;
                    default:
                        continue;
                }

                var atMember = PlanLocation.From(symbol);
                int? fieldNumber = null, dataMemberOrder = null, xmlOrder = null;
                bool ignored = false, isPacked = false, overwriteList = false, isRequired = false;
                bool usesAccessor = false, isReadOnly = false;
                bool wrappedValue = false, wrappedValueGroup = false;
                bool wrappedCollection = false, wrappedCollectionGroup = false;
                var dataFormat = ProtoDataFormat.Default;
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

                if ((isPacked || overwriteList) && GetMemberShape(compilation, memberType, surrogates)
                    is not ({ Repeated.Factory: not null } or { Map.Factory: not null }))
                {
                    // both options only mean anything for a collection
                    return Option(diagnostics, atMember, name,
                        "[ProtoMember(IsPacked/OverwriteList)] on a non-collection member");
                }

                // precedence, per MetaType.ApplyDefaultBehaviour: [ProtoMember] first, then
                // [DataMember(Order)] - to which the offset applies - then [XmlElement]/[XmlArray],
                // to which it does not. An order below 1 means "not declared" (DataMember.Order
                // defaults to -1, and 0 is not a valid protobuf field number).
                fieldNumber ??= isDataContract && dataMemberOrder >= 1
                    ? dataMemberOrder + dataMemberOffset : null;
                fieldNumber ??= isXmlType && xmlOrder >= 1 ? xmlOrder : null;
                if (fieldNumber is null) continue;

                if (symbol is IFieldSymbol { IsConst: true })
                {
                    return Member(diagnostics, atMember, name, symbol.Name, "is a constant");
                }
                if (symbol.IsStatic) return Member(diagnostics, atMember, name, symbol.Name, "is static");
                if (symbol.DeclaredAccessibility != Accessibility.Public)
                {
                    return Member(diagnostics, atMember, name, symbol.Name, "is not public");
                }
                switch (symbol)
                {
                    case IPropertySymbol property:
                        if (property.GetMethod is not { DeclaredAccessibility: Accessibility.Public })
                        {
                            return Member(diagnostics, atMember, name, symbol.Name, "has no public getter");
                        }
                        if (property.SetMethod is null)
                        {
                            // no setter at all is fine: the read still runs, and a collection or
                            // sub-message is populated by mutating the instance it already holds
                            isReadOnly = true;
                        }
                        else if (property.SetMethod.DeclaredAccessibility != Accessibility.Public)
                        {
                            // ref-emit's *compiled* path refuses these ("cannot apply changes to
                            // property"), apparently to stay verifiable; its runtime path reaches
                            // them by reflection. [UnsafeAccessor] lets us do neither - a deliberate
                            // divergence rather than a match
                            if (!SupportsUnsafeAccessor(compilation))
                            {
                                return Member(diagnostics, atMember, name, symbol.Name,
                                    "has a non-public setter, which needs [UnsafeAccessor] (net8.0 or later)");
                            }
                            usesAccessor = true;
                        }
                        // an init-only setter can only be reached via [UnsafeAccessor], which is
                        // net8.0 and up; below that there is no way to assign it at all
                        if (property.SetMethod is { IsInitOnly: true })
                        {
                            if (!SupportsUnsafeAccessor(compilation))
                            {
                                return Member(diagnostics, atMember, name, symbol.Name,
                                    "has an init-only setter, which needs [UnsafeAccessor] (net8.0 or later)");
                            }
                            usesAccessor = true;
                        }
                        break;

                    // a readonly field cannot be assigned after construction, so it has the same
                    // problem an init-only property does
                    case IFieldSymbol { IsReadOnly: true }:
                        return Member(diagnostics, atMember, name, symbol.Name, "is read-only");
                }

                if (GetConditionalPattern(memberSource, symbol.Name) is { } conditional)
                {
                    return Member(diagnostics, atMember, name, symbol.Name,
                        $"is conditional via '{conditional}'");
                }

                if (GetMemberShape(compilation, memberType, surrogates) is not { } shape)
                {
                    return Member(diagnostics, atMember, name, symbol.Name,
                        $"has unsupported type '{memberType.ToDisplayString()}'");
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
                    if (kind is ProtoMemberKind.Message or ProtoMemberKind.Map
                        or ProtoMemberKind.DateTime or ProtoMemberKind.TimeSpan
                        or ProtoMemberKind.Guid or ProtoMemberKind.Decimal)
                    {
                        return Option(diagnostics, atMember, name,
                            "[NullWrappedValue] on a non-scalar");
                    }
                    // a reference-type scalar is already nullable; a value type has to say so
                    if (!shape.IsNullable && kind is not (ProtoMemberKind.String or ProtoMemberKind.Bytes))
                    {
                        return Option(diagnostics, atMember, name,
                            "[NullWrappedValue] on a non-nullable value");
                    }
                    if (declaredDefault is not null)
                    {
                        return Option(diagnostics, atMember, name, "[NullWrappedValue] with [DefaultValue]");
                    }
                }
                // a map is repeated too, and wraps exactly as a collection does
                if (wrappedCollection && !isCollection && !isMap)
                {
                    return Option(diagnostics, atMember, name, "[NullWrappedCollection] on a non-collection");
                }

                // the compatibility level chooses the encoding for the four BCL types, and nothing
                // else; resolving it for every member would be wasted work
                var compatibilityLevel = 200;
                if (IsBclKind(kind))
                {
                    compatibilityLevel = GetEffectiveCompatibilityLevel(
                        GetDeclaredLevel(symbol) ?? GetCompatibilityLevel(compilation, memberSource), dataFormat);

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
                // on anything else WellKnown has nothing to promote, and ref-emit simply ignores it
                // needed for the [UnsafeAccessor] signature, and as the type argument to ReadAny/WriteAny
                var declaredTypeName = shape.DeclaredTypeName ?? (usesAccessor || wrappedValue
                    ? memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null);
                var isNullable = shape.IsNullable;
                var enumTypeName = shape.EnumType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                string? defaultLiteral = null;
                // a null declared default means "no declared default", exactly as ref-emit treats it
                if (declaredDefault is not null && !IsNullDefault(declaredDefault))
                {
                    if (kind == ProtoMemberKind.Message)
                    {
                        return Option(diagnostics, atMember, name, "[DefaultValue] on a message member");
                    }
                    defaultLiteral = GetDefaultLiteral(declaredDefault, kind, enumTypeName);
                    if (defaultLiteral is null)
                    {
                        return Option(diagnostics, atMember, name, "this form of [DefaultValue]");
                    }
                }

                if (shape.Map.Factory is not null)
                {
                    // a map can reach a contract through its key *and* its value
                    foreach (var reached in shape.MapMessages!) reachable.Add(reached);
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, symbol.Name, kind,
                        declaredTypeName: declaredTypeName, map: shape.Map,
                        isPacked: isPacked, overwriteList: overwriteList, wrappedValue: wrappedValue, wrappedValueGroup: wrappedValueGroup, wrappedCollection: wrappedCollection, wrappedCollectionGroup: wrappedCollectionGroup,
                        dataFormat: dataFormat, isRequired: isRequired, usesAccessor: usesAccessor, compatibilityLevel: compatibilityLevel, isReadOnly: isReadOnly));
                }
                else if (kind == ProtoMemberKind.Message)
                {
                    // enqueued even if it turns out to be unsupported, so that it reports its own
                    // reason and this contract is dropped by cascade with a message that chains
                    reachable.Add(message!);
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, symbol.Name, kind,
                        message!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        isNullable: isNullable, messageIsValueType: message.IsValueType,
                        repeated: shape.Repeated, elementTypeName: shape.ElementTypeName,
                        declaredTypeName: declaredTypeName,
                        isPacked: isPacked, overwriteList: overwriteList, wrappedValue: wrappedValue, wrappedValueGroup: wrappedValueGroup, wrappedCollection: wrappedCollection, wrappedCollectionGroup: wrappedCollectionGroup,
                        dataFormat: dataFormat, isRequired: isRequired, usesAccessor: usesAccessor, compatibilityLevel: compatibilityLevel, isReadOnly: isReadOnly));
                }
                else
                {
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, symbol.Name, kind,
                        defaultLiteral: defaultLiteral, isNullable: isNullable,
                        enumTypeName: enumTypeName,
                        repeated: shape.Repeated, elementTypeName: shape.ElementTypeName,
                        declaredTypeName: declaredTypeName,
                        isPacked: isPacked, overwriteList: overwriteList, wrappedValue: wrappedValue, wrappedValueGroup: wrappedValueGroup, wrappedCollection: wrappedCollection, wrappedCollectionGroup: wrappedCollectionGroup,
                        dataFormat: dataFormat, isRequired: isRequired, usesAccessor: usesAccessor, compatibilityLevel: compatibilityLevel, isReadOnly: isReadOnly));
                }
            }

            // note there is no "it has no members" refusal: an empty message is entirely legal
            // protobuf, and .proto-generated DTOs are full of them
            members.Sort(static (x, y) => x.FieldNumber.CompareTo(y.FieldNumber));

            // the whole hierarchy has to be in the model, since every type in it routes through the
            // root; walking both ways gets there from whichever end was seeded
            string? rootTypeName = null;
            var subTypePlans = new ProtoSubTypePlan[subTypes.Count];
            if (subTypes.Count != 0 || linkedBase is not null)
            {
                rootTypeName = GetHierarchyRoot(type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (linkedBase is not null) reachable.Add(linkedBase);
                for (int i = 0; i < subTypes.Count; i++)
                {
                    subTypePlans[i] = new ProtoSubTypePlan(subTypes[i].Tag,
                        subTypes[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    reachable.Add(subTypes[i].Type);
                }
            }

            return new ProtoContractPlan(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                new(members.ToArray()), isValueType, skipConstructor, isSealed: memberSource.IsSealed,
                rootTypeName: rootTypeName, subTypes: new(subTypePlans), extensible: extensible,
                surrogateTypeName: surrogateType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                toSurrogate: declaredSurrogate?.ToSurrogate, toUnderlying: declaredSurrogate?.ToUnderlying);
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

        /// <summary>
        /// The <c>{Name}Specified</c> / <c>ShouldSerialize{Name}()</c> conventions, if present.
        /// </summary>
        /// <remarks>
        /// Inherited from the System.ComponentModel / XmlSerializer conventions, and matched by name
        /// rather than by attribute (see <c>MetaType.ApplyDefaultBehaviour</c>) - so no amount of
        /// attribute inspection would find them. They change both the write guard and the read path,
        /// so a member using one cannot be emitted from the member alone.
        /// </remarks>
        private static string? GetConditionalPattern(INamedTypeSymbol type, string memberName)
        {
            var specified = memberName + "Specified";
            var shouldSerialize = "ShouldSerialize" + memberName;

            foreach (var symbol in type.GetMembers())
            {
                switch (symbol)
                {
                    case IPropertySymbol property when property.Name == specified
                        && property.Type.SpecialType == SpecialType.System_Boolean:
                        return specified;

                    case IMethodSymbol method when method.Name == shouldSerialize
                        && !method.IsStatic && method.Parameters.Length == 0
                        && method.ReturnType.SpecialType == SpecialType.System_Boolean:
                        return shouldSerialize + "()";
                }
            }
            return null;
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
        private static bool TryGetSubTypes(INamedTypeSymbol type, out List<(int Tag, INamedTypeSymbol Type)> subTypes)
        {
            subTypes = new List<(int, INamedTypeSymbol)>();
            foreach (var attribute in type.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() != ProtoIncludeAttributeName) continue;

                // the (int, string) form defers to runtime type resolution, and DataFormat.Group
                // changes the sub-type framing
                if (attribute.NamedArguments.Length != 0) return false;
                if (attribute.ConstructorArguments.Length != 2) return false;
                if (attribute.ConstructorArguments[0].Value is not int tag) return false;
                if (attribute.ConstructorArguments[1].Value is not INamedTypeSymbol derived) return false;

                subTypes.Add((tag, derived));
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
        private static Dictionary<string, SurrogateDeclaration> GetSurrogates(
            INamedTypeSymbol model, List<PlanDiagnostic> diagnostics)
        {
            var result = new Dictionary<string, SurrogateDeclaration>(StringComparer.Ordinal);
            foreach (var attribute in model.GetAttributes())
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
                var name = Simplify(underlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
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
                    toName = FindConverter(converter, toSurrogate, underlying, surrogate);
                    fromName = FindConverter(converter, toUnderlying, surrogate, underlying);
                    if (toName is null || fromName is null)
                    {
                        Contract(diagnostics, at, name,
                            "[ProtoSurrogate] names a converter method that does not exist, is not "
                            + "public and static, or has the wrong signature");
                        continue;
                    }
                }

                result[underlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)]
                    = new SurrogateDeclaration(surrogate, toName, fromName);
            }
            return result;
        }

        /// <summary>The fully-qualified call, if a matching public static one-argument method exists.</summary>
        private static string? FindConverter(INamedTypeSymbol converter, string? methodName,
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

                return converter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + methodName;
            }
            return null;
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
        private static INamedTypeSymbol? GetLinkedBase(INamedTypeSymbol type)
        {
            if (type.BaseType is not { SpecialType: not SpecialType.System_Object } baseType) return null;
            if (!TryGetSubTypes(baseType, out var subTypes)) return null;

            foreach (var candidate in subTypes)
            {
                if (SymbolEqualityComparer.Default.Equals(candidate.Type, type)) return baseType;
            }
            return null;
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

        private static string? GetDefaultLiteral(AttributeData attribute, ProtoMemberKind kind, string? enumTypeName)
        {
            if (attribute.ConstructorArguments.Length != 1) return null;
            if (attribute.ConstructorArguments[0].Value is not object raw) return null;

            // for an enum the constant is its underlying integral value, so render the underlying
            // literal and cast it back; parentheses matter for negatives
            if (enumTypeName is not null)
            {
                var underlying = GetDefaultLiteral(attribute, kind, enumTypeName: null);
                return underlying is null ? null : $"({enumTypeName})({underlying})";
            }

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
            Dictionary<string, SurrogateDeclaration>? surrogates = null)
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

            if (GetScalarKind(type) is { } scalar) return new MemberShape(scalar, isNullable);

            // erase tuple element names before anything looks at the type: they are decoration, and
            // leaving them on would key the same shape twice and emit a duplicate ISerializer<>
            type = EraseTupleNames(compilation, type);

            // a struct contract can be Nullable<T>; a reference-type one cannot
            if (GetMessageKind(type, surrogates, out var message) is { } messageKind)
            {
                if (isNullable && message is not { IsValueType: true }) return null;
                return new MemberShape(messageKind, isNullable, message: message);
            }

            // a tuple-typed member is a sub-message too, even though it carries no contract attribute
            if (type is INamedTypeSymbol candidate && IsTupleCandidate(candidate))
            {
                if (isNullable && !candidate.IsValueType) return null;
                return new MemberShape(ProtoMemberKind.Message, isNullable, message: candidate);
            }

            if (isNullable) return null; // nothing else below here is a value type

            // byte[] is a bytes field, not a repeated byte; note the rank check, since byte[,] is not
            if (type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte })
            {
                return new MemberShape(ProtoMemberKind.Bytes);
            }

            // collections: the element is analysed exactly as a standalone member would be, and the
            // resulting shape *describes the element* - Repeated says how it is stored
            if (ResolveRepeated(type) is { Factory: not null, Element: not null } repeated)
            {
                if (repeated.IsMap) return AsMap(compilation, repeated, type, surrogates);

                // IReadOnlySet<T> support is conditional on the runtime the library was built for
                if (repeated.Factory == "CreateReadOnySet" && !HasFactory(compilation, "CreateReadOnySet")) return null;

                return AsRepeated(compilation, repeated.Element, repeated.AsRepeatedPlan(), type, surrogates);
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
            Dictionary<string, SurrogateDeclaration>? surrogates)
        {
            var key = match.Element!;
            var value = match.Value!;
            if (GetMemberShape(compilation, key, surrogates) is not { } keyShape) return null;
            if (GetMemberShape(compilation, value, surrogates) is not { } valueShape) return null;

            // an enum on either side resolves an ISerializer<TEnum> from the model, which the
            // services type does not expose - the same reason a repeated enum is refused
            if (keyShape.EnumType is not null || valueShape.EnumType is not null) return null;

            // a nested collection is legal for ref-emit (it allows nesting on dictionaries alone) but
            // needs a repeated serializer resolved from the model, so it goes the same way
            if (keyShape.Repeated.Factory is not null || valueShape.Repeated.Factory is not null) return null;
            if (keyShape.Map.Factory is not null || valueShape.Map.Factory is not null) return null;

            // the key is part of the wire identity, so a nullable one has no meaning
            if (keyShape.IsNullable) return null;

            var messages = new List<INamedTypeSymbol>();
            if (keyShape.Message is { } keyMessage) messages.Add(keyMessage);
            if (valueShape.Message is { } valueMessage) messages.Add(valueMessage);

            var map = new ProtoMapPlan(match.Factory!, match.TakesCollectionType,
                keyShape.Kind, key.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                valueShape.Kind, value.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsValidProtobufMap(keyShape, valueShape));

            return new MemberShape(ProtoMemberKind.Map, map: map, mapMessages: messages,
                declaredTypeName: declared.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        /// <summary>
        /// Is this expressible as a protobuf <c>map</c>? A port of
        /// <c>RepeatedSerializerStub.IsValidProtobufMap</c>: the key must be an integral, string or
        /// enum type, and the value must not itself be repeated. When it is not, protobuf-net adds
        /// <c>OptionFailOnDuplicateKey</c>.
        /// </summary>
        private static bool IsValidProtobufMap(MemberShape key, MemberShape value)
        {
            // Guid keys are valid from compatibility level 300, which we do not support yet; note
            // that bool, char and the floating-point types are *not* in the list
            var validKey = key.EnumType is not null || (!key.IsNullable && key.Kind switch
            {
                ProtoMemberKind.String or ProtoMemberKind.SByte or ProtoMemberKind.Int16
                    or ProtoMemberKind.Int32 or ProtoMemberKind.Int64 or ProtoMemberKind.Byte
                    or ProtoMemberKind.UInt16 or ProtoMemberKind.UInt32 or ProtoMemberKind.UInt64 => true,
                _ => false,
            });
            return validKey && value.Repeated.Factory is null && value.Map.Factory is null;
        }

        private static MemberShape? AsRepeated(Compilation compilation, ITypeSymbol element,
            ProtoRepeatedPlan repeated, ITypeSymbol declared,
            Dictionary<string, SurrogateDeclaration>? surrogates)
        {
            // nested collections would need a repeated-of-repeated shape, which the plan cannot carry
            if (GetMemberShape(compilation, element, surrogates) is not { Repeated.Factory: null } shape) return null;

            // a nullable *scalar* element is an ordinary element as far as the encoding goes - it
            // only throws at runtime if a null actually turns up, unless [NullWrappedValue] is on
            // the member. The other kinds have no reference, so they stay refused.
            if (shape.IsNullable && (shape.EnumType is not null || shape.Message is not null
                || IsBclKind(shape.Kind)))
            {
                return null;
            }

            // an enum element is *not* written inline: RepeatedSerializer resolves an
            // ISerializer<TEnum> from the model, so the services type exposes one via
            // ISerializerProxy<TEnum> (see EmitEnumProxies)
            return new MemberShape(shape.Kind, message: shape.Message, enumType: shape.EnumType,
                repeated: repeated,
                elementTypeName: element.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                declaredTypeName: declared.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        private static ProtoMemberKind? GetMessageKind(ITypeSymbol type,
            Dictionary<string, SurrogateDeclaration>? surrogates, out INamedTypeSymbol? message)
        {
            message = null;
            if (type is not INamedTypeSymbol named) return null;

            // anything marked [ProtoContract] counts as reachable, supported or not; whether it can
            // actually be handled is decided when the closure gets to it
            if (named.GetAttributes().Any(static a
                    => a.AttributeClass?.ToDisplayString() == ProtoContractAttributeName))
            {
                message = named;
                return ProtoMemberKind.Message;
            }

            // ... and so does a type the *model* surrogates, which is how something like System.Uri
            // becomes serializable despite never being able to carry the attribute itself
            if (surrogates is not null
                && surrogates.ContainsKey(named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
            {
                message = named;
                return ProtoMemberKind.Message;
            }
            return null;
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
            }

            // TimeSpan and Guid have no SpecialType, so they go by name
            return type.ToDisplayString() switch
            {
                "System.TimeSpan" => ProtoMemberKind.TimeSpan,
                "System.Guid" => ProtoMemberKind.Guid,
                _ => (ProtoMemberKind?)null,
            };
        }
    }
}
