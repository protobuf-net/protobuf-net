﻿using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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
        [Obsolete(PreferStateAPI, false)]
        public static ProtoReader Create(Stream source, TypeModel model, SerializationContext context = null, long length = TO_EOF)
            => Create(source, model, (object)context, length);

        internal static ProtoReader Create(Stream source, TypeModel model, object userState, long length)
        {
            var reader = Pool<StreamProtoReader>.TryGet() ?? new StreamProtoReader();
            reader.Init(source, model ?? TypeModel.DefaultModel, userState, length);
            return reader;
        }

        partial struct State
        {
            /// <summary>
            /// Creates a new reader against a stream
            /// </summary>
            /// <param name="source">The source stream</param>
            /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
            /// <param name="userState">Additional context about this serialization operation</param>
            /// <param name="length">The number of bytes to read, or -1 to read until the end of the stream</param>
            public static State Create(Stream source, TypeModel model, object userState = null, long length = TO_EOF)
            {
#if PREFER_SPANS
                if (TryConsumeSegmentRespectingPosition(source, out var segment, length))
                {
                    return Create(new System.Buffers.ReadOnlySequence<byte>(
                        segment.Array, segment.Offset, segment.Count), model, userState);
                }
#endif


                var reader = ProtoReader.Create(source, model, userState, length);
                return new State(reader);
            }
        }

        private static readonly FieldInfo s_origin = typeof(MemoryStream).GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance),
            s_buffer = typeof(MemoryStream).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static bool ReflectionTryGetBuffer(MemoryStream ms, out ArraySegment<byte> buffer)
        {
            if (s_origin is object && s_buffer is object)
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

        internal static bool TryConsumeSegmentRespectingPosition(Stream source, out ArraySegment<byte> data, long length)
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
            protected internal override State DefaultState() => new State(this);

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

            internal StreamProtoReader() { }

            /// <summary>
            /// Creates a new reader against a stream
            /// </summary>
            /// <param name="source">The source stream</param>
            /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
            /// <param name="context">Additional context about this serialization operation</param>
            [Obsolete("Please use ProtoReader.Create; this API may be removed in a future version", error: false)]
            public StreamProtoReader(Stream source, TypeModel model, SerializationContext context)
                => Init(source, model, context, TO_EOF);

            internal void Init(Stream source, TypeModel model, object userState, long length)
            {
                base.Init(model, userState);
                if (source is null) ThrowHelper.ThrowArgumentNullException(nameof(source));
                if (!source.CanRead) ThrowHelper.ThrowArgumentException("Cannot read from stream", nameof(source));

                if (TryConsumeSegmentRespectingPosition(source, out var segment, length))
                {
                    _ioBuffer = segment.Array;
#pragma warning disable IDE0059 // Unnecessary assignment of a value - kept here for clarity
                    length = _available = segment.Count;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
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
                if (_source is object)
                {
                    _source = null;
                    // make sure we don't pool this if it came from a MemoryStream
                    BufferPool.ReleaseBufferToPool(ref _ioBuffer);
                }
                else
                {
                    _ioBuffer = null;
                }
                Pool<StreamProtoReader>.Put(this);
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
                if (_available == 1) state.ThrowEoF();

                uint chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (_available == 2) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (_available == 3) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (_available == 4) state.ThrowEoF();

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
                state.ThrowOverflow();
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
            private protected override void ImplReadBytes(ref State state, Span<byte> target)
            {
                var len = target.Length;
                // value is now sized with the final length, and (if necessary)
                // contains the old data up to "offset"
                Advance(len); // assume success
                while (len > _available)
                {
                    if (_available > 0)
                    {
                        // copy what we *do* have
                        new Span<byte>(_ioBuffer, _ioIndex, _available).CopyTo(target);
                        len -= _available;
                        target = target.Slice(_available);
                        _ioIndex = _available = 0; // we've drained the buffer
                    }
                    //  now refill the buffer (without overflowing it)
                    int count = len > _ioBuffer.Length ? _ioBuffer.Length : len;
                    if (count > 0) Ensure(ref state, count, true);
                }
                // at this point, we know that len <= available
                if (len > 0)
                {   // still need data, but we have enough buffered
                    new Span<byte>(_ioBuffer, _ioIndex, len).CopyTo(target);
                    _available -= len;
                    _ioIndex += len;
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
                if (_available == 1) state.ThrowEoF();

                ulong chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (_available == 2) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (_available == 3) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (_available == 4) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;
                if (_available == 5) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;
                if (_available == 6) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;
                if (_available == 7) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;
                if (_available == 8) state.ThrowEoF();

                chunk = _ioBuffer[readPos++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;
                if (_available == 9) state.ThrowEoF();

                chunk = _ioBuffer[readPos];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) state.ThrowOverflow();
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
                Debug.Assert(_available <= count, "Asking for data without checking first");
                if (_source is object)
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
                    state.ThrowEoF();
                }
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
                    if (count > _dataRemaining64) state.ThrowEoF();
                    // else assume we're going to be OK
                    _dataRemaining64 -= count;
                }

                // not all available locally; need to jump data in the stream
                if (_source is null) state.ThrowEoF();
                ProtoReader.Seek(_source, count, _ioBuffer);
            }
        }
    }
}