#if PLAT_SPANS

using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Tests
{
    /// <summary>
    /// <see cref="IBufferWriter{T}"/> destinations that do not give us what we asked for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The size passed to <c>GetMemory</c>/<c>GetSpan</c> is a <em>hint</em>. The documented
    /// contract is "at least this much, or throw", but that is a contract we do not control and
    /// cannot verify: a destination is free to be simplistic, and in the limit can hand back one
    /// byte at a time forever. Optimise for a friendly lease; survive an unfriendly one.
    /// </para>
    /// <para>
    /// Two shapes, and they want different answers. <em>Large but not large enough</em> is
    /// usable - anything at least as wide as the widest single op can be written into, we just
    /// re-lease more often. <em>Pathologically small</em> is not usable at all, so the writer
    /// leases its own region instead and pushes the fragmentation back onto the destination via
    /// <c>BuffersExtensions.Write</c>, which loops GetSpan/Advance and copes with any chunk size.
    /// </para>
    /// </remarks>
    public class HostileBufferWriterTests
    {
        /// <summary>Hands back exactly <c>Grant</c> bytes however much was asked for.</summary>
        private sealed class StingyBufferWriter : IBufferWriter<byte>
        {
            private readonly int _grant;
            private readonly MemoryStream _committed = new();
            private byte[] _current = Array.Empty<byte>();

            public StingyBufferWriter(int grant) => _grant = grant;

            public int Leases { get; private set; }
            public byte[] ToArray() => _committed.ToArray();

            public void Advance(int count)
            {
                if (count < 0 || count > _current.Length) throw new ArgumentOutOfRangeException(nameof(count));
                _committed.Write(_current, 0, count);
                _current = Array.Empty<byte>();
            }

            public Memory<byte> GetMemory(int sizeHint = 0) => Lease();
            public Span<byte> GetSpan(int sizeHint = 0) => Lease().Span;

            private Memory<byte> Lease()
            {
                Leases++;
                _current = new byte[_grant]; // note: the hint is ignored entirely
                return _current;
            }
        }

        [ProtoContract]
        public class Inner
        {
            [ProtoMember(1)] public int Id { get; set; }
            [ProtoMember(2)] public string Label { get; set; }
        }

        [ProtoContract]
        public class Payload
        {
            [ProtoMember(1)] public int Small { get; set; }
            [ProtoMember(2)] public long Negative { get; set; }          // the 10-byte varint
            [ProtoMember(3, DataFormat = DataFormat.FixedSize)] public int Fixed32 { get; set; }
            [ProtoMember(4)] public double Fixed64 { get; set; }
            [ProtoMember(5)] public string Text { get; set; }
            [ProtoMember(6)] public byte[] Blob { get; set; }
            [ProtoMember(7)] public Inner Child { get; set; }
            [ProtoMember(8)] public List<Inner> Children { get; set; }
            [ProtoMember(9)] public Dictionary<int, string> Map { get; set; }
        }

        private static Payload CreateValue() => new Payload
        {
            Small = 42,
            Negative = -1234567890123L,
            Fixed32 = 0x11223344,
            Fixed64 = 1234.5678,
            Text = new string('x', 200),
            Blob = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
            Child = new Inner { Id = 7, Label = "child" },
            Children = [new Inner { Id = 1, Label = "a" }, new Inner { Id = 2, Label = "bb" }],
            Map = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } },
        };

        // 1 is the psychotic case; 9 is still narrower than a 10-byte varint; 11..64 are
        // "large but not large enough" - usable, just far below the requested lease
        public static IEnumerable<object[]> Grants()
        {
            foreach (var grant in new[] { 1, 2, 3, 7, 9, 10, 11, 13, 16, 17, 31, 64, 127 })
                yield return new object[] { grant };
        }

        private static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Payload), true);
            model.Add(typeof(Inner), true);
            return model;
        }

        [Theory, MemberData(nameof(Grants))]
        public void SurvivesADestinationThatIgnoresTheHint(int grant)
        {
            var value = CreateValue();
            var model = CreateModel();

            byte[] expected;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                expected = ms.ToArray();
            }

            var bw = new StingyBufferWriter(grant);
            var reported = model.Serialize<Payload>(bw, value);

            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(bw.ToArray()));
            Assert.Equal(expected.Length, reported);
        }

        [Fact]
        public void OneByteAtATimeStillProducesTheRightBytes()
        {
            var value = CreateValue();
            var model = CreateModel();

            byte[] expected;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, value);
                expected = ms.ToArray();
            }

            var bw = new StingyBufferWriter(1);
            model.Serialize<Payload>(bw, value);
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(bw.ToArray()));

            // and it really was one byte per lease - i.e. the destination was as hostile as
            // claimed, and the writer did not quietly get a usable buffer from somewhere
            Assert.True(bw.Leases >= expected.Length,
                $"expected at least {expected.Length} leases, saw {bw.Leases}");
        }
    }
}

#endif
