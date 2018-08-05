using ProtoBuf.Meta;
using System;
using System.IO;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Creates a new reader against a stream
        /// </summary>
        /// <param name="source">The source stream</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        /// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
        public static ProtoReader Create(Stream source, TypeModel model, SerializationContext context = null, long length = TO_EOF)
        {
            var reader = StreamProtoReader.GetRecycled();
            if (reader == null)
            {
#pragma warning disable CS0618
                return new StreamProtoReader(source, model, context, length);
#pragma warning restore CS0618
            }
            reader.Init(source, model, context, length);
            return reader;
        }

        private sealed class StreamProtoReader : ProtoReader
        {
            private Stream _source;
            private byte[] _ioBuffer;
            private bool _isFixedLength;
            private int _ioIndex, _available;
            private long _dataRemaining64;

            /// <summary>
            /// Creates a new reader against a stream
            /// </summary>
            /// <param name="source">The source stream</param>
            /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
            /// <param name="context">Additional context about this serialization operation</param>
            /// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
            [Obsolete("Please use ProtoReader.Create; this API may be removed in a future version", error: false)]
            public StreamProtoReader(Stream source, TypeModel model, SerializationContext context, int length)
                => Init(source, model, context, length);

            /// <summary>
            /// Creates a new reader against a stream
            /// </summary>
            /// <param name="source">The source stream</param>
            /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
            /// <param name="context">Additional context about this serialization operation</param>
            /// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
            [Obsolete("Please use ProtoReader.Create; this API may be removed in a future version", error: false)]
            public StreamProtoReader(Stream source, TypeModel model, SerializationContext context, long length)
                => Init(source, model, context, length);

            /// <summary>
            /// Creates a new reader against a stream
            /// </summary>
            /// <param name="source">The source stream</param>
            /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
            /// <param name="context">Additional context about this serialization operation</param>
            [Obsolete("Please use ProtoReader.Create; this API may be removed in a future version", error: false)]
            public StreamProtoReader(Stream source, TypeModel model, SerializationContext context)
                => Init(source, model, context, TO_EOF);

            internal void Init(Stream source, TypeModel model, SerializationContext context, long length)
            {
                Init(model, context);
                if (source == null) throw new ArgumentNullException(nameof(source));
                if (!source.CanRead) throw new ArgumentException("Cannot read from stream", nameof(source));

#if PLAT_MEMORY_STREAM_BUFFER
                if (source is MemoryStream ms && ms.TryGetBuffer(out var segment))
                {
                    _ioBuffer = segment.Array;
                    int pos = checked((int)ms.Position);
                    _available = segment.Count - pos;
                    _ioIndex = segment.Offset + pos;

                    if (length >= 0)
                    {   // make sure we apply it
                        if (length < _available) _available = (int)length;
                    }
                    else
                    {
                        length = _available;
                    }
                }
                else
#endif
                {
                    _source = source;
                    _ioBuffer = BufferPool.GetBuffer();
                    _available = _ioIndex = 0;
                }

                bool isFixedLength = length >= 0;
                _isFixedLength = isFixedLength;
                _dataRemaining64 = isFixedLength ? length : 0;
            }

            public override void Dispose()
            {
                // importantly, this does **not** own the stream, and does not dispose 
                base.Dispose();
                if (_source != null)
                {
                    _source = null;
                    // make sure we don't pool this if it came from a MemoryStream
                    BufferPool.ReleaseBufferToPool(ref _ioBuffer);
                }
            }

            private protected override int TryReadUInt32VarintWithoutMoving(Read32VarintMode mode, out uint value)
            {
                if (_available < 10) Ensure(10, false);
                if (_available == 0)
                {
                    value = 0;
                    return 0;
                }
                int readPos = _ioIndex;
                value = _ioBuffer[readPos++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (_available == 1) throw EoF(this);

                uint chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (_available == 2) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (_available == 3) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (_available == 4) throw EoF(this);

                chunk = _ioBuffer[readPos];
                value |= chunk << 28; // can only use 4 bits from this chunk
                if ((chunk & 0xF0) == 0) return 5;

                if (mode == Read32VarintMode.Signed // allow for -ve values
                    && (chunk & 0xF0) == 0xF0
                    && _available >= 10
                        && _ioBuffer[++readPos] == 0xFF
                        && _ioBuffer[++readPos] == 0xFF
                        && _ioBuffer[++readPos] == 0xFF
                        && _ioBuffer[++readPos] == 0xFF
                        && _ioBuffer[++readPos] == 0x01)
                {
                    return 10;
                }
                throw AddErrorData(new OverflowException(), this);
            }

            private protected override ulong ImplReadUInt64Fixed()
            {
                if (_available < 8) Ensure(8, true);
                Advance(8);
                _available -= 8;

#if PLAT_SPANS
                var result = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(_ioBuffer.AsSpan(_ioIndex, 8));
                _ioIndex += 8;
                return result;
#else
                var buffer = _ioBuffer;
                return ((ulong)buffer[_ioIndex++])
                    | (((ulong)buffer[_ioIndex++]) << 8)
                    | (((ulong)buffer[_ioIndex++]) << 16)
                    | (((ulong)buffer[_ioIndex++]) << 24)
                    | (((ulong)buffer[_ioIndex++]) << 32)
                    | (((ulong)buffer[_ioIndex++]) << 40)
                    | (((ulong)buffer[_ioIndex++]) << 48)
                    | (((ulong)buffer[_ioIndex++]) << 56);
#endif
            }
            private protected override void ImplReadBytes(ArraySegment<byte> target)
            {
                var value = target.Array;
                var offset = target.Offset;
                var len = target.Count;
                // value is now sized with the final length, and (if necessary)
                // contains the old data up to "offset"
                Advance(len); // assume success
                while (len > _available)
                {
                    if (_available > 0)
                    {
                        // copy what we *do* have
                        Buffer.BlockCopy(_ioBuffer, _ioIndex, value, offset, _available);
                        len -= _available;
                        offset += _available;
                        _ioIndex = _available = 0; // we've drained the buffer
                    }
                    //  now refill the buffer (without overflowing it)
                    int count = len > _ioBuffer.Length ? _ioBuffer.Length : len;
                    if (count > 0) Ensure(count, true);
                }
                // at this point, we know that len <= available
                if (len > 0)
                {   // still need data, but we have enough buffered
                    Buffer.BlockCopy(_ioBuffer, _ioIndex, value, offset, len);
                    _ioIndex += len;
                    _available -= len;
                }
            }

            private protected override int TryReadUInt64VarintWithoutMoving(out ulong value)
            {
                if (_available < 10) Ensure(10, false);
                if (_available == 0)
                {
                    value = 0;
                    return 0;
                }
                int readPos = _ioIndex;
                value = _ioBuffer[readPos++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (_available == 1) throw EoF(this);

                ulong chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (_available == 2) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (_available == 3) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (_available == 4) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;
                if (_available == 5) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;
                if (_available == 6) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;
                if (_available == 7) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;
                if (_available == 8) throw EoF(this);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;
                if (_available == 9) throw EoF(this);

                chunk = _ioBuffer[readPos];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) throw AddErrorData(new OverflowException(), this);
                return 10;
            }

            private protected override string ImplReadString(int bytes)
            {
                if (_available < bytes) Ensure(bytes, true);

                string s = UTF8.GetString(_ioBuffer, _ioIndex, bytes);

                _available -= bytes;
                Advance(bytes);
                _ioIndex += bytes;
                return s;
            }

            private protected override bool IsFullyConsumed =>
                (_isFixedLength ? _dataRemaining64 : _available) == 0;

            private protected override uint ImplReadUInt32Fixed()
            {
                if (_available < 4) Ensure(4, true);
                Advance(4);
                _available -= 4;
#if PLAT_SPANS
                var result = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(_ioBuffer.AsSpan(_ioIndex, 4));
                _ioIndex += 4;
                return result;
#else
                var buffer = _ioBuffer;
                return ((uint)buffer[_ioIndex++])
                    | (((uint)buffer[_ioIndex++]) << 8)
                    | (((uint)buffer[_ioIndex++]) << 16)
                    | (((uint)buffer[_ioIndex++]) << 24);
#endif
            }

            private void Ensure(int count, bool strict)
            {
                Helpers.DebugAssert(_available <= count, "Asking for data without checking first");
                if (count > _ioBuffer.Length)
                {
                    BufferPool.ResizeAndFlushLeft(ref _ioBuffer, count, _ioIndex, _available);
                    _ioIndex = 0;
                }
                else if (_ioIndex + count >= _ioBuffer.Length)
                {
                    // need to shift the buffer data to the left to make space
                    Buffer.BlockCopy(_ioBuffer, _ioIndex, _ioBuffer, 0, _available);
                    _ioIndex = 0;
                }
                count -= _available;
                int writePos = _ioIndex + _available, bytesRead;
                int canRead = _ioBuffer.Length - writePos;
                if (_isFixedLength)
                {   // throttle it if needed
                    if (_dataRemaining64 < canRead) canRead = (int)_dataRemaining64;
                }
                while (count > 0 && canRead > 0 && (bytesRead = _source.Read(_ioBuffer, writePos, canRead)) > 0)
                {
                    _available += bytesRead;
                    count -= bytesRead;
                    canRead -= bytesRead;
                    writePos += bytesRead;
                    if (_isFixedLength) { _dataRemaining64 -= bytesRead; }
                }
                if (strict && count > 0)
                {
                    throw EoF(this);
                }
            }

#if !PLAT_NO_THREADSTATIC
            [ThreadStatic]
            private static StreamProtoReader lastReader;

            internal static StreamProtoReader GetRecycled()
            {
                var tmp = lastReader;
                lastReader = null;
                return tmp;
            }

            internal override void Recycle()
            {
                Dispose();
                lastReader = this;
            }

            private protected override void ImplSkipBytes(long count, bool preservePreviewField)
            {
                if (_available < count && count < 128)
                {
                    Ensure((int)count, true);
                }

                if (count <= _available)
                { // just jump it!
                    _available -= (int)count;
                    _ioIndex += (int)count;
                    Advance(count);
                    return;
                }
                // everything remaining in the buffer is garbage
                Advance(count); // assumes success, but if it fails we're screwed anyway
                count -= _available; // discount anything we've got to-hand
                _ioIndex = _available = 0; // note that we have no data in the buffer

                if (_isFixedLength)
                {
                    if (count > _dataRemaining64) throw EoF(this);
                    // else assume we're going to be OK
                    _dataRemaining64 -= count;
                }

                ProtoReader.Seek(_source, count, _ioBuffer);
            }
#elif !PLAT_NO_INTERLOCKED
        private static object lastReader;
        internal static StreamProtoReader GetRecycled()
        {
            return (StreamProtoReader)System.Threading.Interlocked.Exchange(ref lastReader, null);
        }
        internal override void Recycle(StreamProtoReader reader)
        {
            Dispose();
            System.Threading.Interlocked.Exchange(ref lastReader, this);
        }
#else
        private static readonly object recycleLock = new object();
        internal static StreamProtoReader lastReader;
        private static StreamProtoReader GetRecycled()
        {
            lock(recycleLock)
            {
                ProtoReader tmp = lastReader;
                lastReader = null;
                return tmp;
            }            
        }
        internal static void Recycle(StreamProtoReader reader)
        {
            if(reader != null)
            {
                reader.Dispose();
                lock(recycleLock)
                {
                    lastReader = reader;
                }
            }
        }
#endif
        }
    }
}