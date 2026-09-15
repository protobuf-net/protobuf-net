#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Aot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        /// <summary>The JSON seam, which lives in protobuf-net.Connect rather than in Core.</summary>
        /// <remarks>
        /// Probed rather than referenced, exactly as <c>UnsafeAccessorAttribute</c> and
        /// <c>BclHelpers.ReadDateOnly</c> are: a model in a project that has never heard of Connect
        /// emits no JSON half at all, and so pays nothing for a feature it cannot use. That also
        /// keeps <c>System.Text.Json</c> off protobuf-net.Core's dependency graph, which is the
        /// reason the seam is not declared there.
        /// </remarks>
        internal const string JsonSerializerInterfaceName = "ProtoBuf.Connect.IJsonSerializer`1";

        private const string ProtoEnumAttributeName = "ProtoBuf.ProtoEnumAttribute";

        /// <summary>
        /// The JSON key for a proto field name, by protoc's rule.
        /// </summary>
        /// <remarks>
        /// <b>This does not lowercase the first letter</b>, and that is the whole trap. "Canonical
        /// JSON is lowerCamelCase" is the universal summary and it is wrong at character one: the
        /// algorithm removes underscores and uppercases whatever follows one, and does nothing else.
        /// So <c>UserName</c> stays <c>UserName</c> while <c>already_snake</c> becomes
        /// <c>alreadySnake</c>.
        /// <para>
        /// It matters here more than anywhere else, because protobuf-net puts the <b>C# member name
        /// verbatim</b> into the schema it generates - so code-first fields arrive at this function
        /// already PascalCase, which is exactly the input the "obvious" implementation gets wrong.
        /// Writing <c>char.ToLowerInvariant(name[0]) + name[1..]</c> would emit <c>userName</c> where
        /// every other implementation emits <c>UserName</c>, on every field of every code-first
        /// contract, and would round-trip perfectly against itself. See findings §48.
        /// </para>
        /// </remarks>
        internal static string ToJsonName(string protoName)
        {
            var sb = new StringBuilder(protoName.Length);
            var capitalizeNext = false;
            foreach (var c in protoName)
            {
                if (c == '_') capitalizeNext = true;
                else if (capitalizeNext)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    capitalizeNext = false;
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>The schema name of a member: what the consumer pinned, else the C# name.</summary>
        internal static string SchemaNameOf(ProtoMemberPlan member) => member.SchemaName ?? member.Name;

        /// <summary>
        /// Decides which contracts can carry the canonical JSON mapping, and gathers the enum name
        /// tables that mapping needs.
        /// </summary>
        /// <remarks>
        /// A second, narrower surface over the same plan. It is narrower because several shapes
        /// protobuf-net serializes perfectly well in binary have <b>no canonical JSON at all</b> -
        /// there is nothing to emit, and inventing something would break interop silently, which is
        /// the one failure mode this whole exercise exists to avoid.
        /// </remarks>
        private static void PlanJson(
            Dictionary<string, ProtoContractPlan> parsed,
            Dictionary<string, INamedTypeSymbol> symbols,
            Compilation compilation,
            Dictionary<string, PlanLocation> locations,
            List<PlanDiagnostic> diagnostics,
            out HashSet<string> jsonContracts,
            out ProtoJsonEnumPlan[] jsonEnums)
        {
            var enums = new Dictionary<string, ProtoJsonEnumPlan>(StringComparer.Ordinal);
            jsonContracts = new HashSet<string>(StringComparer.Ordinal);

            foreach (var pair in parsed)
            {
                if (JsonRefusal(pair.Value) is { } reason)
                {
                    Report(pair.Key, reason);
                    continue;
                }
                if (!TryCollectEnums(pair.Value, symbols, compilation, enums))
                {
                    Report(pair.Key, "an enum it reaches could not be resolved, and canonical JSON "
                        + "writes an enum by name rather than by number");
                    continue;
                }
                jsonContracts.Add(pair.Key);
            }

            void Report(string key, string reason)
                => diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.JsonOmitted,
                    locations.TryGetValue(key, out var at) ? at : default, key, reason));

            // Cascade, to a fixed point: a contract whose member type has no JSON serializer cannot
            // have one either - the emitted call would name an IJsonSerializer<T> the services type
            // does not implement, which is a build error in the consumer's project rather than a
            // missing feature. Exactly DropUnsatisfiable's argument, one surface over.
            bool changed;
            do
            {
                changed = false;
                foreach (var key in jsonContracts.ToArray())
                {
                    foreach (var member in parsed[key].Members)
                    {
                        var referenced = member.Map.Factory is not null
                            ? MessageSides(member)
                            : member.Kind == ProtoMemberKind.Message ? new[] { member.TypeName } : null;
                        if (referenced is null) continue;

                        foreach (var name in referenced)
                        {
                            if (name is null || !parsed.ContainsKey(name)) continue;
                            if (jsonContracts.Contains(name)) continue;
                            jsonContracts.Remove(key);
                            Report(key, $"'{name}', which it references, has none either");
                            changed = true;
                            goto nextContract;
                        }
                    }
                    nextContract: ;
                }
            }
            while (changed);

            jsonEnums = enums.Values.OrderBy(static x => x.TypeName, StringComparer.Ordinal).ToArray();
        }

        /// <summary>The contract types a map member reaches, on either side.</summary>
        private static string?[] MessageSides(ProtoMemberPlan member)
            => new[]
            {
                member.Map.KeyKind == ProtoMemberKind.Message ? member.Map.KeyTypeName : null,
                member.Map.ValueKind == ProtoMemberKind.Message ? member.Map.ValueTypeName : null,
            };

        /// <summary>
        /// Why this contract has no canonical JSON mapping, or null if it has one.
        /// </summary>
        /// <remarks>
        /// Every entry here is a shape with <em>no specified JSON form</em>, not a shape we have not
        /// got to. That distinction is the reason this is a list of refusals rather than a backlog:
        /// protobuf-net's sub-type framing, its null wrappers and its retained unknown fields are
        /// protobuf-net extensions to protobuf, and canonical JSON is defined over protobuf.
        /// </remarks>
        private static string? JsonRefusal(ProtoContractPlan contract)
        {
            if (contract.RootTypeName is not null)
            {
                return "it takes part in a [ProtoInclude] hierarchy, and protobuf-net's sub-type "
                    + "framing is an extension to protobuf that canonical JSON has no form for";
            }
            if (contract.Extensible != ProtoExtensibleKind.None)
            {
                return "it is extensible, and retained unknown fields are raw protobuf bytes with no "
                    + "schema - so there is nothing canonical JSON could write them as";
            }
            if (contract.ExternalSerializerTypeName is not null)
            {
                return "it is served by a hand-written serializer, whose JSON form is not knowable here";
            }
            // An auto-tuple's READ is a different emit shape - locals per constructor parameter,
            // constructed at the end - because there is nothing to assign to. The JSON reader assigns,
            // so emitting one produced CS0200 and CS7036 in the consumer's build: broken code, not a
            // missing feature. Refused until that shape is written; the write half already works.
            if (contract.IsTuple)
            {
                return "it is an auto-tuple, whose JSON read needs the construct-at-the-end shape the "
                    + "binary path uses; the reader here assigns to members, and a tuple has none to "
                    + "assign to";
            }
            if (contract.IsGroup)
            {
                return "it is a group, which is a wire-format framing with no JSON counterpart";
            }

            foreach (var member in contract.Members)
            {
                if (MemberJsonRefusal(member) is { } reason)
                {
                    return $"member '{member.Name}' {reason}";
                }
            }
            return null;
        }

        private static string? MemberJsonRefusal(ProtoMemberPlan member)
        {
            if (member.WrappedValue || member.WrappedCollection)
            {
                return "is null-wrapped, which is a protobuf-net extension with no canonical JSON form";
            }

            if (member.Map.Factory is not null)
            {
                // canonical JSON writes a map as a JSON object, so a dictionary protobuf-net does not
                // model as a `map` has nowhere to go - it is a `repeated KeyValuePair_K_V` in the
                // schema, and canonical JSON for that is an array of objects, not a map.
                //
                // Note the wording: protobuf-net's notion of a valid map key is NARROWER than
                // protobuf's in places (bool, char, nint and nuint are all legal protobuf map keys
                // and are modelled as repeated pairs anyway), so "protobuf cannot express this" would
                // be false. The refusal is about what protobuf-net emits, not about what the spec
                // allows.
                if (!member.Map.IsValidProtobufMap || member.DisableMap)
                {
                    return "is a dictionary protobuf-net does not model as a protobuf `map` - it is a "
                        + "repeated key/value pair in the schema, and canonical JSON has a map form "
                        + "only for a map";
                }
                // An ENUM KEY is not a protobuf map key, whatever protobuf-net thinks. Its
                // IsValidProtobufMap accepts one and GetProto duly emits `map<Shade,int32>` - which
                // protoc rejects outright: "Key in map fields cannot be enum types." Verified against
                // plain protoc 35.1 over every candidate key type; an enum is the ONLY shape where
                // protobuf-net emits a schema protoc will not compile. So there is no canonical JSON
                // for this shape because there is no valid schema for it. (A protobuf-net schema bug
                // in its own right; see notes/connect/json-spike.md.)
                if (member.Map.KeyEnumTypeName is not null)
                {
                    return "is a dictionary with an enum key, which protobuf does not allow as a map "
                        + "key at all - protoc rejects the generated schema with \"Key in map fields "
                        + "cannot be enum types\", so there is no canonical JSON form for it";
                }

                var mapKind = JsonCollectionKindOf(member.DeclaredTypeName);
                if (mapKind is not (JsonCollectionKind.Dictionary or JsonCollectionKind.ReadOnlyDictionary))
                {
                    return "is a dictionary this JSON reader cannot construct; a Dictionary<K,V> or "
                        + "IDictionary<K,V> would work";
                }
                if (member.IsReadOnly && mapKind == JsonCollectionKind.ReadOnlyDictionary)
                {
                    return "is a getter-only read-only dictionary, so a JSON read would have nowhere "
                        + "to put what it read";
                }

                // `member` matters and was omitted here once: without it the kind tests read
                // CompatibilityLevel off a default(ProtoMemberPlan), i.e. 0, so every BCL type on
                // either side of a map was refused for being "level 0" whatever level it was
                // actually reached at
                return JsonKindRefusal(member.Map.KeyKind, "map key", member)
                    ?? JsonKindRefusal(member.Map.ValueKind, "map value", member);
            }

            if (member.Repeated.Factory is not null)
            {
                return JsonCollectionRefusal(member) ?? JsonKindRefusal(member.Kind, "element", member);
            }

            return JsonKindRefusal(member.Kind, "type", member);
        }

        /// <summary>
        /// Which collection shapes the JSON reader can actually produce.
        /// </summary>
        /// <remarks>
        /// <b>This is a whitelist, and it has to be.</b> The reader builds a <c>List&lt;T&gt;</c>;
        /// assigning that to a member declared as something else does not compile, and a generator
        /// emitting code the consumer's build rejects is the worst failure available - worse than
        /// emitting nothing, because it breaks a project that was building. Probed rather than
        /// reasoned about: a <c>HashSet&lt;int&gt;</c> and a <c>Queue&lt;int&gt;</c> member each
        /// produced CS0029 in the consuming project before this existed.
        /// <para>
        /// The <em>factory</em> cannot decide this on its own, which is why the declared type is what
        /// is tested: <c>CreateEnumerable</c> serves <c>IEnumerable&lt;T&gt;</c> and also anything
        /// that matched nothing else - <c>class MySet : HashSet&lt;int&gt;</c> among them - so trusting
        /// it would re-admit exactly the shapes this rules out.
        /// </para>
        /// </remarks>
        private static string? JsonCollectionRefusal(ProtoMemberPlan member)
        {
            var kind = JsonCollectionKindOf(member.DeclaredTypeName);
            if (kind == JsonCollectionKind.Unsupported)
            {
                return "is a collection this JSON reader cannot construct; an array, a List<T>, or one "
                    + "of the interfaces a List<T> satisfies would work";
            }

            // a getter-only collection is read by APPENDING into the instance the property already
            // holds, exactly as the binary path does - so it needs somewhere to append to
            if (member.IsReadOnly && kind is JsonCollectionKind.Array or JsonCollectionKind.ReadOnlyInterface)
            {
                return "is a getter-only collection with no Add, so a JSON read would have nowhere to "
                    + "put what it read";
            }
            return null;
        }

        internal enum JsonCollectionKind
        {
            Unsupported,
            /// <summary><c>T[]</c>: built as a list, then <c>ToArray</c>.</summary>
            Array,
            /// <summary><c>List&lt;T&gt;</c>, or an interface it satisfies that also has <c>Add</c>.</summary>
            List,
            /// <summary><c>IEnumerable&lt;T&gt;</c> and the read-only interfaces: assignable, but not appendable.</summary>
            ReadOnlyInterface,
            /// <summary><c>Dictionary&lt;K,V&gt;</c> or <c>IDictionary&lt;K,V&gt;</c>.</summary>
            Dictionary,
            /// <summary><c>IReadOnlyDictionary&lt;K,V&gt;</c>: assignable, but not appendable.</summary>
            ReadOnlyDictionary,
        }

        private const string Generic = "global::System.Collections.Generic.";

        internal static JsonCollectionKind JsonCollectionKindOf(string? declaredTypeName)
        {
            if (declaredTypeName is null) return JsonCollectionKind.Unsupported;
            if (declaredTypeName.EndsWith("[]", StringComparison.Ordinal)) return JsonCollectionKind.Array;

            if (Is("List<") || Is("IList<") || Is("ICollection<")) return JsonCollectionKind.List;
            if (Is("IEnumerable<") || Is("IReadOnlyList<") || Is("IReadOnlyCollection<"))
            {
                return JsonCollectionKind.ReadOnlyInterface;
            }
            if (Is("Dictionary<") || Is("IDictionary<")) return JsonCollectionKind.Dictionary;
            if (Is("IReadOnlyDictionary<")) return JsonCollectionKind.ReadOnlyDictionary;
            return JsonCollectionKind.Unsupported;

            bool Is(string name) => declaredTypeName.StartsWith(Generic + name, StringComparison.Ordinal);
        }

        /// <summary>Whether a scalar kind has a canonical JSON form.</summary>
        private static string? JsonKindRefusal(ProtoMemberKind kind, string what, ProtoMemberPlan member = default)
            => kind switch
            {
                // the compatibility level is what decides whether these are google.protobuf
                // well-known types (which have a JSON form) or protobuf-net's own messages (which do
                // not). Level 200 is the default, so this is the common case rather than a corner
                ProtoMemberKind.DateTime or ProtoMemberKind.TimeSpan when member.CompatibilityLevel < 240
                    => $"has a {what} whose compatibility level is {member.CompatibilityLevel}, where it is "
                        + "a protobuf-net message rather than a google.protobuf well-known type; raise it "
                        + "to 240 or above for a JSON form to exist",
                ProtoMemberKind.DateTime or ProtoMemberKind.TimeSpan => null,
                ProtoMemberKind.Guid or ProtoMemberKind.Decimal when member.CompatibilityLevel < 300
                    => $"has a {what} whose compatibility level is {member.CompatibilityLevel}, where it is "
                        + "a protobuf-net message rather than a string; raise it to 300 for a JSON form "
                        + "to exist",
                ProtoMemberKind.Guid or ProtoMemberKind.Decimal => null,
                ProtoMemberKind.DateOnly or ProtoMemberKind.TimeOnly
                    => $"has a {what} that protobuf-net encodes with its own BclHelpers form, which has "
                        + "no canonical JSON counterpart",
                _ => null,
            };

        /// <summary>
        /// Gathers the name table for every enum this contract reaches, failing the contract if any
        /// of them cannot be resolved back to a symbol.
        /// </summary>
        /// <remarks>
        /// Strict on purpose. The fallback if an enum's names are unavailable is to write the
        /// <em>number</em>, which is legal JSON that every reader accepts - and therefore exactly the
        /// kind of silently-wrong output that passes a round-trip test against itself and fails
        /// against a peer. Refusing the contract is the honest answer.
        /// </remarks>
        private static bool TryCollectEnums(
            ProtoContractPlan contract,
            Dictionary<string, INamedTypeSymbol> symbols,
            Compilation compilation,
            Dictionary<string, ProtoJsonEnumPlan> enums)
        {
            var names = new List<string>();
            foreach (var member in contract.Members)
            {
                if (member.EnumTypeName is { } inline) names.Add(inline);
                if (member.Map.KeyEnumTypeName is { } key) names.Add(key);
                if (member.Map.ValueEnumTypeName is { } value) names.Add(value);
            }
            if (names.Count == 0) return true;

            // the SURROGATE's symbol where there is one: the plan carries the surrogate's members,
            // so looking the enums up on the underlying type finds nothing and refuses the contract
            if (!symbols.TryGetValue(contract.SurrogateTypeName ?? contract.TypeName, out var owner)) return false;

            // resolve each enum name against the types this contract's members actually mention,
            // rather than by parsing the display string back into a symbol - the name is a
            // fully-qualified display form (and may carry an extern alias), so re-resolving it is a
            // string exercise with no correct answer
            var found = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
            foreach (var symbol in owner.GetMembers())
            {
                var type = symbol switch
                {
                    IPropertySymbol property => property.Type,
                    IFieldSymbol field => field.Type,
                    _ => null,
                };
                if (type is not null) CollectEnumTypes(compilation, type, found);
            }

            foreach (var name in names)
            {
                if (!found.TryGetValue(name, out var enumType)) return false;
                if (enums.ContainsKey(name)) continue;
                if (GetJsonEnumPlan(name, enumType) is not { } plan) return false;
                enums[name] = plan;
            }
            return true;
        }

        /// <summary>Every enum type mentioned by a type, including through generics and arrays.</summary>
        private static void CollectEnumTypes(
            Compilation compilation, ITypeSymbol type, Dictionary<string, INamedTypeSymbol> found)
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    CollectEnumTypes(compilation, array.ElementType, found);
                    return;
                case INamedTypeSymbol named:
                    if (named.TypeKind == TypeKind.Enum)
                    {
                        found[Qualified(compilation, named)] = named;
                        return;
                    }
                    foreach (var argument in named.TypeArguments)
                    {
                        CollectEnumTypes(compilation, argument, found);
                    }
                    return;
            }
        }

        private static ProtoJsonEnumPlan? GetJsonEnumPlan(string typeName, INamedTypeSymbol type)
        {
            var members = new List<ProtoJsonEnumMember>();
            var seen = new HashSet<long>();
            var isFlags = type.GetAttributes().Any(static x
                => x.AttributeClass?.ToDisplayString() == "System.FlagsAttribute");

            foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
            {
                if (!field.HasConstantValue || !field.IsConst) continue;

                // widened for de-duplication only; unchecked is injective, which is all that needs
                // to hold, and a ulong-backed enum is the only shape that reaches the wrap
                long value;
                unchecked
                {
                    value = field.ConstantValue switch
                    {
                        sbyte v => v, byte v => v, short v => v, ushort v => v,
                        int v => v, uint v => v, long v => v, ulong v => (long)v,
                        _ => 0,
                    };
                }

                // an alias - two names for one value - would emit two `case` labels of the same
                // constant, which does not compile. protobuf-net dropped value-aliasing support
                // (EnumPassthru's setter throws), so the first name is the only answer there is
                if (!seen.Add(value)) continue;

                var jsonName = field.GetAttributes()
                    .FirstOrDefault(static x => x.AttributeClass?.ToDisplayString() == ProtoEnumAttributeName)
                    ?.NamedArguments.FirstOrDefault(static x => x.Key == "Name").Value.Value as string;

                members.Add(new ProtoJsonEnumMember(field.Name, jsonName ?? field.Name, value));
            }

            var underlying = type.EnumUnderlyingType?.SpecialType switch
            {
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Byte => "byte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                _ => null,
            };
            // the CLR permits a char-backed enum that C# cannot declare, and the binary path already
            // refuses those; there is nothing to write here either
            if (underlying is null) return null;

            return new ProtoJsonEnumPlan(typeName, new(members.ToArray()), isFlags, underlying);
        }
    }
}
