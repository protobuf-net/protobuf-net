#nullable enable
using System;
using ProtoBuf.BuildTools.Internal.Aot;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        private const string Json = "global::System.Text.Json";
        private const string JsonWriter = Json + ".Utf8JsonWriter";
        private const string JsonReader = Json + ".Utf8JsonReader";
        private const string JsonToken = Json + ".JsonTokenType";
        private const string JsonSerializerInterface = "global::ProtoBuf.Connect.IJsonSerializer";
        private const string Invariant = "global::System.Globalization.CultureInfo.InvariantCulture";

        /// <summary>
        /// Emits the canonical protobuf JSON mapping for the contracts that have one.
        /// </summary>
        /// <remarks>
        /// A second emit surface over the same plan, and deliberately not a <c>JsonConverter</c> or
        /// anything else <c>System.Text.Json</c>'s serializer would drive: the mapping is specified
        /// against the <em>proto schema</em>, so it is not a C# shape being serialized. Only the
        /// low-level <see cref="System.Text.Json.Utf8JsonWriter"/> primitives are used, which are
        /// AOT-safe and allocate nothing per field.
        /// <para>
        /// <b>Presence follows protojson, not the binary write guard</b>, and the two therefore
        /// disagree in two places. An empty string or collection is written in binary and omitted
        /// here, because protobuf has no way to tell an empty value from an absent one and canonical
        /// JSON omits it; and <c>[DefaultValue]</c> is ignored here, because it is a protobuf-net
        /// write guard rather than a schema default. Matching the peer matters more than matching
        /// our own other codec - the peer is the reason JSON exists at all.
        /// </para>
        /// </remarks>
        private static void EmitJsonSurface(StringBuilder sb, int indent, ProtoModelPlan plan)
        {
            if (plan.JsonContracts.Count == 0) return;

            var json = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in plan.JsonContracts) json.Add(name);

            var enums = new Dictionary<string, ProtoJsonEnumPlan>(StringComparer.Ordinal);
            foreach (var item in plan.JsonEnums) enums[item.TypeName] = item;

            foreach (var contract in plan.Contracts)
            {
                if (!json.Contains(contract.TypeName)) continue;
                sb.AppendLine();
                EmitJsonContract(sb, indent, contract, enums);
            }

            foreach (var item in plan.JsonEnums)
            {
                sb.AppendLine();
                EmitJsonEnum(sb, indent, item);
            }

            sb.AppendLine();
            EmitJsonHelpers(sb, indent);
        }

        private static void EmitJsonContract(StringBuilder sb, int indent,
            ProtoContractPlan contract, Dictionary<string, ProtoJsonEnumPlan> enums)
        {
            var name = Sanitise(contract.TypeName);
            var type = contract.TypeName;

            // the explicit interface members delegate to plain statics, so that one contract writing
            // another is a direct call rather than a cast back through the interface
            Line(sb, indent, $"void {JsonSerializerInterface}<{type}>.Write({JsonWriter} writer, {type} value)");
            Line(sb, indent + 1, $"=> WriteJson_{name}(writer, value);");
            sb.AppendLine();
            Line(sb, indent, $"{type} {JsonSerializerInterface}<{type}>.Read(ref {JsonReader} reader, {type} value)");
            Line(sb, indent + 1, $"=> ReadJson_{name}(ref reader, value);");
            sb.AppendLine();

            if (contract.SurrogateTypeName is { } surrogate)
            {
                // The surrogate is a contract in its own right and already has its own JSON
                // serializer, so this is a conversion either side of a delegation - where the BINARY
                // path inlines the surrogate's members instead. Delegating is both smaller and more
                // obviously right: the surrogate's shape is emitted once, by the code that owns it.
                var surrogateName = Sanitise(surrogate);
                Line(sb, indent, $"private static void WriteJson_{name}({JsonWriter} writer, {type} value)");
                Line(sb, indent, "{");
                Line(sb, indent + 1, $"var surrogate = {ToSurrogate(contract, "value")};");
                Line(sb, indent + 1, $"WriteJson_{surrogateName}(writer, surrogate);");
                Line(sb, indent, "}");
                sb.AppendLine();
                Line(sb, indent, $"private static {type} ReadJson_{name}(ref {JsonReader} reader, {type} value)");
                Line(sb, indent, "{");
                // seeded from the incoming value, so a merge into an existing instance behaves as the
                // binary path's does rather than silently starting from nothing
                Line(sb, indent + 1, $"var surrogate = ReadJson_{surrogateName}(ref reader, {ToSurrogate(contract, "value")});");
                Line(sb, indent + 1, $"return {ToUnderlying(contract, "surrogate")};");
                Line(sb, indent, "}");
                return;
            }

            EmitJsonWrite(sb, indent, contract, enums, name, type);
            sb.AppendLine();
            EmitJsonRead(sb, indent, contract, enums, name, type);
        }

        private static void EmitJsonWrite(StringBuilder sb, int indent, ProtoContractPlan contract,
            Dictionary<string, ProtoJsonEnumPlan> enums, string name, string type)
        {
            Line(sb, indent, $"private static void WriteJson_{name}({JsonWriter} writer, {type} value)");
            Line(sb, indent, "{");
            if (!contract.IsValueType)
            {
                Line(sb, indent + 1, "if (value is null) { writer.WriteNullValue(); return; }");
            }
            Line(sb, indent + 1, "writer.WriteStartObject();");

            foreach (var member in contract.Members)
            {
                var local = "v" + member.FieldNumber.ToString(CultureInfo.InvariantCulture);
                var key = JsonName(member);
                Line(sb, indent + 1, $"var {local} = {MemberAccess(contract, member, "value")};");

                // an explicit-presence member - Specified, ShouldSerialize, or a Nullable<T> - is
                // written whenever it is present, default value included; that is what presence
                // means, and protojson agrees with the binary path here
                var condition = member.WriteCondition;
                if (condition is not null)
                {
                    Line(sb, indent + 1, $"if (value.{condition})");
                    Line(sb, indent + 1, "{");
                }
                var body = condition is null ? indent + 1 : indent + 2;

                if (member.Map.Factory is not null)
                {
                    EmitJsonMapWrite(sb, body, member, enums, local, key);
                }
                else if (member.Repeated.Factory is not null)
                {
                    EmitJsonRepeatedWrite(sb, body, member, enums, local, key);
                }
                else if (member.IsNullable)
                {
                    Line(sb, body, $"if ({local}.HasValue)");
                    Line(sb, body, "{");
                    EmitJsonValueWrite(sb, body + 1, member, enums, $"{local}.GetValueOrDefault()", key);
                    Line(sb, body, "}");
                }
                else
                {
                    var guard = JsonWriteGuard(member, local);
                    if (guard is null || condition is not null)
                    {
                        EmitJsonValueWrite(sb, body, member, enums, local, key);
                    }
                    else
                    {
                        Line(sb, body, $"if ({guard})");
                        Line(sb, body, "{");
                        EmitJsonValueWrite(sb, body + 1, member, enums, local, key);
                        Line(sb, body, "}");
                    }
                }

                if (condition is not null) Line(sb, indent + 1, "}");
            }

            Line(sb, indent + 1, "writer.WriteEndObject();");
            Line(sb, indent, "}");
        }

        /// <summary>
        /// When this member is worth writing at all, by protojson's rule rather than protobuf-net's.
        /// </summary>
        /// <remarks>
        /// Null means "always". The differences from <see cref="ScalarGuard"/> are deliberate and are
        /// the whole of where the two codecs disagree: a <c>[DefaultValue]</c> is not consulted (it is
        /// a protobuf-net write guard, not a schema default), and an empty string or collection is
        /// omitted rather than written (protobuf cannot tell empty from absent, and canonical JSON
        /// omits).
        /// </remarks>
        private static string? JsonWriteGuard(ProtoMemberPlan member, string local)
        {
            if (member.EnumTypeName is { } enumType) return $"{local} != default({enumType})";

            return member.Kind switch
            {
                ProtoMemberKind.Bool => local,
                ProtoMemberKind.Single => $"{local} != 0f",
                ProtoMemberKind.Double => $"{local} != 0d",
                ProtoMemberKind.String => $"!string.IsNullOrEmpty({local})",
                ProtoMemberKind.Bytes => member.MemberIsValueType ? $"{local}.Length != 0" : $"{local} != null && {local}.Length != 0",
                ProtoMemberKind.Message => member.MemberIsValueType ? null : $"{local} != null",
                ProtoMemberKind.Uri or ProtoMemberKind.Parseable
                    => member.MemberIsValueType ? null : $"{local} != null",
                // unconditional, matching the binary path: zero is a legitimate date, so there is no
                // trivial value to skip - and protobuf-net therefore puts the Timestamp message on the
                // wire always, which means a peer sees the field as PRESENT. Omitting it here made our
                // JSON disagree with our own binary about the same instance
                ProtoMemberKind.DateTime => null,
                ProtoMemberKind.TimeSpan => $"{local} != global::System.TimeSpan.Zero",
                ProtoMemberKind.Guid => $"{local} != global::System.Guid.Empty",
                ProtoMemberKind.Decimal => $"{local} != 0m",
                ProtoMemberKind.Char => $"{local} != '\\0'",
                _ => $"{local} != 0",
            };
        }

        /// <summary>
        /// The JSON key for a member: <see cref="ToJsonName"/> over the schema name.
        /// </summary>
        private static string JsonName(ProtoMemberPlan member) => ToJsonName(SchemaNameOf(member));

        private static void EmitJsonValueWrite(StringBuilder sb, int indent, ProtoMemberPlan member,
            Dictionary<string, ProtoJsonEnumPlan> enums, string value, string? key)
        {
            // Utf8JsonWriter pairs every WriteX(name, value) with a WriteXValue(value), so a single
            // shape serves both a named member and an array or map element
            var named = key is null ? "" : $"\"{key}\", ";
            var suffix = key is null ? "Value" : "";

            if (member.EnumTypeName is { } enumType)
            {
                if (key is not null) Line(sb, indent, $"writer.WritePropertyName(\"{key}\");");
                Line(sb, indent, $"WriteJsonEnum_{Sanitise(enumType)}(writer, {value});");
                return;
            }

            EmitJsonScalarWrite(sb, indent, member.Kind, member.TypeName, value, named, suffix, key);
        }

        private static void EmitJsonScalarWrite(StringBuilder sb, int indent, ProtoMemberKind kind,
            string? typeName, string value, string named, string suffix, string? key)
        {
            switch (kind)
            {
                case ProtoMemberKind.Bool:
                    Line(sb, indent, $"writer.WriteBoolean{suffix}({named}{value});");
                    return;

                // the 64-bit integers are JSON *strings*, which is the single most-quoted rule of
                // the mapping and the one a POCO serializer gets wrong by default: JSON numbers are
                // doubles in most readers, so an int64 above 2^53 would not survive the trip
                case ProtoMemberKind.Int64:
                case ProtoMemberKind.UInt64:
                case ProtoMemberKind.IntPtr:
                case ProtoMemberKind.UIntPtr:
                    Line(sb, indent, $"writer.WriteString{suffix}({named}{value}.ToString({Invariant}));");
                    return;

                case ProtoMemberKind.SByte:
                case ProtoMemberKind.Byte:
                case ProtoMemberKind.Int16:
                case ProtoMemberKind.UInt16:
                case ProtoMemberKind.Int32:
                case ProtoMemberKind.UInt32:
                    Line(sb, indent, $"writer.WriteNumber{suffix}({named}{value});");
                    return;

                // a char is a uint16 varint on the wire, so its schema type is uint32 and its JSON
                // is the number - not the character, which is what any POCO serializer would write
                case ProtoMemberKind.Char:
                    Line(sb, indent, $"writer.WriteNumber{suffix}({named}(ushort){value});");
                    return;

                // NaN and the infinities have no JSON number form, so the mapping spells them as
                // strings; everything else stays a number
                case ProtoMemberKind.Single:
                case ProtoMemberKind.Double:
                    if (key is not null) Line(sb, indent, $"writer.WritePropertyName(\"{key}\");");
                    Line(sb, indent, $"WriteJsonDouble(writer, {value});");
                    return;

                case ProtoMemberKind.String:
                    Line(sb, indent, $"writer.WriteString{suffix}({named}{value});");
                    return;

                case ProtoMemberKind.Uri:
                    Line(sb, indent, $"writer.WriteString{suffix}({named}{value}.OriginalString);");
                    return;

                case ProtoMemberKind.Parseable:
                    Line(sb, indent, $"writer.WriteString{suffix}({named}{value}.ToString());");
                    return;

                // base64, in the standard padded alphabet - deliberately NOT the URL-safe unpadded
                // one Connect's GET encoding uses, which is a different RFC 4648 section for a
                // different job
                case ProtoMemberKind.Bytes:
                    Line(sb, indent, $"writer.WriteBase64String{suffix}({named}{value});");
                    return;

                // google.protobuf.Timestamp, so RFC 3339 with a Z offset; the compatibility level is
                // what made it a Timestamp rather than a protobuf-net message, and the JSON pass
                // refused it below 240 for exactly that reason
                case ProtoMemberKind.DateTime:
                    if (key is not null) Line(sb, indent, $"writer.WritePropertyName(\"{key}\");");
                    Line(sb, indent, $"WriteJsonTimestamp(writer, {value});");
                    return;

                case ProtoMemberKind.TimeSpan:
                    if (key is not null) Line(sb, indent, $"writer.WritePropertyName(\"{key}\");");
                    Line(sb, indent, $"WriteJsonDuration(writer, {value});");
                    return;

                // NOT Guid.ToString(): protobuf-net's level-300 GuidString writes an EMPTY payload
                // for Guid.Empty (GuidHelper.Write's first branch) and the 'D' form otherwise, so a
                // plain ToString puts "00000000-0000-0000-0000-000000000000" where the binary codec
                // and every peer reading our schema see "". Note Guid also has no
                // ToString(IFormatProvider) overload, which is how this first announced itself.
                case ProtoMemberKind.Guid:
                    Line(sb, indent, $"writer.WriteString{suffix}({named}JsonGuid({value}));");
                    return;

                case ProtoMemberKind.Decimal:
                    Line(sb, indent, $"writer.WriteString{suffix}({named}{value}.ToString({Invariant}));");
                    return;

                case ProtoMemberKind.Message:
                    if (key is not null) Line(sb, indent, $"writer.WritePropertyName(\"{key}\");");
                    Line(sb, indent, $"WriteJson_{Sanitise(typeName!)}(writer, {value});");
                    return;

                default:
                    Line(sb, indent, $"writer.WriteNullValue(); // unsupported kind {kind}");
                    return;
            }
        }

        private static void EmitJsonRepeatedWrite(StringBuilder sb, int indent, ProtoMemberPlan member,
            Dictionary<string, ProtoJsonEnumPlan> enums, string local, string key)
        {
            // an empty repeated field is omitted, exactly as a default scalar is: protobuf has no
            // way to distinguish empty from absent, so canonical JSON writes neither
            Line(sb, indent, member.Repeated.IsValueType
                ? $"if ({local}.Length != 0)"
                : $"if ({local} != null && JsonAny({local}))");
            Line(sb, indent, "{");
            Line(sb, indent + 1, $"writer.WritePropertyName(\"{key}\");");
            Line(sb, indent + 1, "writer.WriteStartArray();");
            Line(sb, indent + 1, $"foreach (var item in {local})");
            Line(sb, indent + 1, "{");
            EmitJsonValueWrite(sb, indent + 2, member, enums, "item", null);
            Line(sb, indent + 1, "}");
            Line(sb, indent + 1, "writer.WriteEndArray();");
            Line(sb, indent, "}");
        }

        private static void EmitJsonMapWrite(StringBuilder sb, int indent, ProtoMemberPlan member,
            Dictionary<string, ProtoJsonEnumPlan> enums, string local, string key)
        {
            Line(sb, indent, $"if ({local} != null && {local}.Count != 0)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, $"writer.WritePropertyName(\"{key}\");");
            Line(sb, indent + 1, "writer.WriteStartObject();");
            Line(sb, indent + 1, $"foreach (var pair in {local})");
            Line(sb, indent + 1, "{");

            // a map key is ALWAYS a string in canonical JSON, whatever it is in the schema - so an
            // int32 key goes out as "1". That is the rule a POCO serializer happens to agree with
            // for strings and disagrees with for everything else
            Line(sb, indent + 2, $"writer.WritePropertyName({JsonMapKey(member, "pair.Key")});");

            var valueMember = new ProtoMemberPlan(member.FieldNumber, member.Name, member.Map.ValueKind,
                typeName: member.Map.ValueTypeName, enumTypeName: member.Map.ValueEnumTypeName);
            EmitJsonValueWrite(sb, indent + 2, valueMember, enums, "pair.Value", null);

            Line(sb, indent + 1, "}");
            Line(sb, indent + 1, "writer.WriteEndObject();");
            Line(sb, indent, "}");
        }

        private static string JsonMapKey(ProtoMemberPlan member, string value)
        {
            // no enum branch: protobuf forbids an enum map key and the JSON pass refuses the shape
            // before it reaches here. An `Enum.ToString()` fallback would be *accidentally plausible*
            // - it produces the member name - which is exactly the kind of quietly-wrong output that
            // survives a self-test, so there is deliberately nothing to fall back to
            return member.Map.KeyKind switch
            {
                ProtoMemberKind.String => value,
                ProtoMemberKind.Bool => $"({value} ? \"true\" : \"false\")",
                _ => $"{value}.ToString({Invariant})",
            };
        }

        private static void EmitJsonEnum(StringBuilder sb, int indent, ProtoJsonEnumPlan plan)
        {
            var name = Sanitise(plan.TypeName);

            Line(sb, indent, $"private static void WriteJsonEnum_{name}({JsonWriter} writer, {plan.TypeName} value)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "switch (value)");
            Line(sb, indent + 1, "{");
            foreach (var member in plan.Members)
            {
                Line(sb, indent + 2, $"case {plan.TypeName}.{Escape(member.CSharpName)}: writer.WriteStringValue(\"{member.JsonName}\"); return;");
            }
            // an unrecognised value has no name to write; the mapping's answer is the number, which
            // is also what a [Flags] combination lands on
            Line(sb, indent + 2, $"default: writer.WriteNumberValue(({plan.UnderlyingTypeName})value); return;");
            Line(sb, indent + 1, "}");
            Line(sb, indent, "}");
            sb.AppendLine();

            Line(sb, indent, $"private static string JsonEnumName_{name}({plan.TypeName} value)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "switch (value)");
            Line(sb, indent + 1, "{");
            foreach (var member in plan.Members)
            {
                Line(sb, indent + 2, $"case {plan.TypeName}.{Escape(member.CSharpName)}: return \"{member.JsonName}\";");
            }
            Line(sb, indent + 2, $"default: return (({plan.UnderlyingTypeName})value).ToString({Invariant});");
            Line(sb, indent + 1, "}");
            Line(sb, indent, "}");
            sb.AppendLine();

            Line(sb, indent, $"private static {plan.TypeName} ReadJsonEnum_{name}(ref {JsonReader} reader)");
            Line(sb, indent, "{");
            // a reader accepts the number as well as the name, which is what makes an enum added by
            // a newer peer survive a round trip through an older one
            Line(sb, indent + 1, $"if (reader.TokenType != {JsonToken}.String) return ({plan.TypeName})reader.Get{NumberAccessor(plan.UnderlyingTypeName)}();");
            foreach (var member in plan.Members)
            {
                Line(sb, indent + 1, $"if (reader.ValueTextEquals(\"{member.JsonName}\")) return {plan.TypeName}.{Escape(member.CSharpName)};");
            }
            Line(sb, indent + 1, $"throw new {Json}.JsonException($\"Unknown value '{{reader.GetString()}}' for enum {plan.TypeName.Replace("global::", "")}\");");
            Line(sb, indent, "}");
        }

        private static string NumberAccessor(string underlying) => underlying switch
        {
            "ulong" => "UInt64",
            "long" => "Int64",
            "uint" => "UInt32",
            _ => "Int32",
        };

        private static void EmitJsonRead(StringBuilder sb, int indent, ProtoContractPlan contract,
            Dictionary<string, ProtoJsonEnumPlan> enums, string name, string type)
        {
            Line(sb, indent, $"private static {type} ReadJson_{name}(ref {JsonReader} reader, {type} value)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, $"if (reader.TokenType == {JsonToken}.None) reader.Read();");
            if (!contract.IsValueType)
            {
                // a JSON null for a message member means "absent", which merges as nothing
                Line(sb, indent + 1, $"if (reader.TokenType == {JsonToken}.Null) return value;");
                Line(sb, indent + 1, $"value ??= {ConstructJson(contract)};");
            }
            Line(sb, indent + 1, "while (reader.Read())");
            Line(sb, indent + 1, "{");
            Line(sb, indent + 2, $"if (reader.TokenType == {JsonToken}.EndObject) break;");

            foreach (var member in contract.Members)
            {
                var number = member.FieldNumber.ToString(CultureInfo.InvariantCulture);
                var target = MemberAccess(contract, member, "value");

                // a reader accepts BOTH spellings - the JSON name and the original field name - which
                // is a requirement of the mapping rather than a kindness; the two coincide for an
                // unpinned code-first member, so only a pinned one emits the second test
                var schema = SchemaNameOf(member);
                var json = ToJsonName(schema);
                var test = schema == json
                    ? $"reader.ValueTextEquals(\"{json}\")"
                    : $"reader.ValueTextEquals(\"{json}\") || reader.ValueTextEquals(\"{schema}\")";

                Line(sb, indent + 2, $"if ({test})");
                Line(sb, indent + 2, "{");
                Line(sb, indent + 3, "reader.Read();");

                if (member.Map.Factory is not null)
                {
                    EmitJsonCollectionStore(sb, indent + 3, contract, member, target, number,
                        $"ReadJsonMap_{name}_{number}", MapConcreteType(member));
                }
                else if (member.Repeated.Factory is not null)
                {
                    EmitJsonCollectionStore(sb, indent + 3, contract, member, target, number,
                        $"ReadJsonList_{name}_{number}",
                        $"global::System.Collections.Generic.List<{JsonElementTypeName(member)}>");
                }
                else
                {
                    Line(sb, indent + 3, $"if (reader.TokenType == {JsonToken}.Null) {{ reader.Skip(); continue; }}");
                    Line(sb, indent + 3, Assign(contract, member, "value", target,
                        JsonReadExpression(member, member.Kind, member.TypeName, member.EnumTypeName,
                            member.IsNullable, target)));
                }

                Line(sb, indent + 3, "continue;");
                Line(sb, indent + 2, "}");
            }

            // unknown fields are ignored rather than rejected. Google's parser rejects by default and
            // offers IgnoreUnknownFields; ignoring is the choice that lets a peer add a field without
            // breaking us, which is the whole point of having a schema
            Line(sb, indent + 2, "reader.Read();");
            Line(sb, indent + 2, "reader.Skip();");
            Line(sb, indent + 1, "}");
            Line(sb, indent + 1, "return value;");
            Line(sb, indent, "}");

            foreach (var member in contract.Members)
            {
                var number = member.FieldNumber.ToString(CultureInfo.InvariantCulture);
                if (member.Map.Factory is not null)
                {
                    sb.AppendLine();
                    EmitJsonMapRead(sb, indent, member, name, number);
                }
                else if (member.Repeated.Factory is not null)
                {
                    sb.AppendLine();
                    EmitJsonListRead(sb, indent, member, name, number);
                }
            }
        }

        /// <summary>
        /// Stores a collection that has just been read, by the only route the declared type allows.
        /// </summary>
        /// <remarks>
        /// Three shapes rather than one, and the split is not cosmetic. A getter-only collection must
        /// be <em>appended to</em> - assigning would compile to a discarded expression, or not compile
        /// at all - which is also how the binary path treats one. A writable appendable collection is
        /// appended to as well, so the two codecs merge alike. Only the shapes with no <c>Add</c> -
        /// an array, or a read-only interface - are replaced wholesale, because there is no other
        /// option; <c>JsonCollectionRefusal</c> has already excluded the combinations with neither.
        /// </remarks>
        private static void EmitJsonCollectionStore(StringBuilder sb, int indent,
            ProtoContractPlan contract, ProtoMemberPlan member, string target, string number,
            string fill, string concrete)
        {
            var kind = JsonCollectionKindOf(member.DeclaredTypeName);
            var appendable = kind is JsonCollectionKind.List or JsonCollectionKind.Dictionary;

            if (!appendable)
            {
                // no Add to reach, so the whole collection is replaced
                Line(sb, indent, $"var tmp{number} = new {concrete}();");
                Line(sb, indent, $"{fill}(ref reader, tmp{number});");
                Line(sb, indent, Assign(contract, member, "value", target,
                    kind == JsonCollectionKind.Array ? $"tmp{number}.ToArray()" : $"tmp{number}"));
                return;
            }

            Line(sb, indent, $"var tmp{number} = {target};");
            if (member.IsReadOnly)
            {
                Line(sb, indent, $"if (tmp{number} != null) {fill}(ref reader, tmp{number}); else reader.Skip();");
                return;
            }
            Line(sb, indent, $"if (tmp{number} == null)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, $"tmp{number} = new {concrete}();");
            Line(sb, indent + 1, Assign(contract, member, "value", target, $"tmp{number}"));
            Line(sb, indent, "}");
            Line(sb, indent, $"{fill}(ref reader, tmp{number});");
        }

        private static void EmitJsonListRead(StringBuilder sb, int indent, ProtoMemberPlan member,
            string owner, string number)
        {
            var element = JsonElementTypeName(member);
            Line(sb, indent, $"private static void ReadJsonList_{owner}_{number}(ref {JsonReader} reader, global::System.Collections.Generic.ICollection<{element}> list)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, $"if (reader.TokenType == {JsonToken}.Null) return;");
            Line(sb, indent + 1, "while (reader.Read())");
            Line(sb, indent + 1, "{");
            Line(sb, indent + 2, $"if (reader.TokenType == {JsonToken}.EndArray) break;");
            Line(sb, indent + 2, $"list.Add({JsonReadExpression(member, member.Kind, member.TypeName, member.EnumTypeName, false, "default")});");
            Line(sb, indent + 1, "}");
            Line(sb, indent, "}");
        }

        private static void EmitJsonMapRead(StringBuilder sb, int indent, ProtoMemberPlan member,
            string owner, string number)
        {
            var concrete = MapConcreteType(member);
            Line(sb, indent, $"private static void ReadJsonMap_{owner}_{number}(ref {JsonReader} reader, global::System.Collections.Generic.IDictionary<{member.Map.KeyEnumTypeName ?? member.Map.KeyTypeName}, {member.Map.ValueEnumTypeName ?? member.Map.ValueTypeName}> map)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, $"if (reader.TokenType == {JsonToken}.Null) return;");
            Line(sb, indent + 1, "while (reader.Read())");
            Line(sb, indent + 1, "{");
            Line(sb, indent + 2, $"if (reader.TokenType == {JsonToken}.EndObject) break;");

            // the key arrives as a property name, i.e. always a string, whatever the schema says it
            // is - so every non-string key is parsed back out of one
            Line(sb, indent + 2, "var key = reader.GetString();");
            Line(sb, indent + 2, "reader.Read();");

            var valueMember = new ProtoMemberPlan(member.FieldNumber, member.Name, member.Map.ValueKind,
                typeName: member.Map.ValueTypeName, enumTypeName: member.Map.ValueEnumTypeName);
            Line(sb, indent + 2, $"map[{JsonMapKeyParse(member)}] = {JsonReadExpression(valueMember, member.Map.ValueKind, member.Map.ValueTypeName, member.Map.ValueEnumTypeName, false, "default")};");
            Line(sb, indent + 1, "}");
            Line(sb, indent, "}");
        }

        private static string JsonMapKeyParse(ProtoMemberPlan member)
        {
            // as for JsonMapKey: an enum key is refused, so there is nothing to parse
            return member.Map.KeyKind switch
            {
                ProtoMemberKind.String => "key",
                ProtoMemberKind.Bool => "bool.Parse(key)",
                ProtoMemberKind.Int32 => $"int.Parse(key, {Invariant})",
                ProtoMemberKind.UInt32 => $"uint.Parse(key, {Invariant})",
                ProtoMemberKind.Int64 => $"long.Parse(key, {Invariant})",
                ProtoMemberKind.UInt64 => $"ulong.Parse(key, {Invariant})",
                ProtoMemberKind.SByte => $"sbyte.Parse(key, {Invariant})",
                ProtoMemberKind.Byte => $"byte.Parse(key, {Invariant})",
                ProtoMemberKind.Int16 => $"short.Parse(key, {Invariant})",
                ProtoMemberKind.UInt16 => $"ushort.Parse(key, {Invariant})",
                _ => "key",
            };
        }

        /// <summary>Reads one value at the reader's current position.</summary>
        private static string JsonReadExpression(ProtoMemberPlan member, ProtoMemberKind kind,
            string? typeName, string? enumTypeName, bool isNullable, string existing)
        {
            if (enumTypeName is not null)
            {
                return $"ReadJsonEnum_{Sanitise(enumTypeName)}(ref reader)";
            }

            var core = kind switch
            {
                ProtoMemberKind.Bool => "reader.GetBoolean()",
                ProtoMemberKind.SByte => "(sbyte)ReadJsonInt32(ref reader)",
                ProtoMemberKind.Byte => "(byte)ReadJsonInt32(ref reader)",
                ProtoMemberKind.Int16 => "(short)ReadJsonInt32(ref reader)",
                ProtoMemberKind.UInt16 => "(ushort)ReadJsonInt32(ref reader)",
                ProtoMemberKind.Int32 => "ReadJsonInt32(ref reader)",
                ProtoMemberKind.UInt32 => "(uint)ReadJsonInt64(ref reader)",
                ProtoMemberKind.Int64 => "ReadJsonInt64(ref reader)",
                ProtoMemberKind.UInt64 => "ReadJsonUInt64(ref reader)",
                ProtoMemberKind.IntPtr => "(nint)ReadJsonInt64(ref reader)",
                ProtoMemberKind.UIntPtr => "(nuint)ReadJsonUInt64(ref reader)",
                ProtoMemberKind.Single => "(float)ReadJsonDouble(ref reader)",
                ProtoMemberKind.Double => "ReadJsonDouble(ref reader)",
                ProtoMemberKind.Char => "(char)ReadJsonInt32(ref reader)",
                ProtoMemberKind.String => "reader.GetString()",
                ProtoMemberKind.Bytes => "reader.GetBytesFromBase64()",
                ProtoMemberKind.Uri => "new global::System.Uri(reader.GetString(), global::System.UriKind.RelativeOrAbsolute)",
                ProtoMemberKind.Parseable => $"{typeName}.Parse(reader.GetString())",
                ProtoMemberKind.DateTime => "ReadJsonTimestamp(ref reader)",
                ProtoMemberKind.TimeSpan => "ReadJsonDuration(ref reader)",
                ProtoMemberKind.Guid => "ReadJsonGuid(ref reader)",
                ProtoMemberKind.Decimal => $"decimal.Parse(reader.GetString(), {Invariant})",
                // merging into the existing instance, exactly as the binary read does - so a caller
                // deserializing over a populated object gets the same answer from either codec
                ProtoMemberKind.Message => $"ReadJson_{Sanitise(typeName!)}(ref reader, {existing})",
                _ => "default",
            };

            return isNullable ? $"({core})" : core;
        }

        private static string ConstructJson(ProtoContractPlan contract)
            => contract.UsesConstructorAccessor
                ? $"{ConstructorAccessorName(contract)}()"
                : $"new {contract.TypeName}()";

        /// <summary>The element type of a repeated member, as a C# type name.</summary>
        private static string JsonElementTypeName(ProtoMemberPlan member)
            => member.EnumTypeName ?? member.ElementTypeName ?? member.TypeName ?? "object";

        private static string MapConcreteType(ProtoMemberPlan member)
            => $"global::System.Collections.Generic.Dictionary<{member.Map.KeyEnumTypeName ?? member.Map.KeyTypeName}, {member.Map.ValueEnumTypeName ?? member.Map.ValueTypeName}>";

        /// <summary>
        /// The handful of primitives every generated JSON serializer shares.
        /// </summary>
        /// <remarks>
        /// Emitted onto the services type rather than shipped in a library, because there is no
        /// library on this side of the seam that a generated model already references - and because
        /// keeping them here means the whole JSON surface is visible in the generated file.
        /// </remarks>
        private static void EmitJsonHelpers(StringBuilder sb, int indent)
        {
            Line(sb, indent, "// --- shared JSON primitives ---");
            sb.AppendLine();

            // every numeric accepts a JSON string as well as a number: the 64-bit types are *written*
            // as strings, and a peer is free to quote any of them
            Line(sb, indent, $"private static int ReadJsonInt32(ref {JsonReader} reader)");
            Line(sb, indent + 1, $"=> reader.TokenType == {JsonToken}.String ? int.Parse(reader.GetString(), {Invariant}) : reader.GetInt32();");
            sb.AppendLine();
            Line(sb, indent, $"private static long ReadJsonInt64(ref {JsonReader} reader)");
            Line(sb, indent + 1, $"=> reader.TokenType == {JsonToken}.String ? long.Parse(reader.GetString(), {Invariant}) : reader.GetInt64();");
            sb.AppendLine();
            Line(sb, indent, $"private static ulong ReadJsonUInt64(ref {JsonReader} reader)");
            Line(sb, indent + 1, $"=> reader.TokenType == {JsonToken}.String ? ulong.Parse(reader.GetString(), {Invariant}) : reader.GetUInt64();");
            sb.AppendLine();

            // NaN and the infinities have no JSON number form and travel as these exact strings
            Line(sb, indent, $"private static void WriteJsonDouble({JsonWriter} writer, double value)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "if (double.IsNaN(value)) writer.WriteStringValue(\"NaN\");");
            Line(sb, indent + 1, "else if (double.IsPositiveInfinity(value)) writer.WriteStringValue(\"Infinity\");");
            Line(sb, indent + 1, "else if (double.IsNegativeInfinity(value)) writer.WriteStringValue(\"-Infinity\");");
            Line(sb, indent + 1, "else writer.WriteNumberValue(value);");
            Line(sb, indent, "}");
            sb.AppendLine();

            Line(sb, indent, $"private static double ReadJsonDouble(ref {JsonReader} reader)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, $"if (reader.TokenType != {JsonToken}.String) return reader.GetDouble();");
            Line(sb, indent + 1, "var text = reader.GetString();");
            Line(sb, indent + 1, "if (text == \"NaN\") return double.NaN;");
            Line(sb, indent + 1, "if (text == \"Infinity\") return double.PositiveInfinity;");
            Line(sb, indent + 1, "if (text == \"-Infinity\") return double.NegativeInfinity;");
            Line(sb, indent + 1, $"return double.Parse(text, {Invariant});");
            Line(sb, indent, "}");
            sb.AppendLine();

            // google.protobuf.Timestamp: RFC 3339 with a Z offset, and 0/3/6/9 fractional digits -
            // "the number of digits is the smallest of 0, 3, 6 or 9 that can represent the value"
            Line(sb, indent, $"private static void WriteJsonTimestamp({JsonWriter} writer, global::System.DateTime value)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "var utc = value.Kind == global::System.DateTimeKind.Unspecified");
            Line(sb, indent + 2, "? global::System.DateTime.SpecifyKind(value, global::System.DateTimeKind.Utc)");
            Line(sb, indent + 2, ": value.ToUniversalTime();");
            Line(sb, indent + 1, "var ticks = utc.Ticks % global::System.TimeSpan.TicksPerSecond;");
            Line(sb, indent + 1, $"var text = utc.ToString(\"yyyy'-'MM'-'dd'T'HH':'mm':'ss\", {Invariant});");
            Line(sb, indent + 1, "writer.WriteStringValue(text + JsonFraction(ticks) + \"Z\");");
            Line(sb, indent, "}");
            sb.AppendLine();

            Line(sb, indent, $"private static global::System.DateTime ReadJsonTimestamp(ref {JsonReader} reader)");
            Line(sb, indent + 1, "=> global::System.DateTime.Parse(reader.GetString(), " + Invariant + ",");
            Line(sb, indent + 2, "global::System.Globalization.DateTimeStyles.AdjustToUniversal | global::System.Globalization.DateTimeStyles.AssumeUniversal);");
            sb.AppendLine();

            // google.protobuf.Duration: seconds with up to nine fractional digits and a literal "s"
            Line(sb, indent, $"private static void WriteJsonDuration({JsonWriter} writer, global::System.TimeSpan value)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "var ticks = value.Ticks;");
            Line(sb, indent + 1, "var seconds = ticks / global::System.TimeSpan.TicksPerSecond;");
            Line(sb, indent + 1, "var fraction = global::System.Math.Abs(ticks % global::System.TimeSpan.TicksPerSecond);");
            // the sign lives on the fraction when the whole-seconds part is zero, and integer division
            // truncates toward zero - so -0.25s has seconds == 0 and would otherwise print as "0.250s"
            Line(sb, indent + 1, "var sign = ticks < 0 && seconds == 0 ? \"-\" : \"\";");
            Line(sb, indent + 1, $"writer.WriteStringValue(sign + seconds.ToString({Invariant}) + JsonFraction(fraction) + \"s\");");
            Line(sb, indent, "}");
            sb.AppendLine();

            Line(sb, indent, $"private static global::System.TimeSpan ReadJsonDuration(ref {JsonReader} reader)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "var text = reader.GetString();");
            Line(sb, indent + 1, "if (text.EndsWith(\"s\", global::System.StringComparison.Ordinal)) text = text.Substring(0, text.Length - 1);");
            Line(sb, indent + 1, $"return global::System.TimeSpan.FromTicks((long)(decimal.Parse(text, {Invariant}) * global::System.TimeSpan.TicksPerSecond));");
            Line(sb, indent, "}");
            sb.AppendLine();

            // The fractional part of a Timestamp or a Duration, which share one rule: 0, 3, 6 or 9
            // digits - "the smallest number that can represent the value exactly". Getting this wrong
            // is not cosmetic in either direction. Emitting nine digits where six would do disagrees
            // with every other implementation's *text*; emitting six where the value needs more
            // TRUNCATES, and .NET's 100ns tick needs seven, so the nine-digit form is the only one
            // that can carry a tick-precision value at all. Both mistakes were made here first.
            Line(sb, indent, "private static string JsonFraction(long ticks)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "if (ticks == 0) return \"\";");
            Line(sb, indent + 1, $"if (ticks % 10000 == 0) return \".\" + (ticks / 10000).ToString(\"000\", {Invariant});");
            Line(sb, indent + 1, $"if (ticks % 10 == 0) return \".\" + (ticks / 10).ToString(\"000000\", {Invariant});");
            Line(sb, indent + 1, $"return \".\" + (ticks * 100).ToString(\"000000000\", {Invariant});");
            Line(sb, indent, "}");
            sb.AppendLine();

            // protobuf-net's GuidString form, mirrored: empty payload for Guid.Empty, 'D' otherwise.
            // The read is forgiving in the same way GuidHelper.Read is, which accepts the 32-char
            // unhyphenated form as well as the 36-char one
            Line(sb, indent, "private static string JsonGuid(global::System.Guid value)");
            Line(sb, indent + 1, "=> value == global::System.Guid.Empty ? \"\" : value.ToString();");
            sb.AppendLine();

            Line(sb, indent, $"private static global::System.Guid ReadJsonGuid(ref {JsonReader} reader)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "var text = reader.GetString();");
            Line(sb, indent + 1, "return string.IsNullOrEmpty(text) ? global::System.Guid.Empty : global::System.Guid.Parse(text);");
            Line(sb, indent, "}");
            sb.AppendLine();

            // "has any element" without demanding a Count, since the declared type may be an
            // interface the reader will satisfy with a List
            Line(sb, indent, "private static bool JsonAny<T>(global::System.Collections.Generic.IEnumerable<T> items)");
            Line(sb, indent, "{");
            Line(sb, indent + 1, "foreach (var unused in items) return true;");
            Line(sb, indent + 1, "return false;");
            Line(sb, indent, "}");
        }
    }
}
