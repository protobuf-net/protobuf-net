#nullable enable
using Google.Protobuf.Reflection;
using ProtoBuf.BuildTools.Internal.Aot;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;

namespace ProtoBuf.BuildTools.Internal
{
    /// <summary>
    /// How a schema's DTOs were told to handle unknown sub-types, mirroring
    /// protobuf-net.Reflection's <c>UnknownSubTypeHandling</c>.
    /// </summary>
    /// <remarks>
    /// It lives here rather than on either generator because BOTH halves need it and neither owns
    /// it: the value is read from the DTO generator's own item metadata (<c>ProtoBuf_SubTypes</c>)
    /// and threaded into the model plan. That is the whole point - the two generated halves must
    /// not be able to disagree, and they cannot if there is one key and one answer.
    /// </remarks>
    internal enum SchemaSubTypes
    {
        /// <summary>Emit the runtime check, as protobuf-net always has.</summary>
        Default = 0,
        /// <summary>The DTOs are <c>sealed</c>; a sub-type cannot exist, so the check is dead.</summary>
        Sealed = 1,
        /// <summary>The DTOs carry <c>IgnoreUnknownSubTypes</c>; the check is suppressed.</summary>
        Ignore = 2,
    }

    /// <summary>
    /// Builds AOT plans directly from a parsed <c>.proto</c> schema, rather than from Roslyn
    /// symbols — see <c>notes/aot-schema-model.md</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **This is a spike**: it covers scalars, enums and nested messages, and nothing else. It
    /// exists to establish that the plan can be built from the schema at all, and that the emitted
    /// model compiles against the DTOs the *other* generator emits. Breadth (repeated, map,
    /// oneof, extensions, groups, proto2 defaults) is deliberately not here yet.
    /// </para>
    /// <para>
    /// The whole difficulty of this direction is that the plan describes the C# that
    /// <see cref="CSharpCodeGenerator"/> is about to emit, and the two must not drift. Every
    /// convention this relies on is called out with a CONVENTION comment, and each one is a place
    /// that would break silently if protogen changed. The eventual answer is a shared codegen
    /// model rather than a second implementation of the same decisions; until then,
    /// <c>SchemaSourcedModelSpikeTests</c> compiles both halves together, which is what catches
    /// a drift.
    /// </para>
    /// </remarks>
    internal static class SchemaPlanBuilder
    {
        /// <summary>
        /// Projects every message in the set into a contract plan. Returns null if anything in
        /// the set is outside the spike's scope, since a partial model is worse than none.
        /// </summary>
        internal static ProtoModelPlan? TryBuild(FileDescriptorSet set, NameNormalizer names,
            string? modelNamespace, string modelTypeName, out string? unsupported,
            SchemaSubTypes subTypes = SchemaSubTypes.Default)
        {
            unsupported = null;
            var contracts = new List<ProtoContractPlan>();
            var enums = new List<ProtoEnumPlan>();

            // FIRST PASS: every declared type, keyed by its full schema name (".pkg.Outer.Inner"),
            // mapped to the C# name protogen will emit for it. A type reference in a field arrives
            // in schema terms, so resolving it by re-deriving names segment by segment would be a
            // second implementation of protogen's naming; a lookup built by the same walk cannot
            // disagree with itself
            var types = new Dictionary<string, string>(StringComparer.Ordinal);
            var mapEntries = new Dictionary<string, DescriptorProto>(StringComparer.Ordinal);
            foreach (var file in set.Files)
            {
                // NOTE: every file, INCLUDING imports. A field may reference a type from an
                // imported schema, and that reference has to resolve to whatever C# name that
                // file's own generation produces. Indexing only IncludeInOutput files made such a
                // reference a KeyNotFoundException thrown out of a source generator - found by the
                // corpus on its first run, against google/protobuf/timestamp.proto
                var ns = Namespace(file, names);
                var package = string.IsNullOrEmpty(file.Package) ? "" : "." + file.Package;
                foreach (var message in file.MessageTypes) IndexMessage(message, ns, package, null, names, types, mapEntries);
                foreach (var @enum in file.EnumTypes)
                {
                    types[package + "." + @enum.Name] = Qualify(ns, names.GetName(@enum));
                }
            }

            // SECOND PASS: the contracts themselves
            foreach (var file in set.Files)
            {
                if (!file.IncludeInOutput) continue;
                var ns = Namespace(file, names);

                foreach (var message in file.MessageTypes)
                {
                    if (!AddMessage(message, ns, null, names, types, mapEntries, contracts, ref unsupported, subTypes, IsProto2(file))) return null;
                }

                // an enum reached as a MEMBER is an inline scalar and needs no plan of its own; it
                // would only become a ProtoEnumPlan as a model root, which this path never produces
            }

            // NOT a refusal: a schema of only enums, services or extensions legitimately has no
            // messages, and contributes no contracts. Refusing produced a spurious diagnostic on
            // 14 of the corpus's 268 schemas, which is a warning telling the consumer off for
            // something entirely valid

            return new ProtoModelPlan(modelNamespace, modelTypeName,
                new EquatableArray<ProtoContractPlan>(contracts.ToArray()),
                enums: new EquatableArray<ProtoEnumPlan>(enums.ToArray()));
        }

        /// <summary>
        /// Adds a message and everything nested inside it.
        /// </summary>
        /// <remarks>
        /// CONVENTION: protogen emits a nested message as a nested C# class, so
        /// <c>Outer.Inner</c> - the enclosing chain is part of the name, which is what
        /// <paramref name="outer"/> carries.
        /// <para>
        /// A <c>map&lt;k,v&gt;</c> compiles to a SYNTHETIC nested entry message
        /// (<c>options.map_entry</c>) plus a repeated field of it, and protogen emits a
        /// <c>Dictionary&lt;K,V&gt;</c> and <b>no C# type</b> for the entry. So the entry must be
        /// skipped here: walking it would emit a contract for a type that does not exist, which
        /// is a build break in the consumer's project.
        /// </para>
        /// </remarks>
        private static bool AddMessage(DescriptorProto message, string? ns, string? outer,
            NameNormalizer names, Dictionary<string, string> types,
            Dictionary<string, DescriptorProto> mapEntries,
            List<ProtoContractPlan> contracts, ref string? unsupported, SchemaSubTypes subTypes, bool proto2)
        {
            // the synthetic map-entry type: real to the descriptor, absent from the C#
            if (message.Options?.MapEntry == true) return true;

            var localName = names.GetName(message);
            var qualified = outer is null ? localName : outer + "." + localName;

            var contract = TryBuildMessage(message, ns, qualified, names, types, mapEntries, ref unsupported, subTypes, proto2);
            if (contract is null) return false;
            contracts.Add(contract);

            foreach (var nested in message.NestedTypes)
            {
                if (!AddMessage(nested, ns, qualified, names, types, mapEntries, contracts, ref unsupported, subTypes, proto2)) return false;
            }
            return true;
        }

        /// <summary>
        /// The first pass: record what C# name each declared type will be emitted as.
        /// </summary>
        /// <remarks>
        /// Two chains are tracked, and conflating them is the easy mistake: the SCHEMA chain
        /// (<c>.pkg.Outer.Inner</c>, which is how a field's type reference is spelled) and the
        /// C# chain (<c>Outer.Inner</c>, after normalisation). They differ at every segment.
        /// </remarks>
        private static void IndexMessage(DescriptorProto message, string? ns, string schemaOuter,
            string? csOuter, NameNormalizer names, Dictionary<string, string> types,
            Dictionary<string, DescriptorProto> mapEntries)
        {
            var entrySchemaName = schemaOuter + "." + message.Name;
            if (message.Options?.MapEntry == true)
            {
                // no C# TYPE is emitted for a map entry - protogen emits a Dictionary<K,V> - but the
                // entry descriptor is still the only place the key and value types are written down,
                // so it is indexed separately rather than discarded
                mapEntries[entrySchemaName] = message;
                return;
            }

            var schemaName = schemaOuter + "." + message.Name;
            var localName = names.GetName(message);
            var csName = csOuter is null ? localName : csOuter + "." + localName;

            types[schemaName] = Qualify(ns, csName);

            foreach (var nested in message.NestedTypes)
            {
                IndexMessage(nested, ns, schemaName, csName, names, types, mapEntries);
            }
            foreach (var @enum in message.EnumTypes)
            {
                types[schemaName + "." + @enum.Name] = Qualify(ns, csName + "." + names.GetName(@enum));
            }
        }

        /// <summary>
        /// CONVENTION: the package becomes the namespace through the same normalizer the DTO
        /// generator uses; an empty package means the global namespace.
        /// </summary>
        private static string? Namespace(FileDescriptorProto file, NameNormalizer names)
        {
            var ns = names.GetName(file);
            return string.IsNullOrWhiteSpace(ns) ? null : ns;
        }

        private static ProtoContractPlan? TryBuildMessage(DescriptorProto message, string? ns,
            string qualifiedName, NameNormalizer names, Dictionary<string, string> types,
            Dictionary<string, DescriptorProto> mapEntries, ref string? unsupported, SchemaSubTypes subTypes, bool proto2)
        {
            var typeName = Qualify(ns, qualifiedName);
            var members = new List<ProtoMemberPlan>();

            foreach (var field in message.Fields)
            {
                if (field.label == FieldDescriptorProto.Label.LabelRepeated)
                {
                    var repeated = TryBuildRepeated(message, field, types, mapEntries, names, ref unsupported);
                    if (repeated is null) return null;
                    members.Add(repeated.Value);
                    continue;
                }

                var kind = MapKind(field, out var enumOrMessage, out var dataFormat);
                if (kind is null)
                {
                    unsupported = $"{message.Name}.{field.Name}: type {field.type} is outside the spike";
                    return null;
                }

                var name = names.GetName(field);
                string? typeNameForMember = null, enumTypeName = null, defaultLiteral = null;

                if (field.type is FieldDescriptorProto.Type.TypeEnum or FieldDescriptorProto.Type.TypeMessage)
                {
                    // NEVER index directly: an unresolvable reference - a well-known type, or a
                    // shape the index skipped - must refuse with a diagnostic rather than throw
                    // KeyNotFoundException out of a source generator, which is the worst failure
                    // mode available (it takes the consumer's build with it)
                    if (!types.TryGetValue(enumOrMessage!, out var resolved))
                    {
                        unsupported = $"{message.Name}.{field.Name}: the type {enumOrMessage} could "
                            + "not be resolved (an imported or well-known type?)";
                        return null;
                    }
                    // an enum member is its underlying scalar plus a cast, so the plan carries the
                    // enum's name and the kind is the underlying scalar (proto enums are int32)
                    if (field.type == FieldDescriptorProto.Type.TypeEnum) enumTypeName = resolved;
                    else typeNameForMember = resolved;
                }
                else if (field.type == FieldDescriptorProto.Type.TypeString)
                {
                    // CONVENTION: a proto3 string member is emitted with [DefaultValue("")] AND an
                    // `= ""` initialiser, which moves the write guard from `!= null` to `!= ""`.
                    // Getting this wrong is a silent wire difference, not a build break.
                    defaultLiteral = "\"\"";
                }

                members.Add(new ProtoMemberPlan(field.Number, name, kind.Value,
                    typeName: typeNameForMember,
                    enumTypeName: enumTypeName,
                    defaultLiteral: defaultLiteral,
                    dataFormat: dataFormat,
                    writeCondition: WriteCondition(field, name, proto2),
                    // proto2 `required` becomes [ProtoMember(..., IsRequired = true)], which DROPS
                    // the write guard - so a required zero reaches the wire where an optional one
                    // does not. Without this an all-zero required message serialized to NOTHING
                    isRequired: field.label == FieldDescriptorProto.Label.LabelRequired));
            }

            // ORDER BY FIELD NUMBER, not by declaration. protobuf-net writes members in field-number
            // order, and a schema is free to declare them in any order at all - so emitting in
            // declaration order is a straight byte disagreement with ref-emit for any schema that
            // does. Found by pointing this front-end at descriptor.proto and diffing the emitted
            // tag sequences against the checked-in symbol-derived model: 17 of 21 shared contracts
            // matched exactly, and every one that did not differed ONLY in order (DescriptorProto
            // declares 6 before 3, FieldDescriptorProto declares 3 before 2, and so on). The
            // hand-written conformance schema had missed it entirely by declaring fields ascending
            members.Sort(static (x, y) => x.FieldNumber.CompareTo(y.FieldNumber));

            // CONVENTION: protogen emits `: IExtensible` on every message, with a private
            // __pbn__extensionData field - so the read's default case appends rather than skips
            // Mirror how the DTO generator was configured for THIS schema, so the two generated
            // halves cannot disagree. The distinction is kept rather than collapsed to one flag:
            // `sealed` says a sub-type is impossible, `Ignore` says one is tolerated and written
            // as the base - both elide the check, but they are not the same statement
            return new ProtoContractPlan(typeName,
                new EquatableArray<ProtoMemberPlan>(members.ToArray()),
                extensible: ProtoExtensibleKind.Untyped,
                isSealed: subTypes == SchemaSubTypes.Sealed,
                ignoreUnknownSubTypes: subTypes == SchemaSubTypes.Ignore);
        }

        /// <summary>
        /// A <c>repeated</c> field, which protogen emits as one of TWO C# shapes.
        /// </summary>
        /// <remarks>
        /// CONVENTION, read off protogen's output rather than assumed - and it is not the split
        /// anyone would guess:
        /// <list type="bullet">
        /// <item>a <b>packable scalar</b> (the numeric and bool types) becomes
        /// <c>T[] { get; set; }</c> with <c>IsPacked = true</c>;</item>
        /// <item><b>string</b>, <b>bytes</b> and <b>message</b> become a <b>getter-only</b>
        /// <c>List&lt;T&gt;</c> with an initialiser, and are not packed - correctly, since
        /// length-delimited elements cannot be;</item>
        /// <item>an <b>enum</b> becomes a getter-only <c>List&lt;E&gt;</c> that IS packed, which is
        /// the one combination that crosses the other two. It is refused here for a different
        /// reason - see below.</item>
        /// </list>
        /// The getter-only shape needs no accessor: the initialiser guarantees an instance, and a
        /// collection read mutates the instance the property already holds rather than assigning.
        /// </remarks>
        private static ProtoMemberPlan? TryBuildRepeated(DescriptorProto message,
            FieldDescriptorProto field, Dictionary<string, string> types,
            Dictionary<string, DescriptorProto> mapEntries, NameNormalizer names,
            ref string? unsupported)
        {
            // A repeated enum was PARKED here on the grounds that "the packed write arm disagrees
            // with ref-emit on an empty collection", emitting a zero-length field where ref-emit
            // wrote nothing. Re-checked 2026-08-14 and every clause of that reasoning failed:
            //
            //  * `IsPacked` IS honoured by the symbol path (ListOptions pins five such members
            //    against ref-emit), so it is not an unexercised argument;
            //  * there is no separate "packed raw-write arm" to be unexercised: RawRepeatedWritable
            //    declines `IsPacked` outright, so a packed member falls back to
            //    RepeatedSerializer.WriteRepeated - the SAME runtime call ref-emit makes;
            //  * and protobuf-net never actually packs an enum on either path anyway, because
            //    RepeatedSerializer.Write takes the packed branch only when the element serializer
            //    is IMeasuringSerializer<T>, and EnumSerializer<TEnum> is not one.
            //
            // Packing is also the WRITER'S choice - a reader must accept both forms - so declining
            // to pack could not be a wire bug even if the paths did differ. The byte gate below
            // (Schemas/conformance.proto's repeated enum, empty and populated) is what now holds
            // this honest, rather than a refusal.

            var elementKind = MapKind(field, out var typeRef, out var format);
            if (elementKind is null)
            {
                unsupported = $"{message.Name}.{field.Name}: repeated {field.type} is outside the spike";
                return null;
            }

            // a map arrives here as a repeated field of the synthetic entry message, which is
            // deliberately absent from the TYPE index (protogen emits no C# type for it) and
            // present in the entry index instead
            if (typeRef is not null && mapEntries.TryGetValue(typeRef, out var entry))
            {
                return TryBuildMap(message, field, entry, types, names, ref unsupported);
            }
            if (typeRef is not null && !types.ContainsKey(typeRef))
            {
                // not a map and not a known type: refuse rather than throw KeyNotFoundException
                // out of a source generator
                unsupported = $"{message.Name}.{field.Name}: type {typeRef} could not be resolved";
                return null;
            }

            var name = names.GetName(field);
            var isMessage = field.type == FieldDescriptorProto.Type.TypeMessage;
            var isEnum = field.type == FieldDescriptorProto.Type.TypeEnum;

            // an enum element is the ENUM type in the collection (List<E>) while its KIND is the
            // underlying scalar, which is what goes on the wire. Naming it on the plan is all the
            // serializer proxy needs: EmitEnumProxies derives ISerializerProxy<E> / <E?> from it,
            // which is how a repeated enum resolves its element serializer from the model
            var elementTypeName = isMessage || isEnum
                ? types[typeRef!]
                : ScalarTypeName(elementKind.Value);

            // packable scalars take the array shape; everything else the getter-only List<T>.
            // An enum is packable but is NOT an array - protogen emits a getter-only List<E> that
            // is packed, which is the one combination crossing the other two
            var packable = !isMessage && !isEnum
                && field.type is not (FieldDescriptorProto.Type.TypeString
                    or FieldDescriptorProto.Type.TypeBytes);

            var declaredTypeName = packable
                ? elementTypeName + "[]"
                : $"global::System.Collections.Generic.List<{elementTypeName}>";

            return new ProtoMemberPlan(field.Number, name, elementKind.Value,
                typeName: isMessage ? elementTypeName : null,
                enumTypeName: isEnum ? elementTypeName : null,
                // packable already excludes enums, so an enum takes the List factory
                repeated: new ProtoRepeatedPlan(packable ? "CreateVector" : "CreateList",
                    takesCollectionType: false, isValueType: false),
                elementTypeName: elementTypeName,
                declaredTypeName: declaredTypeName,
                // an enum collection IS packed, even though it takes the List shape
                isPacked: packable || isEnum,
                // the List<T> shape is GETTER-ONLY, so the read must mutate the instance the
                // property already holds rather than assign back to it. The initialiser protogen
                // emits is what guarantees there is one, so no accessor is needed
                isReadOnly: !packable,
                dataFormat: format);
        }

        /// <summary>
        /// A <c>map&lt;k,v&gt;</c>, which protogen emits as a getter-only
        /// <c>Dictionary&lt;K,V&gt;</c> with an initialiser.
        /// </summary>
        /// <remarks>
        /// The entry message's two fields ARE the key and value, by construction: field 1 is
        /// <c>key</c> and field 2 is <c>value</c>. Read from the descriptor rather than assumed
        /// positionally, since the schema is what defines them.
        /// <para>
        /// <c>IsValidProtobufMap</c> is protobuf-net's question, not proto's, and the two do not
        /// agree: protobuf-net excludes <c>bool</c> (and char and floating point) from valid keys,
        /// while proto permits <c>map&lt;bool, V&gt;</c>. Getting it wrong adds
        /// <c>OptionFailOnDuplicateKey</c>, which switches reading from <c>SetValues</c> to
        /// <c>AddRange</c> - a behavioural difference, not a cosmetic flag.
        /// </para>
        /// </remarks>
        private static ProtoMemberPlan? TryBuildMap(DescriptorProto message,
            FieldDescriptorProto field, DescriptorProto entry, Dictionary<string, string> types,
            NameNormalizer names, ref string? unsupported)
        {
            FieldDescriptorProto? keyField = null, valueField = null;
            foreach (var f in entry.Fields)
            {
                if (f.Number == 1) keyField = f;
                else if (f.Number == 2) valueField = f;
            }
            if (keyField is null || valueField is null)
            {
                unsupported = $"{message.Name}.{field.Name}: the map entry has no key/value pair";
                return null;
            }

            var keyKind = MapKind(keyField, out var keyRef, out _);
            var valueKind = MapKind(valueField, out var valueRef, out _);
            if (keyKind is null || valueKind is null)
            {
                unsupported = $"{message.Name}.{field.Name}: map<{keyField.type}, {valueField.type}> "
                    + "is outside the spike";
                return null;
            }

            var valueIsMessage = valueField.type == FieldDescriptorProto.Type.TypeMessage;
            if (valueIsMessage && !types.ContainsKey(valueRef!))
            {
                // DEFENSIVE, and believed unreachable: proto forbids a map as a map VALUE - the
                // parser rejects `map<string, map<string, int32>>` outright ("expected Symbol
                // '>'"). Kept so an unresolvable value type refuses rather than throwing
                // KeyNotFoundException out of a source generator. A map whose value is a MESSAGE
                // that happens to contain a map is ordinary, and is supported
                unsupported = $"{message.Name}.{field.Name}: the map value type could not be "
                    + "resolved";
                return null;
            }

            // an enum on either side is the Dictionary's type argument, while its KIND stays the
            // underlying scalar; naming it is all ISerializerProxy<TEnum> needs
            var keyIsEnum = keyField.type == FieldDescriptorProto.Type.TypeEnum;
            var valueIsEnum = valueField.type == FieldDescriptorProto.Type.TypeEnum;
            // an enum VALUE was parked here purely "alongside the repeated enum above, to keep the
            // two moving together". That case turned out to rest on a disproven premise and is now
            // lifted, so this follows it - the parking had no reason of its own, and the enum map
            // KEY was already supported directly below. Covered by the byte gate rather than by a
            // refusal: Schemas/conformance.proto's map<string, Grade>.

            var keyTypeName = keyIsEnum ? types[keyRef!] : ScalarTypeName(keyKind.Value);
            var valueTypeName = valueIsMessage || valueIsEnum
                ? types[valueRef!]
                : ScalarTypeName(valueKind.Value);

            // protobuf-net's own validity rule. Note bool is NOT a valid key to it, though proto
            // allows one - so that case is real and is covered. An ENUM key cannot occur at all:
            // proto forbids it ("invalid map key type (only integral and string types are
            // allowed)"), so keyIsEnum is dead on this path and is not tested for
            var validKey = keyKind.Value is ProtoMemberKind.Int32 or ProtoMemberKind.UInt32
                or ProtoMemberKind.Int64 or ProtoMemberKind.UInt64 or ProtoMemberKind.String;

            return new ProtoMemberPlan(field.Number, names.GetName(field), ProtoMemberKind.Message,
                declaredTypeName: $"global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}>",
                map: new ProtoMapPlan("CreateDictionary", takesCollectionType: false,
                    keyKind.Value, keyTypeName, valueKind.Value, valueTypeName,
                    isValidProtobufMap: validKey,
                    keyEnumTypeName: keyIsEnum ? keyTypeName : null,
                    valueEnumTypeName: valueIsEnum ? valueTypeName : null),
                // getter-only, exactly as the List<T> shape is
                isReadOnly: true);
        }

        /// <summary>
        /// The write guard for a field whose presence is tracked rather than inferred from its
        /// value: a <c>oneof</c> case, or a proto3 <c>optional</c>.
        /// </summary>
        /// <remarks>
        /// CONVENTION, and a much smaller one than it looks. protogen emits both of these as
        /// ORDINARY members with a <c>ShouldSerialize{Name}()</c> alongside — the
        /// <c>DiscriminatedUnion32Object</c> for a oneof, and the private nullable backing field
        /// for a proto3 optional, are implementation details behind the property. The plan already
        /// models exactly that as <see cref="ProtoMemberPlan.WriteCondition"/>, which emits
        /// <c>if (value.ShouldSerializeX())</c> and — importantly — REPLACES the trivial-value
        /// guard rather than nesting inside it. That is what makes an explicitly-set zero or empty
        /// string round-trip, which is the entire point of tracked presence.
        /// <para>
        /// Both arrive here identically: a proto3 <c>optional</c> is represented as a synthetic
        /// one-field oneof, so <c>ShouldSerializeOneofIndex()</c> is true for both and neither
        /// needs distinguishing. That is why one line covers two features.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// PROTO2 arrives by a different route to the same place. Every proto2 <c>optional</c> is
        /// presence-tracked whether or not it declares a default — protogen backs each one with a
        /// nullable field and emits <c>ShouldSerialize{Name}()</c> — so the condition applies to
        /// all of them, and <c>[default = x]</c> then needs no handling of its own: the condition
        /// REPLACES the value guard, so the declared default never reaches a comparison.
        /// <para>
        /// This was not a missing feature but a WIRE BUG, and it went both ways: an all-default
        /// <c>Defaulted</c> serialized to a full payload where ref-emit writes nothing (we
        /// compared each member against its type's zero, and the getters return the declared
        /// defaults), while an all-zero <c>Required</c> serialized to nothing where ref-emit
        /// writes every member. Caught by adding proto2 to the byte gate, which was proto3-only —
        /// the corpus probe could not see it, since it only asks whether a plan BUILDS.
        /// </para>
        /// </remarks>
        private static string? WriteCondition(FieldDescriptorProto field, string name, bool proto2)
        {
            // a oneof member, or a proto3 `optional` (a synthetic one-field oneof)
            if (field.ShouldSerializeOneofIndex()) return "ShouldSerialize" + name + "()";

            // ...or any proto2 optional. `required` is unconditional and `repeated` is a
            // collection, so neither gets one
            if (proto2 && field.label == FieldDescriptorProto.Label.LabelOptional)
                return "ShouldSerialize" + name + "()";

            return null;
        }

        /// <summary>
        /// Whether a file is proto2 — which is the case when `syntax` is ABSENT as well as when
        /// it is stated, since proto2 is the default and most proto2 files never say so.
        /// </summary>
        /// <remarks>
        /// It has to come from the FILE: a proto3 singular field is also <c>LabelOptional</c> in
        /// the descriptor, so the label alone cannot tell the two apart, and getting that wrong
        /// would put a <c>ShouldSerialize</c> guard on every proto3 field in the corpus.
        /// </remarks>
        private static bool IsProto2(FileDescriptorProto file)
        {
            var syntax = file?.Syntax;
            return string.IsNullOrEmpty(syntax)
                || string.Equals(syntax, "proto2", StringComparison.Ordinal);
        }

        /// <summary>The C# spelling of a scalar kind, as the element of a collection.</summary>
        private static string ScalarTypeName(ProtoMemberKind kind) => kind switch
        {
            ProtoMemberKind.Bool => "bool",
            ProtoMemberKind.Int32 => "int",
            ProtoMemberKind.UInt32 => "uint",
            ProtoMemberKind.Int64 => "long",
            ProtoMemberKind.UInt64 => "ulong",
            ProtoMemberKind.Single => "float",
            ProtoMemberKind.Double => "double",
            ProtoMemberKind.String => "string",
            ProtoMemberKind.Bytes => "byte[]",
            _ => "object", // unreachable: the caller has already refused anything else
        };

        /// <summary>
        /// The member's kind AND its <see cref="ProtoDataFormat"/> - which are two questions, not
        /// one.
        /// </summary>
        /// <remarks>
        /// The .proto scalar spellings encode the WIRE FORM as well as the CLR type:
        /// <c>int32</c>, <c>sint32</c> and <c>sfixed32</c> are all a C# <c>int</c>, and all three
        /// go on the wire differently - twos-complement varint, zig-zag varint, and fixed 32 bits.
        /// <para>
        /// Collapsing them to the CLR type alone compiles perfectly and writes the wrong bytes,
        /// which is exactly what the first version of this did: <c>sint32 delta = -17</c> went out
        /// as a sign-extended ten-byte varint instead of the single byte <c>0x21</c>. The byte
        /// differential caught it on its first run; nothing else could have.
        /// </para>
        /// </remarks>
        private static ProtoMemberKind? MapKind(FieldDescriptorProto field, out string? typeRef,
            out ProtoDataFormat format)
        {
            typeRef = null;
            format = ProtoDataFormat.Default;
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TypeBool: return ProtoMemberKind.Bool;

                case FieldDescriptorProto.Type.TypeInt32: return ProtoMemberKind.Int32;
                case FieldDescriptorProto.Type.TypeSint32:
                    format = ProtoDataFormat.ZigZag;
                    return ProtoMemberKind.Int32;
                case FieldDescriptorProto.Type.TypeSfixed32:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.Int32;

                case FieldDescriptorProto.Type.TypeUint32: return ProtoMemberKind.UInt32;
                case FieldDescriptorProto.Type.TypeFixed32:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.UInt32;

                case FieldDescriptorProto.Type.TypeInt64: return ProtoMemberKind.Int64;
                case FieldDescriptorProto.Type.TypeSint64:
                    format = ProtoDataFormat.ZigZag;
                    return ProtoMemberKind.Int64;
                case FieldDescriptorProto.Type.TypeSfixed64:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.Int64;

                case FieldDescriptorProto.Type.TypeUint64: return ProtoMemberKind.UInt64;
                case FieldDescriptorProto.Type.TypeFixed64:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.UInt64;

                case FieldDescriptorProto.Type.TypeFloat: return ProtoMemberKind.Single;
                case FieldDescriptorProto.Type.TypeDouble: return ProtoMemberKind.Double;
                case FieldDescriptorProto.Type.TypeString: return ProtoMemberKind.String;
                case FieldDescriptorProto.Type.TypeBytes: return ProtoMemberKind.Bytes;
                case FieldDescriptorProto.Type.TypeEnum:
                    typeRef = field.TypeName;
                    return ProtoMemberKind.Int32; // proto enums are int32 on the wire
                case FieldDescriptorProto.Type.TypeMessage:
                    typeRef = field.TypeName;
                    return ProtoMemberKind.Message;
                default:
                    return null;
            }
        }

        /// <summary>
        /// A schema type reference is fully qualified and dotted (<c>.probe.Inner</c>); the
        /// emitted C# name is the normalized package plus the normalized type name.
        /// </summary>
        private static string QualifyTypeRef(string typeRef, string? ns, NameNormalizer names)
        {
            var trimmed = typeRef.StartsWith(".") ? typeRef.Substring(1) : typeRef;
            var lastDot = trimmed.LastIndexOf('.');
            var leaf = lastDot < 0 ? trimmed : trimmed.Substring(lastDot + 1);
            // the spike only handles types in the file's own package, which the caller's scope
            // check has already narrowed to
            return Qualify(ns, leaf);
        }

        private static string Qualify(string? ns, string typeName)
            => string.IsNullOrEmpty(ns) ? $"global::{typeName}" : $"global::{ns}.{typeName}";
    }
}
