using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// Pins the packed fixed-width block-copy path against <b>hand-computed wire bytes</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why hand-computed rather than differential.</b> The AOT differential compares the
    /// generated model against <c>RuntimeTypeModel</c> — but both go through
    /// <c>RepeatedSerializer</c>, so a wrong block copy would be wrong identically on both sides
    /// and pass. The same is true of a round trip, which would agree with itself. The only oracle
    /// that can catch it is the encoding rules themselves.
    /// </para>
    /// <para>
    /// The values are chosen so a byte-order mistake cannot hide: <c>1f</c> is
    /// <c>00-00-80-3F</c> little-endian and <c>3F-80-00-00</c> big-endian, which are different in
    /// every position that matters.
    /// </para>
    /// </remarks>
    public class PackedBlockCopyTests
    {
        [ProtoContract]
        public class Floats
        {
            [ProtoMember(1, IsPacked = true)] public float[] Values { get; set; }
        }

        [ProtoContract]
        public class Doubles
        {
            [ProtoMember(1, IsPacked = true)] public double[] Values { get; set; }
        }

        [ProtoContract]
        public class Fixed32s
        {
            [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.FixedSize)]
            public int[] Values { get; set; }
        }

        private static string Serialize<T>(T value)
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(T), true);
            using var ms = new MemoryStream();
            model.Serialize(ms, value);
            return BitConverter.ToString(ms.ToArray());
        }

        [Fact]
        public void PackedFloatsAreLittleEndianIeee754()
        {
            // tag: field 1, wire type 2 (length-delimited) => 0x0A
            // len: two floats * 4 bytes  => 0x08
            // 1f  => 00-00-80-3F   |   2f => 00-00-00-40   (little-endian IEEE-754)
            Assert.Equal("0A-08-00-00-80-3F-00-00-00-40",
                Serialize(new Floats { Values = [1f, 2f] }));
        }

        [Fact]
        public void PackedDoublesAreLittleEndianIeee754()
        {
            // 1d => 00-00-00-00-00-00-F0-3F ; 2d => 00-00-00-00-00-00-00-40
            Assert.Equal("0A-10-00-00-00-00-00-00-F0-3F-00-00-00-00-00-00-00-40",
                Serialize(new Doubles { Values = [1d, 2d] }));
        }

        /// <summary>
        /// A SINGLE element is deliberately written unpacked, even on a packed member — tag+value
        /// beats tag+length+value, and packing is the writer's choice so both are legal.
        /// </summary>
        /// <remarks>
        /// The condition is <c>(count == 0 || count &gt; 1)</c> in <c>RepeatedSerializer.Write</c>.
        /// Pinned here because it was found by a wrong expectation in the test above: the payload
        /// came back as <c>09-…</c>, i.e. wire type 1 (Fixed64) rather than 2 (length-delimited),
        /// which is only explicable if the packed branch was skipped.
        /// </remarks>
        [Fact]
        public void ASinglePackedElementIsWrittenUnpacked()
        {
            // tag 09 = field 1, wire type 1 (Fixed64) - NOT 0A, which would be length-delimited
            Assert.Equal("09-00-00-00-00-00-00-F0-3F",
                Serialize(new Doubles { Values = [1d] }));
        }

        /// <summary>An EMPTY packed collection writes a zero-length header, not nothing.</summary>
        /// <remarks>
        /// This is the shape gap B1 was filed against, claiming we emitted it where ref-emit did
        /// not. Both paths are this one code, so there was never a disagreement to find - and the
        /// spec is silent on the empty case, so either form is readable.
        /// </remarks>
        [Fact]
        public void AnEmptyPackedCollectionWritesAZeroLengthHeader()
            => Assert.Equal("0A-00", Serialize(new Doubles { Values = [] }));

        [Fact]
        public void PackedFixed32IsLittleEndianTwosComplement()
        {
            // 1 => 01-00-00-00 ; -1 => FF-FF-FF-FF ; 258 => 02-01-00-00
            Assert.Equal("0A-0C-01-00-00-00-FF-FF-FF-FF-02-01-00-00",
                Serialize(new Fixed32s { Values = [1, -1, 258] }));
        }

        [ProtoContract]
        public class CrossWidth
        {
            [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.FixedSize)]
            public double[] Values { get; set; }
        }

        /// <summary>
        /// A C# <c>double</c> member CANNOT be narrowed onto a <c>float</c> field by any
        /// <c>[ProtoMember]</c> option — which is what makes the cross-width write path
        /// unreachable from ordinary attribute-driven code.
        /// </summary>
        /// <remarks>
        /// <c>ValueMember</c> calls <c>GetIntWireType</c> — the only thing that consults
        /// <c>DataFormat</c> for a width — from the INTEGER cases alone; <c>Single</c> and
        /// <c>Double</c> assign <c>Fixed32</c>/<c>Fixed64</c> unconditionally. So
        /// <c>WriteDouble</c>'s documented <c>Fixed32</c> support exists for models configured by
        /// other means (and for READING a payload whose schema says <c>float</c>), not for anything
        /// a consumer can express with an attribute.
        /// <para>
        /// Pinned because it decides the priority of gap B20 (SIMD narrow/widen): the shape it
        /// optimises is not one <c>[ProtoMember]</c> can produce.
        /// </para>
        /// </remarks>
        [Fact]
        public void ADoubleMemberIsAlwaysFixed64_EvenWithFixedSize()
        {
            // tag 0A = field 1 length-delimited (packed); body = 2 * EIGHT bytes, not 2 * four
            var hex = Serialize(new CrossWidth { Values = [1d, 2d] });
            Assert.StartsWith("0A-10-", hex);   // 0x10 = 16 bytes => Fixed64 per element
        }

        /// <summary>
        /// A payload long enough to cross the writer's buffer boundaries, so the block copy's slow
        /// arm (<c>ImplWriteBytes</c>) is exercised as well as the in-buffer one.
        /// </summary>
        [Fact]
        public void LargePackedBlockRoundTripsAndIsExactlyTheRawBytes()
        {
            var values = new float[8192];
            for (int i = 0; i < values.Length; i++) values[i] = i * 1.5f;

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Floats), true);

            using var ms = new MemoryStream();
            model.Serialize(ms, new Floats { Values = values });
            var payload = ms.ToArray();

            // header is tag + a varint length of 32768; the body must then be the array verbatim
            var expectedBody = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, expectedBody, 0, expectedBody.Length);
            // explicit offsets rather than a range operator: System.Index/System.Range do not
            // exist on net472, which this fixture also runs on
            var body = new byte[expectedBody.Length];
            Buffer.BlockCopy(payload, payload.Length - expectedBody.Length, body, 0, body.Length);
            Assert.Equal(BitConverter.ToString(expectedBody), BitConverter.ToString(body));

            ms.Position = 0;
            var back = model.Deserialize<Floats>(ms);
            Assert.Equal(values, back.Values);
        }

        [ProtoContract]
        public class Varints
        {
            [ProtoMember(1, IsPacked = true)] public uint[] U32 { get; set; }
            [ProtoMember(2, IsPacked = true)] public int[] I32 { get; set; }
            [ProtoMember(3, IsPacked = true)] public ulong[] U64 { get; set; }
            [ProtoMember(4, IsPacked = true)] public long[] I64 { get; set; }
        }

        /// <summary>
        /// The varint <b>block blit</b> (gaps.md B21 tier 1), swept across its block boundary and
        /// checked against a byte oracle built from the encoding rules.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Nothing that existed before covered this, and the gap was structural rather than an
        /// oversight: the blit only engages at <b>32 elements</b> (four vectors of eight, or eight
        /// of four), and every other packed fixture here is far shorter. The AOT differential
        /// cannot see it either - both sides go through <c>RepeatedSerializer</c>, so a wrong blit
        /// is wrong identically on both and compares equal. A round trip agrees with itself for the
        /// same reason.
        /// </para>
        /// <para>
        /// The lengths straddle the boundary deliberately (31 is one short of a block, 32 is
        /// exactly one, 33 and 47 leave ragged tails, 64 is two clean blocks), and the value
        /// patterns cover the three ways a block can fail to be uniform - a wide value first, in
        /// the middle, and in the tail after the last full block. The <c>negative</c> pattern is
        /// the one that matters most for <c>int32</c>: it must <i>never</i> blit, because a
        /// negative sign-extends to ten bytes, and what prevents it is an unsigned comparison
        /// rather than an explicit sign test.
        /// </para>
        /// </remarks>
        [Theory]
        [InlineData(31, "small")]
        [InlineData(32, "small")]
        [InlineData(33, "small")]
        [InlineData(47, "small")]
        [InlineData(64, "small")]
        [InlineData(999, "small")]
        [InlineData(64, "wideFirst")]
        [InlineData(64, "wideMiddle")]
        [InlineData(64, "wideTail")]
        [InlineData(64, "negative")]
        [InlineData(999, "negative")]
        [InlineData(999, "mixed")]
        // 128-255 is the band immediately ABOVE the one-byte cutoff, and it has to be here
        // explicitly: without it, widening the uniformity threshold from 0x80 to 0x100 changes
        // nothing anywhere in this theory, so the sweep could not tell a correct cutoff from a
        // wrong one. Found by sabotaging the threshold and watching the tests still pass.
        [InlineData(64, "justOver")]
        [InlineData(999, "justOver")]
        public void PackedVarintBlockMatchesTheEncodingRules(int count, string pattern)
        {
            var u32 = new uint[count];
            for (int i = 0; i < count; i++)
            {
                u32[i] = pattern switch
                {
                    "wideFirst" => i == 0 ? 300u : (uint)(i % 128),
                    "wideMiddle" => i == count / 2 ? 70000u : (uint)(i % 128),
                    "wideTail" => i == count - 2 ? uint.MaxValue : (uint)(i % 128),
                    "justOver" => (uint)(128 + (i % 128)),
                    "negative" => unchecked((uint)-(i + 1)),
                    "mixed" => (i & 3) switch
                    {
                        0 => (uint)(i % 128),
                        1 => 5000u,
                        2 => 1u << 20,
                        _ => uint.MaxValue,
                    },
                    _ => (uint)(i % 128),
                };
            }
            var i32 = Array.ConvertAll(u32, v => unchecked((int)v));
            var u64 = Array.ConvertAll(u32, v => (ulong)v | ((ulong)v << 33));
            var i64 = Array.ConvertAll(u64, v => unchecked((long)v));

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Varints), true);

            using var ms = new MemoryStream();
            model.Serialize(ms, new Varints { U32 = u32, I32 = i32, U64 = u64, I64 = i64 });
            var payload = ms.ToArray();

            var expected = new MemoryStream();
            AppendField(expected, 1, Encode(Array.ConvertAll(u32, v => (ulong)v)));
            // a negative int32 sign-extends to the full 64-bit form: that IS the encoding, and it
            // is what the blit must decline to take a shortcut through
            AppendField(expected, 2, Encode(Array.ConvertAll(i32, v => unchecked((ulong)(long)v))));
            AppendField(expected, 3, Encode(u64));
            AppendField(expected, 4, Encode(Array.ConvertAll(i64, v => unchecked((ulong)v))));

            Assert.Equal(
                BitConverter.ToString(expected.ToArray()),
                BitConverter.ToString(payload));

            ms.Position = 0;
            var back = model.Deserialize<Varints>(ms);
            Assert.Equal(u32, back.U32);
            Assert.Equal(i32, back.I32);
            Assert.Equal(u64, back.U64);
            Assert.Equal(i64, back.I64);
        }

        /// <summary>The oracle: LEB128 by the book, one byte at a time, no vectors anywhere.</summary>
        private static byte[] Encode(ulong[] values)
        {
            var ms = new MemoryStream();
            foreach (var value in values)
            {
                var v = value;
                while (v >= 0x80) { ms.WriteByte((byte)((v & 0x7F) | 0x80)); v >>= 7; }
                ms.WriteByte((byte)v);
            }
            return ms.ToArray();
        }

        private static void AppendField(MemoryStream target, int fieldNumber, byte[] body)
        {
            // tag then length, both varints, run together exactly as the wire wants them
            var header = Encode(new[] { (ulong)((fieldNumber << 3) | 2), (ulong)body.Length });
            target.Write(header, 0, header.Length);
            target.Write(body, 0, body.Length);
        }

        public enum Level { None = 0, Low = 1, Mid = 2, High = 3 }

        [ProtoContract]
        public class Enums
        {
            [ProtoMember(1, IsPacked = true)] public Level[] Values { get; set; }
        }

        /// <summary>
        /// Are enums packed? <c>notes/packed-writes.md</c> and gaps.md B1 both recorded that they
        /// are not, because <c>EnumSerializer</c> "is not an <c>IMeasuringSerializer</c>".
        /// </summary>
        /// <remarks>
        /// Pinned rather than assumed because the recorded reason does not survive reading the
        /// code: <c>TypeHelper.CanBePacked</c> returns true for <c>type.IsEnum</c> outright, and
        /// the concrete <c>EnumSerializer&lt;TEnum, TRaw&gt;</c> does implement
        /// <c>IMeasuringSerializer&lt;TEnum&gt;</c> - it is only the public abstract base that does
        /// not, and the runtime check at <c>RepeatedSerializer</c> tests the instance.
        /// </remarks>
        [Fact]
        public void PackedEnumsAreActuallyPacked()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Enums), true);

            using var ms = new MemoryStream();
            model.Serialize(ms, new Enums { Values = [Level.Low, Level.Mid, Level.High] });

            // packed: tag 1 wire-type 2, length 3, then the three values
            Assert.Equal("0A-03-01-02-03", BitConverter.ToString(ms.ToArray()));
        }
    }
}
