using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    /// <summary>
    /// Asserts that a length-prefix is not used to size an allocation before the data behind it has
    /// been read. A prefix that overstates the data available should fail cheaply, with allocation
    /// proportional to the bytes actually supplied rather than to the declared length.
    /// </summary>
    public class LengthPrefixAllocationTests
    {
        // large enough that an allocation against it is unmistakable in the measurement, small
        // enough to stay comfortably allocatable so the assertion reports rather than OOMs
        const int ClaimedLength = 512 * 1024 * 1024;

        // headroom for the read itself; the bounded paths rent at most a handful of 32KiB chunks
        const long AllowedBytes = 4 * 1024 * 1024;

        [ProtoContract]
        public class HazBlob
        {
            [ProtoMember(1)]
            public byte[] Value { get; set; }
        }

        [ProtoContract]
        public class HazString
        {
            [ProtoMember(1)]
            public string Value { get; set; }
        }

        [ProtoContract]
        public class HazPackedFixed
        {
            [ProtoMember(1, IsPacked = true, DataFormat = DataFormat.FixedSize)]
            public long[] Value { get; set; }
        }

        [ProtoContract]
        public class HazPackedVarint
        {
            [ProtoMember(1, IsPacked = true)]
            public int[] Value { get; set; }
        }

        [ProtoContract]
        public class HazSubMessage
        {
            [ProtoMember(1)]
            public HazString Value { get; set; }
        }

        /// <summary>
        /// field 1, wire-type 2 (length-prefixed), declaring <paramref name="length"/> bytes -
        /// and then supplying none of them.
        /// </summary>
        static byte[] OverstatedLengthPrefix(int length = ClaimedLength)
        {
            var buffer = new List<byte> { 0x0A }; // field 1, WireType.String
            var value = (uint)length;
            while (value > 127)
            {
                buffer.Add((byte)(value | 0x80));
                value >>= 7;
            }
            buffer.Add((byte)value);
            return buffer.ToArray();
        }

        [Fact]
        public void OverstatedLengthPrefixEncoding() // sanity-check the payload construction itself
        {
            Assert.Equal(new byte[] { 0x0A, 0x80, 0x80, 0x80, 0x80, 0x02 }, OverstatedLengthPrefix());
        }

        // ---- the four ways a payload reaches a reader ----------------------------------------

        static void ViaMemoryStream<T>(byte[] payload)
        {
            // StreamProtoReader, but the buffer is handed over wholesale: length is known exactly
            using var ms = new MemoryStream(payload);
            Serializer.Deserialize<T>(ms);
        }

        static void ViaUnboundedStream<T>(byte[] payload)
        {
            // StreamProtoReader with a stream it can neither seek nor size: length is unknowable
            using var stream = new UnseekableStream(payload);
            Serializer.Deserialize<T>(stream);
        }

        static void ViaSingleSegment<T>(byte[] payload)
        {
            Serializer.Deserialize<T>(new ReadOnlySequence<byte>(payload));
        }

        static void ViaMultiSegment<T>(byte[] payload)
        {
            // ReadOnlySequenceProtoReader across segment boundaries - the pipelines/Kestrel shape
            Serializer.Deserialize<T>(Segment.Create(payload, chunkSize: 2));
        }

        public static IEnumerable<object[]> AllReaders => new[]
        {
            new object[] { nameof(ViaMemoryStream) },
            new object[] { nameof(ViaUnboundedStream) },
            new object[] { nameof(ViaSingleSegment) },
            new object[] { nameof(ViaMultiSegment) },
        };

        static void Invoke<T>(string reader, byte[] payload)
        {
            switch (reader)
            {
                case nameof(ViaMemoryStream): ViaMemoryStream<T>(payload); break;
                case nameof(ViaUnboundedStream): ViaUnboundedStream<T>(payload); break;
                case nameof(ViaSingleSegment): ViaSingleSegment<T>(payload); break;
                case nameof(ViaMultiSegment): ViaMultiSegment<T>(payload); break;
                default: throw new ArgumentOutOfRangeException(nameof(reader));
            }
        }

        // ---- the tests ------------------------------------------------------------------------

        [Theory, MemberData(nameof(AllReaders))]
        public void BlobDoesNotAllocateAgainstClaimedLength(string reader)
            => AssertBoundedAllocation<HazBlob>(reader, OverstatedLengthPrefix());

        [Theory, MemberData(nameof(AllReaders))]
        public void StringDoesNotAllocateAgainstClaimedLength(string reader)
            => AssertBoundedAllocation<HazString>(reader, OverstatedLengthPrefix());

        [Theory, MemberData(nameof(AllReaders))]
        public void PackedFixedDoesNotAllocateAgainstClaimedLength(string reader)
            => AssertBoundedAllocation<HazPackedFixed>(reader, OverstatedLengthPrefix());

        [Theory, MemberData(nameof(AllReaders))]
        public void PackedVarintDoesNotAllocateAgainstClaimedLength(string reader)
            => AssertBoundedAllocation<HazPackedVarint>(reader, OverstatedLengthPrefix());

        [Theory, MemberData(nameof(AllReaders))]
        public void SubMessageDoesNotAllocateAgainstClaimedLength(string reader)
            => AssertBoundedAllocation<HazSubMessage>(reader, OverstatedLengthPrefix());

        /// <summary>
        /// A blob whose length-prefix is honest must still round-trip, including at sizes past the
        /// point where the reader stops trusting the prefix outright.
        /// </summary>
        [Theory, MemberData(nameof(AllReaders))]
        public void HonestLargeBlobStillRoundTrips(string reader)
        {
            var expected = new byte[256 * 1024]; // comfortably over the 32KiB eager-allocation limit
            new Random(12345).NextBytes(expected);

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, new HazBlob { Value = expected });
            var payload = ms.ToArray();

            HazBlob actual = null;
            switch (reader)
            {
                case nameof(ViaMemoryStream):
                    using (var source = new MemoryStream(payload)) actual = Serializer.Deserialize<HazBlob>(source);
                    break;
                case nameof(ViaUnboundedStream):
                    using (var source = new UnseekableStream(payload)) actual = Serializer.Deserialize<HazBlob>(source);
                    break;
                case nameof(ViaSingleSegment):
                    actual = Serializer.Deserialize<HazBlob>(new ReadOnlySequence<byte>(payload));
                    break;
                case nameof(ViaMultiSegment):
                    actual = Serializer.Deserialize<HazBlob>(Segment.Create(payload, chunkSize: 1024));
                    break;
            }

            Assert.Equal(expected, actual.Value);
        }

        /// <summary>
        /// As above, for strings - this is the path that previously had to fit the whole value in a
        /// single contiguous buffer sized from the prefix.
        /// </summary>
        [Theory, MemberData(nameof(AllReaders))]
        public void HonestLargeStringStillRoundTrips(string reader)
        {
            // include multi-byte characters so a chunked decode can't get away with byte-slicing
            var expected = string.Join("|", new string('a', 100_000), new string('ä', 100_000), new string('中', 100_000));

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, new HazString { Value = expected });
            var payload = ms.ToArray();

            HazString actual = null;
            switch (reader)
            {
                case nameof(ViaMemoryStream):
                    using (var source = new MemoryStream(payload)) actual = Serializer.Deserialize<HazString>(source);
                    break;
                case nameof(ViaUnboundedStream):
                    using (var source = new UnseekableStream(payload)) actual = Serializer.Deserialize<HazString>(source);
                    break;
                case nameof(ViaSingleSegment):
                    actual = Serializer.Deserialize<HazString>(new ReadOnlySequence<byte>(payload));
                    break;
                case nameof(ViaMultiSegment):
                    actual = Serializer.Deserialize<HazString>(Segment.Create(payload, chunkSize: 1024));
                    break;
            }

            Assert.Equal(expected, actual.Value);
        }

        // ---- assertion machinery --------------------------------------------------------------

        /// <summary>
        /// Asserts that deserializing the payload fails, and that it fails without allocating
        /// anything like the length it claimed.
        /// </summary>
        static void AssertBoundedAllocation<T>(string reader, byte[] payload)
        {
            // warm up every code path involved (JIT, model build, serializer construction) against
            // a well-formed payload, so that the measurement below sees only the read itself
            try { Invoke<T>(reader, new byte[] { 0x0A, 0x00 }); } catch { }

            long before = GetAllocatedBytes();
            var ex = Record.Exception(() => Invoke<T>(reader, payload));
            long allocated = GetAllocatedBytes() - before;

            Assert.NotNull(ex); // the prefix cannot be satisfied; it must not succeed
            Assert.IsNotType<OutOfMemoryException>(ex);
            Assert.True(allocated < AllowedBytes,
                $"{reader}/{typeof(T).Name}: allocated {allocated:n0} bytes for a payload of {payload.Length} bytes claiming {ClaimedLength:n0}; threw {ex.GetType().Name}: {ex.Message}");
        }

        static long GetAllocatedBytes()
        {
#if NET8_0_OR_GREATER
            return GC.GetAllocatedBytesForCurrentThread();
#else
            return GC.GetTotalMemory(forceFullCollection: false);
#endif
        }

        // ---- helpers --------------------------------------------------------------------------

        /// <summary>A stream that reveals nothing about its length, forcing the streaming reader.</summary>
        sealed class UnseekableStream : Stream
        {
            private readonly byte[] _data;
            private int _position;
            public UnseekableStream(byte[] data) => _data = data;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override void Flush() { }
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                int take = Math.Min(count, _data.Length - _position);
                if (take <= 0) return 0;
                Buffer.BlockCopy(_data, _position, buffer, offset, take);
                _position += take;
                return take;
            }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        /// <summary>Splits a payload into a genuinely multi-segment sequence.</summary>
        sealed class Segment : ReadOnlySequenceSegment<byte>
        {
            public static ReadOnlySequence<byte> Create(byte[] data, int chunkSize)
            {
                Segment first = null, last = null;
                for (int offset = 0; offset < data.Length; offset += chunkSize)
                {
                    var chunk = new ReadOnlyMemory<byte>(data, offset, Math.Min(chunkSize, data.Length - offset));
                    var next = new Segment { Memory = chunk, RunningIndex = offset };
                    if (first is null) first = next;
                    else last.Next = next;
                    last = next;
                }
                if (first is null) return ReadOnlySequence<byte>.Empty;
                return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
            }
        }
    }
}
