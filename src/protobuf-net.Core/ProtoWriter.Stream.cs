using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
        /// <summary>
        /// Creates a new writer against a stream
        /// </summary>
        /// <param name="dest">The destination stream</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        [Obsolete(ProtoReader.PreferStateAPI, false)]
        public static ProtoWriter Create(Stream dest, TypeModel model, SerializationContext context = null)
            => StreamProtoWriter.CreateStreamProtoWriter(dest, model, context);

        partial struct State
        {
            /// <summary>
            /// Creates a new writer against a stream
            /// </summary>
            /// <param name="dest">The destination stream</param>
            /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
            /// <param name="userState">Additional context about this serialization operation</param>
            public static State Create(Stream dest, TypeModel model, object userState = null)
            {
                var writer = StreamProtoWriter.CreateStreamProtoWriter(dest, model, userState);
                return new State(writer);
            }
        }

        private class StreamProtoWriter : ProtoWriter
        {
            private Stream dest;
            private int flushLock;

            private protected override bool ImplDemandFlushOnDispose => true;

            private StreamProtoWriter()
            {
                // share the *same* known objects key, exactly as the buffer-writer does, so a
                // length measured here serves the write through the shared cache
                _nullWriter = new NullProtoWriter(netCache);
            }

            private readonly NullProtoWriter _nullWriter;

            // the null-writer sidecar shares this cache and clears it in its own Cleanup
            private protected override void ClearKnownObjects() { }

            /// <summary>
            /// Sub-messages go measure-first ONLY where the serializer can price itself without
            /// writing; everything else keeps this backend's reserve-and-back-fill.
            /// </summary>
            /// <remarks>
            /// The strategy is chosen on its own merits, PER SERIALIZER - not per backend, and
            /// emphatically not on whether callbacks are present. Measured on the descriptor
            /// corpus (notes/nano-writer.md): for a serializer with no arithmetic measure, pricing
            /// by null-writer traversal costs about 2.3x what back-filling does, which is exactly
            /// why the buffer-writer - which cannot back-fill at all - is so much slower for
            /// runtime models. So a model with no <see cref="IMeasuringSerializer{T}"/> takes the
            /// identical path it always did, and a generated measurable contract skips the
            /// reserve, the flushLock and the back-fill shuffle.
            /// </remarks>
            protected internal override void WriteMessage<T>(ref State state, T value, ISerializer<T> serializer,
                PrefixStyle style, bool recursionCheck)
            {
                switch (WireType)
                {
                    case WireType.String:
                    case WireType.Fixed32:
                        var resolved = serializer ?? TypeModel.ResolveSerializer<T>(Model);
                        if (resolved is IMeasuringSerializer<T>
                            && resolved.Features.HasFlag(SerializerFeatures.OptionTrySkipWritingWhenMeasuring))
                        {
                            PreSubItem(ref state, TypeHelper<T>.IsReferenceType & recursionCheck ? (object)value : null);
                            WriteMeasuredWithLengthPrefix<T>(_nullWriter, ref state, value, resolved, style);
                            PostSubItem(ref state);
                            return;
                        }
                        goto default;
                    default:
                        base.WriteMessage<T>(ref state, value, serializer, style, recursionCheck);
                        return;
                }
            }
            internal static StreamProtoWriter CreateStreamProtoWriter(Stream dest, TypeModel model, object userState)
            {
                var obj = Pool<StreamProtoWriter>.TryGet() ?? new StreamProtoWriter();
                obj.Init(model, userState, true);
                if (dest is null) ThrowHelper.ThrowArgumentNullException(nameof(dest));
                if (!dest.CanWrite) ThrowHelper.ThrowArgumentException("Cannot write to stream", nameof(dest));
                //if (model is null) ThrowHelper.ThrowArgumentNullException("model");
                obj.dest = dest;
                obj.ioBuffer = BufferPool.GetBuffer();
                return obj;
            }

            internal override void Init(TypeModel model, object userState, bool impactCount)
            {
                base.Init(model, userState, impactCount);
                _nullWriter.Init(model, userState, impactCount: false);
                ioIndex = 0;
                flushLock = 0;
            }

            internal override void Dispose()
            {
                base.Dispose();
                Pool<StreamProtoWriter>.Put(this);
            }

            protected private override void Cleanup()
            {
                base.Cleanup();
                // importantly, this does **not** own the stream, and does not dispose it
                _nullWriter.Cleanup();
                dest = null;
                BufferPool.ReleaseBufferToPool(ref ioBuffer);
            }

            private static void IncrementedAndReset(int length, ref State state, StreamProtoWriter writer)
            {
                Debug.Assert(length >= 0);
                state.LocalAdvance(length);
                writer.WireType = WireType.None;
            }

            // ---- where the pending bytes are counted (notes/nano-writer.md, the buffer core) ----
            //
            // ioBuffer belongs to the WRITER and cannot move onto State the way a buffer-writer
            // lease does: back-filling a length prefix reaches into bytes already written, and
            // resizing replaces the array outright. What moves is the POSITION within it, from
            // the writer's ioIndex to state.OffsetInCurrent - which is the whole point, since a
            // span-direct raw op maintains that offset and touches nothing else, so the fast arm
            // added in cut 9 can finally fire on this backend.
            //
            // ioIndex is therefore the SOLID form: it is authoritative only while no State is
            // active over the buffer, which is the museum API's world (one State per call, see
            // the bridge on ProtoWriter). Everything else asks Pending.

            private byte[] ioBuffer;
            private int ioIndex;

            /// <summary>
            /// Pending (written but not yet handed to the stream) bytes: from the live state if
            /// there is one, else from the solid field. The ONE place the two forms meet.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int Pending(in State state) => state.IsActive ? state.OffsetInCurrent : ioIndex;

            protected internal override State DefaultState()
            {
                // liquify: the museum API's temporary state adopts the buffer where the last
                // one left it. Paired with Solidify, which is not optional - see ProtoWriter
                var state = new State(this);
                state.Init(ioBuffer, ioIndex);
                return state;
            }

            internal override void Solidify(ref State state)
            {
                if (state.IsActive) ioIndex = state.OffsetInCurrent;
            }

            // the deferred-position invariant: the committed count only moves where bytes
            // actually leave for the destination stream - a flush, or one of the
            // write-straight-through arms below
            private protected override long GetUncommitted(in State state) => Pending(in state);

            private protected override bool TryFlush(ref State state)
            {
                if (flushLock != 0) return false;
                int pending = Pending(in state);
                if (pending != 0 && dest is not null)
                {
                    dest.Write(ioBuffer, 0, pending);
                    Advance(pending);
                    if (state.IsActive) state.Init(ioBuffer, 0);
                    else ioIndex = 0;
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void DemandSpace(int required, StreamProtoWriter writer, ref State state)
            {
                if (state.RemainingInCurrent < required) MakeSpace(required, writer, ref state);
            }

            /// <summary>
            /// Adopt the buffer, empty it, or grow it - in that order, and out of line, since a
            /// chunk with room is the case worth being fast.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void MakeSpace(int required, StreamProtoWriter writer, ref State state)
            {
                if (!state.IsActive)
                {
                    // first write through this state: take the buffer where the solid form left it
                    state.Init(writer.ioBuffer, writer.ioIndex);
                    writer._needFlush = true;
                    if (state.RemainingInCurrent >= required) return;
                }

                if (writer.TryFlush(ref state) && state.RemainingInCurrent >= required) return;

                // either the buffer cannot be emptied (a sub-item is mid-flight and its length
                // has still to be back-filled) or emptying did not free enough: grow it. The
                // array is REPLACED, so the span has to be re-taken at the same offset - the one
                // hazard this whole shape introduces, and the reason nothing may cache ioBuffer
                // across a DemandSpace
                int offset = state.OffsetInCurrent;
                BufferPool.ResizeAndFlushLeft(ref writer.ioBuffer, required + offset, 0, offset);
                state.Init(writer.ioBuffer, offset);
            }

            protected private override void ImplWriteBytes(ref State state, ReadOnlySpan<byte> bytes)
            {
                var length = bytes.Length;
                if (flushLock != 0 || length <= ioBuffer.Length) // write to the buffer
                {
                    DemandSpace(length, this, ref state);
                    state.LocalWriteBytes(bytes);
                }
                else
                {
                    // writing data that is bigger than the buffer (and the buffer
                    // isn't currently locked due to a sub-object needing the size backfilled)
                    state.Flush(); // commit any existing data from the buffer
                                   // now just write directly to the underlying stream

#if PLAT_SPAN_OVERLOADS
                    dest.Write(bytes);
#else
                    WriteFallback(bytes, dest);
#endif
                    Advance(length); // straight through: committed without ever being pending
                    // since we've flushed offset etc is 0, and remains
                    // zero since we're writing directly to the stream
                }
            }

#if !PLAT_SPAN_OVERLOADS
            static void WriteFallback(ReadOnlySpan<byte> bytes, Stream stream)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(2048);
                try
                {
                    var target = new Span<byte>(buffer);
                    var capacity = target.Length;
                    // add all the chunks of (buffer size)
                    while (bytes.Length >= capacity)
                    {
                        bytes.Slice(0, capacity).CopyTo(target);
                        stream.Write(buffer, 0, capacity);
                        bytes = bytes.Slice(start: capacity);
                    }
                    // and anything that is left
                    bytes.CopyTo(target);
                    stream.Write(buffer, 0, bytes.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
#endif
            private protected override void ImplWriteBytes(ref State state, System.Buffers.ReadOnlySequence<byte> data)
            {
                int length = checked((int)data.Length);
                if (length == 0) return;
                if (flushLock != 0 || length <= ioBuffer.Length) // write to the buffer
                {
                    DemandSpace(length, this, ref state);
                    System.Buffers.BuffersExtensions.CopyTo(data, state.Remaining.Slice(0, length));
                    state.LocalAdvance(length);
                }
                else
                {
                    // writing data that is bigger than the buffer (and the buffer
                    // isn't currently locked due to a sub-object needing the size backfilled)
                    state.Flush(); // commit any existing data from the buffer
                                      // now just write directly to the underlying stream
                    foreach(var chunk in data)
                    {
#if PLAT_SPAN_OVERLOADS
                        dest.Write(chunk.Span);
#else
                        if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(chunk, out var segment))
                        {
                            dest.Write(segment.Array, segment.Offset, segment.Count);
                        }
                        else
                        {
                            var arr = System.Buffers.ArrayPool<byte>.Shared.Rent(chunk.Length);
                            try
                            {
                                chunk.CopyTo(arr);
                                dest.Write(arr, 0, chunk.Length);
                            }
                            finally
                            {
                                System.Buffers.ArrayPool<byte>.Shared.Return(arr);
                            }
                        }
#endif
                    }
                    Advance(length); // straight through: committed without ever being pending

                    // since we've flushed offset etc is 0, and remains
                    // zero since we're writing directly to the stream
                }
            }

            private protected override void ImplWriteString(ref State state, string value, int expectedBytes)
            {
                DemandSpace(expectedBytes, this, ref state);
                state.LocalWriteString(value);
            }

            private static void WriteUInt32ToBuffer(uint value, byte[] buffer, int index)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(index, 4), value);
            }

            private protected override void ImplWriteFixed32(ref State state, uint value)
            {
                DemandSpace(4, this, ref state);
                state.LocalWriteFixed32(value);
            }
            private protected override void ImplWriteFixed64(ref State state, ulong value)
            {
                DemandSpace(8, this, ref state);
                state.LocalWriteFixed64(value);
            }

            internal override int ImplWriteVarint64(ref State state, ulong value)
            {
                DemandSpace(10, this, ref state);
                return state.LocalWriteVarint64(value);
            }

            private protected override int ImplWriteVarint32(ref State state, uint value)
            {
                DemandSpace(5, this, ref state);
                return state.LocalWriteVarint32(value);
            }

            private protected override void ImplCopyRawFromStream(ref State state, Stream source)
            {
                // one byte of headroom is enough to have a live span to count in; the buffer is
                // the writer's throughout, so this reads straight into it around the state's
                // offset rather than through the span
                DemandSpace(1, this, ref state);

                byte[] buffer = ioBuffer;
                int space = buffer.Length - state.OffsetInCurrent, bytesRead = 1; // 1 here to spoof case where already full

                // try filling the buffer first
                while (space > 0 && (bytesRead = source.Read(buffer, state.OffsetInCurrent, space)) > 0)
                {
                    state.LocalAdvance(bytesRead);
                    space -= bytesRead;
                }
                if (bytesRead <= 0) return; // all done using just the buffer; stream exhausted

                // at this point the stream still has data, but buffer is full;
                if (flushLock == 0)
                {
                    // flush the buffer and write to the underlying stream instead
                    state.Flush();
                    while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dest.Write(buffer, 0, bytesRead);
                        Advance(bytesRead);
                    }
                }
                else
                {
                    while (true)
                    {
                        // need more space; resize (double) as necessary,
                        // requesting a reasonable minimum chunk each time
                        // (128 is the minimum; there may actually be much
                        // more space than this in the buffer). NOTE the resize replaces the
                        // array, so ioBuffer is re-read here rather than reusing `buffer`
                        DemandSpace(128, this, ref state);
                        if ((bytesRead = source.Read(ioBuffer, state.OffsetInCurrent,
                            ioBuffer.Length - state.OffsetInCurrent)) <= 0)
                        {
                            break;
                        }
                        state.LocalAdvance(bytesRead);
                    }
                }
            }
            private protected override SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style)
            {
                switch (WireType)
                {
                    case WireType.String:
                        WireType = WireType.None;
                        DemandSpace(32, this, ref state); // make some space in anticipation...
                        flushLock++;
                        var at = state.OffsetInCurrent;
                        state.LocalAdvance(1); // leave 1 space (optimistic) for length
                        return new SubItemToken((long)at);
                    case WireType.Fixed32:
                        DemandSpace(32, this, ref state); // make some space in anticipation...
                        flushLock++;
                        SubItemToken token = new SubItemToken((long)state.OffsetInCurrent);
                        IncrementedAndReset(4, ref state, this); // leave 4 space (rigid) for length
                        return token;
                    default:
                        state.ThrowInvalidSerializationOperation();
                        return default;
                }
            }

            private protected override void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style)
            {
                // so we're backfilling the length into an existing sequence. The bytes are still
                // in the writer's buffer - flushLock is what guaranteed that - and the CURRENT
                // position is the state's, so the length is derived from that rather than from
                // the solid ioIndex, which is stale for the whole of a state-driven write
                int len;
                int value = (int)token.value64;
                switch (style)
                {
                    case PrefixStyle.Fixed32:
                        len = (int)(state.OffsetInCurrent - value - 4);
                        WriteUInt32ToBuffer((uint)len, ioBuffer, value);
                        break;
                    case PrefixStyle.Fixed32BigEndian:
                        len = (int)(state.OffsetInCurrent - value - 4);
                        byte[] buffer = ioBuffer;
                        WriteUInt32ToBuffer((uint)len, buffer, value);
                        // and swap the byte order
                        byte b = buffer[value];
                        buffer[value] = buffer[value + 3];
                        buffer[value + 3] = b;
                        b = buffer[value + 1];
                        buffer[value + 1] = buffer[value + 2];
                        buffer[value + 2] = b;
                        break;
                    case PrefixStyle.Base128:
                        // string - complicated because we only reserved one byte;
                        // if the prefix turns out to need more than this then
                        // we need to shuffle the existing data
                        len = (int)(state.OffsetInCurrent - value - 1);
                        int offset = 0;
                        uint tmp = (uint)len;
                        while ((tmp >>= 7) != 0) offset++;
                        if (offset == 0)
                        {
                            ioBuffer[value] = (byte)(len & 0x7F);
                        }
                        else
                        {
                            DemandSpace(offset, this, ref state);
                            // re-read AFTER DemandSpace: growing replaces the array
                            byte[] blob = ioBuffer;
                            Buffer.BlockCopy(blob, value + 1, blob, value + 1 + offset, len);
                            tmp = (uint)len;
                            do
                            {
                                blob[value++] = (byte)((tmp & 0x7F) | 0x80);
                            } while ((tmp >>= 7) != 0);
                            blob[value - 1] = (byte)(blob[value - 1] & ~0x80);
                            state.LocalAdvance(offset);
                        }
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(style));
                        break;
                }
                // and this object is no longer a blockage - also flush if sensible
                const int ADVISORY_FLUSH_SIZE = 1024;
                if (--flushLock == 0 && Pending(in state) >= ADVISORY_FLUSH_SIZE)
                {
                    state.Flush();
                }
            }
        }
    }
}