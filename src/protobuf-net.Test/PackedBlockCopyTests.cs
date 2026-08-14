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
    }
}
