using ProtoBuf.Meta;
using System;
using System.IO;
using System.Reflection;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        internal const bool PreferSpans
#if PLAT_SPAN_OVERLOADS
            = true;
#else
            = false;
#endif

        /// <summary>
        /// Creates a new reader against a stream
        /// </summary>
        /// <param name="source">The source stream</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        /// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
        [Obsolete(UseStateAPI, false)]
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

        /// <summary>
        /// Creates a new reader against a stream
        /// </summary>
        /// <param name="source">The source stream</param>
        /// <param name="state">Reader state</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        /// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
        public static ProtoReader Create(out State state, Stream source, TypeModel model, SerializationContext context = null, long length = TO_EOF)
        {
#if PLAT_SPAN_OVERLOADS
            if (PreferSpans && TryConsumeSegmentRespectingPosition(source, out var segment, length))
            {
                return Create(out state, new System.Buffers.ReadOnlySequence<byte>(
                    segment.Array, segment.Offset, segment.Count), model, context);
            }
#endif

            state = default; // not used by this API
#pragma warning disable CS0618 // Type or member is obsolete
            return Create(source, model, context, length);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        internal static ProtoReader CreateSolid(out SolidState state, Stream source, TypeModel model, SerializationContext context = null, long length = TO_EOF)
        {
            var reader = Create(out var liquid, source, model, context, length);
            state = liquid.Solidify();
            return reader;
        }

        private static readonly FieldInfo s_origin = typeof(MemoryStream).GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance),
            s_buffer = typeof(MemoryStream).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static bool ReflectionTryGetBuffer(MemoryStream ms, out ArraySegment<byte> buffer)
        {
            if (s_origin != null && s_buffer != null)
            {
                try
                {
                    int offset = (int)s_origin.GetValue(ms);
                    byte[] arr = (byte[])s_buffer.GetValue(ms);
                    buffer = new ArraySegment<byte>(arr, offset, checked((int)ms.Length));
                    return true;
                }
                catch { }
            }
            buffer = default;
            return false;
        }

#pragma warning disable RCS1163
        internal static bool TryConsumeSegmentRespectingPosition(Stream source, out ArraySegment<byte> data, long length)
#pragma warning restore RCS1163
        {
            if (source is MemoryStream ms && ms.CanSeek
                && (ms.TryGetBuffer(out var segment) || ReflectionTryGetBuffer(ms, out segment)))
            {
                int pos = checked((int)ms.Position);
                var count = segment.Count - pos;
                var offset = segment.Offset + pos;

                if (length >= 0 && length < count)
                {   // make sure we apply a length limit
                    count = (int)length;
                }
                data = new ArraySegment<byte>(segment.Array, offset, count);
                // skip the data in the source
                ms.Seek(count, SeekOrigin.Current);
                return true;
            }
            data = default;
            return false;
        }

        private sealed class StreamProtoReader : ProtoReader
        {
            protected internal override State DefaultState() => default;

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

                if (TryConsumeSegmentRespectingPosition(source, out var segment, length))
                {
                    _ioBuffer = segment.Array;
                    length = _available = segment.Count;
                    _ioIndex = segment.Offset;

                    // don't set _isFixedLength/_dataRemaining64; despite it
                    // sounding right: it isn't
                }
                else
                {
                    _source = source;
                    _ioBuffer = BufferPool.GetBuffer();
                    _available = _ioIndex = 0;

                    bool isFixedLength = length >= 0;
                    _isFixedLength = isFixedLength;
                    _dataRemaining64 = isFixedLength ? length : 0;
                }
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

            private protected override int ImplTryReadUInt32VarintWithoutMoving(ref State state, Read32VarintMode mode, out uint value)
            {
                if (_available < 10) Ensure(ref state, 10, false);
                if (_available == 0)
                {
                    value = 0;
                    return 0;
                }
                int readPos = _ioIndex;
                value = _ioBuffer[readPos++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (_available == 1) ThrowEoF(this, ref state);

                uint chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (_available == 2) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (_available == 3) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (_available == 4) ThrowEoF(this, ref state);

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
                ThrowOverflow(this, ref state);
                return 0;
            }

            private protected override ulong ImplReadUInt64Fixed(ref State state)
            {
                if (_available < 8) Ensure(ref state, 8, true);
                Advance(8);
                _available -= 8;

                var result = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(_ioBuffer.AsSpan(_ioIndex, 8));
                _ioIndex += 8;
                return result;
            }
            private protected override void ImplReadBytes(ref State state, ArraySegment<byte> target)
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
                    if (count > 0) Ensure(ref state, count, true);
                }
                // at this point, we know that len <= available
                if (len > 0)
                {   // still need data, but we have enough buffered
                    Buffer.BlockCopy(_ioBuffer, _ioIndex, value, offset, len);
                    _ioIndex += len;
                    _available -= len;
                }
            }

            private protected override int ImplTryReadUInt64VarintWithoutMoving(ref State state, out ulong value)
            {
                if (_available < 10) Ensure(ref state, 10, false);
                if (_available == 0)
                {
                    value = 0;
                    return 0;
                }
                int readPos = _ioIndex;
                value = _ioBuffer[readPos++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (_available == 1) ThrowEoF(this, ref state);

                ulong chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (_available == 2) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (_available == 3) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (_available == 4) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;
                if (_available == 5) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;
                if (_available == 6) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;
                if (_available == 7) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;
                if (_available == 8) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;
                if (_available == 9) ThrowEoF(this, ref state);

                chunk = _ioBuffer[readPos];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) throw AddErrorData(new OverflowException(), this, ref state);
                return 10;
            }

            private protected override string ImplReadString(ref State state, int bytes)
            {
                if (_available < bytes) Ensure(ref state, bytes, true);

                string s = UTF8.GetString(_ioBuffer, _ioIndex, bytes);

                _available -= bytes;
                Advance(bytes);
                _ioIndex += bytes;
                return s;
            }

            private protected override bool IsFullyConsumed(ref State state) =>
                (_isFixedLength ? _dataRemaining64 : _available) == 0;

            private protected override uint ImplReadUInt32Fixed(ref State state)
            {
                if (_available < 4) Ensure(ref state, 4, true);
                Advance(4);
                _available -= 4;
                var result = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(_ioBuffer.AsSpan(_ioIndex, 4));
                _ioIndex += 4;
                return result;
            }

            private void Ensure(ref State state, int count, bool strict)
            {
                Helpers.DebugAssert(_available <= count, "Asking for data without checking first");
                if (_source != null)
                {
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
                }
                if (strict && count > 0)
                {
                    ThrowEoF(this, ref state);
                }
            }

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

            private protected override void ImplSkipBytes(ref State state, long count)
            {
                if (_available < count && count < 128)
                {
                    Ensure(ref state, (int)count, true);
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
                    if (count > _dataRemaining64) ThrowEoF(this, ref state);
                    // else assume we're going to be OK
                    _dataRemaining64 -= count;
                }

                ProtoReader.Seek(_source, count, _ioBuffer);
            }
        }
    }
}