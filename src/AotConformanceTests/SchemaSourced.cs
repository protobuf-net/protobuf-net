using ProtoBuf;
using ProtoBuf.Meta;

namespace ProtoBuf.AotConformance.SchemaSourced
{
    /// <summary>
    /// A model built from a <c>.proto</c> in this same project (notes/aot-schema-model.md) - the
    /// one-project shape, and the only place the schema-sourced path is checked on BYTES.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here declares a contract in C#: <c>Schemas/conformance.proto</c> is an
    /// <c>AdditionalFiles</c> item, so <c>ProtoFileGenerator</c> emits the DTOs and
    /// <c>[ProtoSchema]</c> makes <c>ProtoModelGenerator</c> emit serializers for the same schema.
    /// Neither generator can see the other's output; the compiler joins them.
    /// </para>
    /// <para>
    /// <c>DifferentialTests</c> discovers this model like any other and compares it against
    /// <c>RuntimeTypeModel</c> over the generated DTOs, in both directions - which is the only
    /// check that can catch a wrong write GUARD, as opposed to a wrong name.
    /// </para>
    /// </remarks>
    [ProtoModel, ProtoSchema("conformance.proto"), ProtoSchema("legacy.proto")]
    public partial class SchemaSourcedModel : TypeModel { }

    /// <summary>
    /// Concrete instances, which the schema alone cannot supply.
    /// </summary>
    /// <remarks>
    /// Chosen so the guards are actually exercised rather than merely present: the empty-string
    /// case is the one that separates <c>!= ""</c> from <c>!= null</c>, and the all-defaults case
    /// is the one that should write no bytes at all.
    /// </remarks>
    public static class SchemaSourcedSamples
    {
        public static object[] Values =>
        [
            // everything populated, and every scalar distinct so a swapped field number shows
            new global::Conformance.Sample
            {
                Id = 42,
                Name = "the quick brown fox",
                Active = true,
                Balance = 1234.5678,
                Ratio = 2.5f,
                Ticks = 9_876_543_210L,
                Count = 7u,
                Total = 18_000_000_000UL,
                Blob = [1, 2, 3, 4, 5],
                Grade = global::Conformance.Grade.GradeHigh,
                Detail = new global::Conformance.Detail { Depth = 3, Note = "nested" },
                Delta = -17,
                Marker = 0xDEADBEEF,
                Offset = -123_456_789L,
                Nums = [1, -2, 300, -40_000],
                Reals = [1.5, -2.25],
                Names = { "alpha", "beta" },
                Blobs = { new byte[] { 9, 8 }, System.Array.Empty<byte>() },
                Details =
                {
                    new global::Conformance.Detail { Depth = 1, Note = "one" },
                    new global::Conformance.Detail(),
                },
            },

            // repeated members present but EMPTY: an empty array and an empty list must write
            // nothing, which is a different guard from "absent"
            new global::Conformance.Sample { Id = 2, Nums = [] },

            // a single-element packed run, which is where the length prefix is easiest to get wrong
            new global::Conformance.Sample { Id = 3, Nums = [7], Names = { "solo" } },

            // the pluralised member: `repeated int32 tally` is emitted as Tallies
            new global::Conformance.Sample { Id = 4, Tallies = [11, 22] },

            // nested message and nested enum, emitted as Sample.Node / Sample.Flavour
            new global::Conformance.Sample
            {
                Id = 5,
                // lowercase: `Node` would collide with the nested TYPE Node, so protogen
                // keeps the original name. GetName(FieldDescriptorProto) does this for us
                node = new global::Conformance.Sample.Node { Weight = 8, Tag = "n" },
                flavour = global::Conformance.Sample.Flavour.FlavourSalt,
                Nodes =
                {
                    new global::Conformance.Sample.Node { Weight = 1 },
                    new global::Conformance.Sample.Node { Tag = "two" },
                },
            },

            // maps, including the bool-keyed one that protobuf-net does not consider a valid
            // protobuf map. Keys are disjoint per sample because the differential manufactures
            // repeated fields by concatenating payloads, and AddRange throws on a repeated key
            new global::Conformance.Sample
            {
                Id = 6,
                Counts = { { "a", 1 }, { "b", 2 } },
                Lookups = { { 7, new global::Conformance.Detail { Depth = 4, Note = "in map" } } },
                Flags = { { true, "yes" } },
            },

            new global::Conformance.Sample.Node { Weight = 99, Tag = "standalone nested" },
            new global::Conformance.Sample.Node(),

            // THE guard case: an empty string must write nothing, because proto3 emits
            // [DefaultValue("")]. A plan that omitted the declared default writes two bytes here
            // and disagrees with ref-emit while compiling perfectly
            new global::Conformance.Sample { Id = 1, Name = "" },

            // negative and boundary values: the signed varints sign-extend, the fixed ones do not
            new global::Conformance.Sample
            {
                Id = -1,
                Ticks = long.MinValue,
                Delta = int.MinValue,
                Offset = long.MaxValue,
                Marker = uint.MaxValue,
                Count = uint.MaxValue,
                Total = ulong.MaxValue,
            },

            // an all-defaults instance writes nothing at all
            new global::Conformance.Sample(),

            // out-of-order field declarations, which must still write in field-number order
            new global::Conformance.Shuffled
            {
                First = 1,
                Middle = true,
                Nested = new global::Conformance.Detail { Depth = 2, Note = "x" },
                Last = "z",
            },
            new global::Conformance.Shuffled(),

            // presence tracking: the cases that only work because ShouldSerialize REPLACES the
            // trivial-value guard. An explicitly-set ZERO and an explicitly-set EMPTY STRING must
            // both go on the wire - an ordinary proto3 field would drop them
            new global::Conformance.Presence { Always = 1, PickNumber = 0 },
            new global::Conformance.Presence { Always = 2, PickText = "" },
            new global::Conformance.Presence { Always = 3, PickText = "chosen" },
            new global::Conformance.Presence
            {
                Always = 4,
                PickMessage = new global::Conformance.Detail { Depth = 1, Note = "in oneof" },
            },
            new global::Conformance.Presence { Always = 5, MaybeNumber = 0 },
            new global::Conformance.Presence { Always = 6, MaybeText = "" },
            new global::Conformance.Presence { Always = 7, MaybeFlag = false },
            new global::Conformance.Presence { Always = 8, MaybeNumber = 42, MaybeFlag = true },
            // nothing set at all: none of the tracked members may be written
            new global::Conformance.Presence(),

            new global::Conformance.Detail { Depth = 9, Note = "standalone" },
            new global::Conformance.Detail(),

            // ---- proto2 (Schemas/legacy.proto) ----------------------------------------------
            // Both of these are chosen so the GUARD can fail, which is the only way this half of
            // the gate is worth anything: a wrong guard compiles perfectly and round-trips
            // perfectly against itself.

            // `required` DROPS the write guard, so an all-zero Required must still write every
            // member. A plan that treats these as ordinary optionals writes nothing at all here
            new global::Legacy.Required(),
            new global::Legacy.Required { Id = 5, Name = "named", Flag = true, Spare = 1 },
            // required present, optional absent - the two must be distinguishable
            new global::Legacy.Required { Id = 0, Name = "", Flag = false },

            // presence, not value: protogen backs a defaulted proto2 optional with a nullable
            // field and ShouldSerialize, so a value EQUAL to the declared default must still be
            // written once assigned. This is exactly what a `!= default` guard gets wrong
            new global::Legacy.Defaulted { Count = 7, Label = "unnamed", Enabled = true, Ratio = 1.5 },
            // ...and the opposite corner: values that differ from the declared defaults
            new global::Legacy.Defaulted
            {
                Count = 0,
                Label = "",
                Enabled = false,
                Ratio = 0,
                Shade = global::Legacy.Shade.ShadeRed,
                Plain = 0,
            },
            // nothing assigned: no member may be written, however non-zero its default
            new global::Legacy.Defaulted(),
        ];
    }
}
