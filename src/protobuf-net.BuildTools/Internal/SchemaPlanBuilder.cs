#nullable enable
using Google.Protobuf.Reflection;
using ProtoBuf.BuildTools.Internal.Aot;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;

namespace ProtoBuf.BuildTools.Internal
{
    /// <summary>
    /// Builds AOT plans directly from a parsed <c>.proto</c> schema, rather than from Roslyn
    /// symbols — see <c>docs/aot-schema-model.md</c>.
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
            string? modelNamespace, string modelTypeName, out string? unsupported)
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
                if (!file.IncludeInOutput) continue;
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
                    if (!AddMessage(message, ns, null, names, types, mapEntries, contracts, ref unsupported)) return null;
                }

                // an enum reached as a MEMBER is an inline scalar and needs no plan of its own; it
                // would only become a ProtoEnumPlan as a model root, which this path never produces
            }

            if (contracts.Count == 0)
            {
                unsupported = "no messages";
                return null;
            }

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
            List<ProtoContractPlan> contracts, ref string? unsupported)
        {
            // the synthetic map-entry type: real to the descriptor, absent from the C#
            if (message.Options?.MapEntry == true) return true;

            var localName = names.GetName(message);
            var qualified = outer is null ? localName : outer + "." + localName;

            var contract = TryBuildMessage(message, ns, qualified, names, types, mapEntries, ref unsupported);
            if (contract is null) return false;
            contracts.Add(contract);

            foreach (var nested in message.NestedTypes)
            {
                if (!AddMessage(nested, ns, qualified, names, types, mapEntries, contracts, ref unsupported)) return false;
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
            Dictionary<string, DescriptorProto> mapEntries, ref string? unsupported)
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
                if (field.ShouldSerializeOneofIndex())
                {
                    unsupported = $"{message.Name}.{field.Name}: oneof is outside the spike";
                    return null;
                }

                var kind = MapKind(field, out var enumOrMessage, out var dataFormat);
                if (kind is null)
                {
                    unsupported = $"{message.Name}.{field.Name}: type {field.type} is outside the spike";
                    return null;
                }

                var name = names.GetName(field);
                string? typeNameForMember = null, enumTypeName = null, defaultLiteral = null;

                if (field.type == FieldDescriptorProto.Type.TypeEnum)
                {
                    // an enum member is its underlying scalar plus a cast, so the plan carries the
                    // enum's name and the kind is the underlying scalar (proto enums are int32)
                    enumTypeName = types[enumOrMessage!];
                }
                else if (field.type == FieldDescriptorProto.Type.TypeMessage)
                {
                    typeNameForMember = types[enumOrMessage!];
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
                    dataFormat: dataFormat));
            }

            // CONVENTION: protogen emits `: IExtensible` on every message, with a private
            // __pbn__extensionData field - so the read's default case appends rather than skips
            return new ProtoContractPlan(typeName,
                new EquatableArray<ProtoMemberPlan>(members.ToArray()),
                extensible: ProtoExtensibleKind.Untyped);
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
            if (field.type == FieldDescriptorProto.Type.TypeEnum)
            {
                // a repeated enum resolves its element serializer FROM THE MODEL, so the services
                // type has to implement ISerializerProxy<TEnum> for it (AGENTS.md, "A repeated enum
                // needs a serializer proxy"). That is real plumbing rather than a plan field, so it
                // waits for its own commit and its own byte test
                unsupported = $"{message.Name}.{field.Name}: a repeated enum needs a serializer proxy, "
                    + "which is not built yet";
                return null;
            }

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
            var elementTypeName = isMessage
                ? types[typeRef!]
                : ScalarTypeName(elementKind.Value);

            // packable scalars take the array shape; everything else the getter-only List<T>
            var packable = !isMessage
                && field.type is not (FieldDescriptorProto.Type.TypeString
                    or FieldDescriptorProto.Type.TypeBytes);

            var declaredTypeName = packable
                ? elementTypeName + "[]"
                : $"global::System.Collections.Generic.List<{elementTypeName}>";

            return new ProtoMemberPlan(field.Number, name, elementKind.Value,
                typeName: isMessage ? elementTypeName : null,
                repeated: new ProtoRepeatedPlan(packable ? "CreateVector" : "CreateList",
                    takesCollectionType: false, isValueType: false),
                elementTypeName: elementTypeName,
                declaredTypeName: declaredTypeName,
                isPacked: packable,
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

            if (keyField.type == FieldDescriptorProto.Type.TypeEnum
                || valueField.type == FieldDescriptorProto.Type.TypeEnum)
            {
                // same reason a repeated enum is refused: the element serializer is resolved from
                // the model, so the services type needs an ISerializerProxy<TEnum>
                unsupported = $"{message.Name}.{field.Name}: an enum in a map needs a serializer "
                    + "proxy, which is not built yet";
                return null;
            }

            var keyKind = MapKind(keyField, out _, out _);
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
                // a map whose value is itself a map: legal proto, and not modelled here
                unsupported = $"{message.Name}.{field.Name}: a map value that is itself a map is "
                    + "not supported yet";
                return null;
            }

            var keyTypeName = ScalarTypeName(keyKind.Value);
            var valueTypeName = valueIsMessage ? types[valueRef!] : ScalarTypeName(valueKind.Value);

            // protobuf-net's own validity rule; note bool is NOT a valid key to it, though proto
            // allows one
            var validKey = keyKind.Value is ProtoMemberKind.Int32 or ProtoMemberKind.UInt32
                or ProtoMemberKind.Int64 or ProtoMemberKind.UInt64 or ProtoMemberKind.String;

            return new ProtoMemberPlan(field.Number, names.GetName(field), ProtoMemberKind.Message,
                declaredTypeName: $"global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}>",
                map: new ProtoMapPlan("CreateDictionary", takesCollectionType: false,
                    keyKind.Value, keyTypeName, valueKind.Value, valueTypeName,
                    isValidProtobufMap: validKey),
                // getter-only, exactly as the List<T> shape is
                isReadOnly: true);
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
