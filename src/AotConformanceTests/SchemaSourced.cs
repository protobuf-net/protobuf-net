using ProtoBuf;
using ProtoBuf.Meta;

namespace ProtoBuf.AotConformance.SchemaSourced
{
    /// <summary>
    /// A model built from a <c>.proto</c> in this same project (docs/aot-schema-model.md) - the
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
    [ProtoModel, ProtoSchema("conformance.proto")]
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

            new global::Conformance.Detail { Depth = 9, Note = "standalone" },
            new global::Conformance.Detail(),
        ];
    }
}
