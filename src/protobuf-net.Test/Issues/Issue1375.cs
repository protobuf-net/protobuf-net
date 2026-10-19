using System;
using System.IO;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    /// <summary>
    /// Regression test for StreamProtoReader pool state corruption.
    /// https://github.com/protobuf-net/protobuf-net/issues/1375
    /// 
    /// When a StreamProtoReader is pooled after an interrupted fixed-length deserialization,
    /// its _isFixedLength and _dataRemaining64 fields remain dirty. On reuse for a MemoryStream
    /// deserialization, IsFullyConsumed incorrectly checks the stale fixed-length fields instead
    /// of _available, causing spurious "Incorrect number of bytes consumed" errors on valid data.
    /// </summary>
    public class Issue1375
    {
        [Fact]
        public void StreamReaderPoolNotCorruptedByInterruptedFixedLengthRead()
        {
            var model = RuntimeTypeModel.Create();
            var payload = new TestPayload { Ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() } };

            // Serialize payload
            byte[] serialized;
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, payload);
                serialized = ms.ToArray();
            }

            // Deserialize successfully (baseline)
            using (var ms = new MemoryStream(serialized))
            {
                var result = model.Deserialize<TestPayload>(ms);
                Assert.NotNull(result.Ids);
            }

            // Perform an interrupted fixed-length deserialization (e.g., length-prefix with limit)
            using (var prefixed = new MemoryStream())
            {
                model.SerializeWithLengthPrefix(prefixed, payload, typeof(TestPayload), PrefixStyle.Base128, 1);

                var ex = Assert.Throws<TimeoutException>(() =>
                {
                    model.DeserializeWithLengthPrefix(
                        new LimitedBytesStream(new MemoryStream(prefixed.ToArray()), maxBytes: 50),
                        null,
                        typeof(TestPayload),
                        PrefixStyle.Base128,
                        1);
                });
                Assert.Equal("Byte limit exceeded", ex.Message);
            }

            // Deserialize again from a fresh MemoryStream on the same thread.
            // This reuses the pooled StreamProtoReader that was left in the interrupted state.
            // If not properly reset, _isFixedLength and _dataRemaining64 remain dirty,
            // causing spurious "Incorrect number of bytes consumed" errors.
            using (var ms = new MemoryStream(serialized))
            {
                var result = model.Deserialize<TestPayload>(ms);
                Assert.NotNull(result.Ids);
                Assert.Equal(payload.Ids, result.Ids);
            }
        }

        [ProtoContract]
        private class TestPayload
        {
            [ProtoMember(1)]
            public Guid[] Ids { get; set; }
        }

        /// <summary>
        /// Simulates a stream that throws after delivering a limited number of bytes.
        /// Used to interrupt deserialization mid-read.
        /// </summary>
        private class LimitedBytesStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxBytes;
            private long _delivered;

            public LimitedBytesStream(Stream inner, long maxBytes)
            {
                _inner = inner;
                _maxBytes = maxBytes;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;
            public override long Position { get => _delivered; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_delivered >= _maxBytes)
                    throw new TimeoutException("Byte limit exceeded");

                int toRead = Math.Min(count, (int)(_maxBytes - _delivered));
                int read = _inner.Read(buffer, offset, toRead);
                _delivered += read;
                return read;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
