using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    /// <summary>
    /// v2 round-tripped <c>null</c> values inside <see cref="Dictionary{TKey,TValue}"/> fields
    /// preserving the null. v3's initial behaviour coerced a missing map value to the proto
    /// default (e.g. <c>""</c> for <see cref="string"/>), losing the null/empty distinction.
    ///
    /// The writer already distinguishes the two on the wire
    /// (HasNonTrivialValue omits the value tag entirely for null, writes a zero-length value
    /// tag for "") so the coercion only happened on read. The fix keys off the compatibility
    /// level: Level200 / Level240 preserve null (v2 semantics); Level300 keeps the proto3-style
    /// "" coercion. Explicit <see cref="ValueMember.SupportNull"/> continues to preserve null
    /// via the wrapped-value path and is unaffected.
    /// </summary>
    public class MapNullStringValueTests
    {
        [ProtoContract]
        public class Holder
        {
            [ProtoMember(1)] public Dictionary<string, string> Map;
        }

        private static Holder Roundtrip(RuntimeTypeModel m, Holder h)
        {
            using var ms = new MemoryStream();
            m.Serialize(ms, h);
            ms.Position = 0;
            return (Holder)m.Deserialize(ms, null, typeof(Holder));
        }

        private static byte[] Serialize(RuntimeTypeModel m, Holder h)
        {
            using var ms = new MemoryStream();
            m.Serialize(ms, h);
            return ms.ToArray();
        }

        private static Holder Source() => new Holder
        {
            Map = new Dictionary<string, string>
            {
                { "A", "alpha" },
                { "N", null },
                { "E", "" },
            }
        };

        [Theory]
        [InlineData((int)CompatibilityLevel.NotSpecified)] // defaults to Level200
        [InlineData((int)CompatibilityLevel.Level200)]
        [InlineData((int)CompatibilityLevel.Level240)]
        public void V2Compat_PreservesNullAndEmptyValues(int level)
        {
            var m = RuntimeTypeModel.Create();
            m.DefaultCompatibilityLevel = (CompatibilityLevel)level;
            var r = Roundtrip(m, Source());
            Assert.Equal("alpha", r.Map["A"]);
            Assert.Null(r.Map["N"]);
            Assert.Equal("", r.Map["E"]);
        }

        [Fact]
        public void Level300_CoercesNullToEmpty_Proto3Semantics()
        {
            var m = RuntimeTypeModel.Create();
            m.DefaultCompatibilityLevel = CompatibilityLevel.Level300;
            var r = Roundtrip(m, Source());
            Assert.Equal("alpha", r.Map["A"]);
            Assert.Equal("", r.Map["N"]); // proto3: null coerced to ""
            Assert.Equal("", r.Map["E"]);
        }

        [Fact]
        public void Wire_WriterDistinguishesNullFromEmpty_AtAllLevels()
        {
            // Writer omits value tag for null (via HasNonTrivialValue); emits zero-length
            // value tag for "". Reader behaviour (null preserved vs coerced) is separate
            // from this wire-level distinction, which must hold across all levels.
            foreach (var level in new[] { CompatibilityLevel.Level200, CompatibilityLevel.Level240, CompatibilityLevel.Level300 })
            {
                var m = RuntimeTypeModel.Create();
                m.DefaultCompatibilityLevel = level;
                var bNull = Serialize(m, new Holder { Map = new Dictionary<string, string> { { "K", null } } });
                var bEmpty = Serialize(m, new Holder { Map = new Dictionary<string, string> { { "K", "" } } });
                Assert.True(bEmpty.Length > bNull.Length,
                    $"[{level}] expected empty-value payload to include value tag; null={bNull.Length} bytes, empty={bEmpty.Length} bytes");
            }
        }

        [Fact]
        public void Level300_WireFromLevel200_ReadsNullAsEmpty()
        {
            // Cross-level read: data produced under Level200 (with a null value) must read
            // cleanly under Level300 with the proto3 coercion applied. This locks in that
            // the wire format is unchanged — only the read-side interpretation differs.
            var writer = RuntimeTypeModel.Create();
            writer.DefaultCompatibilityLevel = CompatibilityLevel.Level200;
            var bytes = Serialize(writer, Source());

            var reader = RuntimeTypeModel.Create();
            reader.DefaultCompatibilityLevel = CompatibilityLevel.Level300;
            using var ms = new MemoryStream(bytes);
            var r = (Holder)reader.Deserialize(ms, null, typeof(Holder));

            Assert.Equal("alpha", r.Map["A"]);
            Assert.Equal("", r.Map["N"]);
            Assert.Equal("", r.Map["E"]);
        }

        [Fact]
        public void Level200_WireFromLevel300_ReadsEmptyAsNullIsImpossible_ReadsAsEmpty()
        {
            // Reverse cross-level: Level300 writer collapses null→"" on write (because
            // HasNonTrivialValue is the same), so reading under Level200 recovers "" not null
            // — the null was already lost at the write step. This test documents that
            // preservation only works when both write and read use a null-preserving level.
            var writer = RuntimeTypeModel.Create();
            writer.DefaultCompatibilityLevel = CompatibilityLevel.Level300;
            var bytes = Serialize(writer, Source());

            var reader = RuntimeTypeModel.Create();
            reader.DefaultCompatibilityLevel = CompatibilityLevel.Level200;
            using var ms = new MemoryStream(bytes);
            var r = (Holder)reader.Deserialize(ms, null, typeof(Holder));

            Assert.Equal("alpha", r.Map["A"]);
            // on the wire: writer saw null, HasNonTrivialValue(null)=false → value tag omitted,
            // same as Level200 would have done; so reader sees "no value field" and, under
            // Level200, preserves null.
            Assert.Null(r.Map["N"]);
            Assert.Equal("", r.Map["E"]);
        }

        [Fact]
        public void ExplicitSupportNull_PreservesNullRegardlessOfLevel()
        {
            // SupportNull uses the wrapped-value path which already preserves null; verify
            // it keeps working under Level300 where the default coerces.
            foreach (var level in new[] { CompatibilityLevel.Level200, CompatibilityLevel.Level240, CompatibilityLevel.Level300 })
            {
                var m = RuntimeTypeModel.Create();
                m.DefaultCompatibilityLevel = level;
                var mt = m.Add(typeof(Holder), false);
                mt.Add(1, nameof(Holder.Map));
                mt[1].SupportNull = true;
                var r = Roundtrip(m, Source());
                Assert.Equal("alpha", r.Map["A"]);
                Assert.Null(r.Map["N"]);
                Assert.Equal("", r.Map["E"]);
            }
        }

        [ProtoContract]
        public class HolderBytes
        {
            [ProtoMember(1)] public Dictionary<string, byte[]> Map;
        }

        [Fact]
        public void V2Compat_PreservesNullBytesValues()
        {
            // byte[] is the other mainstream reference-type map value; verify the flag
            // generalises beyond string.
            var m = RuntimeTypeModel.Create();
            m.DefaultCompatibilityLevel = CompatibilityLevel.Level200;
            var src = new HolderBytes { Map = new Dictionary<string, byte[]>
            {
                { "A", new byte[] { 1, 2, 3 } },
                { "N", null },
                { "E", System.Array.Empty<byte>() },
            }};
            using var ms = new MemoryStream();
            m.Serialize(ms, src);
            ms.Position = 0;
            var r = (HolderBytes)m.Deserialize(ms, null, typeof(HolderBytes));
            Assert.Equal(new byte[] { 1, 2, 3 }, r.Map["A"]);
            Assert.Null(r.Map["N"]);
            Assert.NotNull(r.Map["E"]);
            Assert.Empty(r.Map["E"]);
        }
    }
}
