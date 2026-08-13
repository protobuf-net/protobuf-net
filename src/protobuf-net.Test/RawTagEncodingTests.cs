#if PLAT_SPANS

using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace ProtoBuf.Tests
{
    /// <summary>
    /// The pre-encoded constant tag ladder (docs/nano-writer.md): every width of
    /// <c>WriteRawTag</c>, and the bool composition, checked against the shipped varint encoder.
    /// </summary>
    /// <remarks>
    /// The arms fold to literals at a generated call site, which is the whole point - and also
    /// means a wrong constant is invisible until it reaches the wire. So these are DERIVED
    /// rather than typed: the expectation comes from the same varint writer the slow arm uses,
    /// not from a hand-written table. The varint matrix caught two of its own strategies this
    /// way (bad multiply-shift constants, and a lookup table wrong at its second entry).
    /// </remarks>
    public class RawTagEncodingTests
    {
        /// <summary>
        /// Hands out exactly <c>lease</c> bytes at a time, so the tag can be forced to straddle
        /// a chunk boundary at every offset and take the out-of-line arm instead.
        /// </summary>
        private sealed class ExactBufferWriter : IBufferWriter<byte>
        {
            private readonly List<byte> _committed = new();
            private readonly int _lease;
            private byte[] _current = Array.Empty<byte>();

            public ExactBufferWriter(int lease) => _lease = lease;

            public byte[] ToArray() => _committed.ToArray();

            public void Advance(int count)
            {
                for (int i = 0; i < count; i++) _committed.Add(_current[i]);
                _current = Array.Empty<byte>();
            }

            public Memory<byte> GetMemory(int sizeHint = 0) => _current = new byte[Math.Max(sizeHint, _lease)];
            public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;
        }

        /// <summary>The reference encoding: the shipped varint writer, which the slow arm uses.</summary>
        private static byte[] ExpectedVarint(uint value)
        {
            var buffer = new byte[10];
            int count = ProtoWriter.State.WriteVarint64(value, buffer);
            return new ReadOnlySpan<byte>(buffer, 0, count).ToArray();
        }

        /// <summary>
        /// Every tag width, and both sides of every boundary. Field numbers run to 2^29-1 and the
        /// tag is (field &lt;&lt; 3) | wire, so the tag spans the whole uint range.
        /// </summary>
        public static IEnumerable<object[]> Tags()
        {
            var seen = new HashSet<uint>();
            var list = new List<object[]>();

            void Add(long candidate)
            {
                // a tag of 0 is not legal protobuf (field 0), so it is not a case
                if (candidate > 0 && candidate <= uint.MaxValue && seen.Add((uint)candidate))
                {
                    list.Add(new object[] { (uint)candidate });
                }
            }

            // every varint-width boundary, either side
            foreach (var shift in new[] { 7, 14, 21, 28 })
            {
                Add((1L << shift) - 1);
                Add(1L << shift);
                Add((1L << shift) + 1);
            }
            // real field numbers across the widths, at every wire type
            foreach (var field in new[] { 1, 2, 15, 16, 17, 999, 2047, 2048, 262143, 262144, 33554431, 33554432, 536870911 })
            {
                for (uint wire = 0; wire <= 5; wire++) Add(((long)field << 3) | wire);
            }
            Add(uint.MaxValue);
            Add(1);
            return list;
        }

        [Theory]
        [MemberData(nameof(Tags))]
        public void TagMatchesTheVarintEncoder(uint tag)
        {
            var expected = ExpectedVarint(tag);

            // a lease wide enough for the fast arm, and then every lease that forces the
            // boundary through the middle of this tag
            for (int lease = 1; lease <= expected.Length + 1; lease++)
            {
                var bw = new ExactBufferWriter(lease);
                var state = ProtoWriter.State.Create(bw, RuntimeTypeModel.Default);
                try
                {
                    state.WriteRawTag(tag);
                    state.Flush();
                }
                finally
                {
                    state.Dispose();
                }
                Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(bw.ToArray()));
            }
        }

        [Theory]
        [MemberData(nameof(Tags))]
        public void TagBoolMatchesTagThenVarint(uint tag)
        {
            foreach (var value in new[] { false, true })
            {
                var tagBytes = ExpectedVarint(tag);
                var expected = new byte[tagBytes.Length + 1];
                tagBytes.CopyTo(expected, 0);
                expected[tagBytes.Length] = value ? (byte)1 : (byte)0;

                for (int lease = 1; lease <= expected.Length + 1; lease++)
                {
                    var bw = new ExactBufferWriter(lease);
                    var state = ProtoWriter.State.Create(bw, RuntimeTypeModel.Default);
                    try
                    {
                        state.WriteRawTagBool(tag, value);
                        state.Flush();
                    }
                    finally
                    {
                        state.Dispose();
                    }
                    Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(bw.ToArray()));
                }
            }
        }

        /// <summary>
        /// The same two, against the STREAM backend - which is where the span-direct arm only
        /// started firing with the buffer core, and where every headline benchmark row runs.
        /// </summary>
        [Theory]
        [MemberData(nameof(Tags))]
        public void TagMatchesOnTheStreamBackend(uint tag)
        {
            var expected = ExpectedVarint(tag);
            using var ms = new System.IO.MemoryStream();
            var state = ProtoWriter.State.Create(ms, RuntimeTypeModel.Default);
            try
            {
                state.WriteRawTag(tag);
                state.WriteRawTagBool(tag, true);
                state.Close();
            }
            finally
            {
                state.Dispose();
            }

            var both = new byte[(expected.Length * 2) + 1];
            expected.CopyTo(both, 0);
            expected.CopyTo(both, expected.Length);
            both[both.Length - 1] = 1;
            Assert.Equal(BitConverter.ToString(both), BitConverter.ToString(ms.ToArray()));
        }
    }
}

#endif
