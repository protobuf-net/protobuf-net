using ProtoBuf.Meta;
using System;
using System.IO;

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
        [Obsolete(UseStateAPI, false)]
        public static ProtoWriter Create(Stream dest, TypeModel model, SerializationContext context = null)
            => Create(out _, dest, model, context);

        /// <summary>
        /// Creates a new writer against a stream
        /// </summary>
        /// <param name="dest">The destination stream</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to serialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        /// <param name="state">Writer state</param>
        public static ProtoWriter Create(out State state, Stream dest, TypeModel model, SerializationContext context = null)
        {
            state = default;
            return new StreamProtoWriter(dest, model, context);
        }
        private class StreamProtoWriter : ProtoWriter
        {
            private Stream dest;
            private int flushLock;

            internal StreamProtoWriter(Stream dest, TypeModel model, SerializationContext context)
                : base(model, context)
            {
                if (dest == null) throw new ArgumentNullException(nameof(dest));
                if (!dest.CanWrite) throw new ArgumentException("Cannot write to stream", nameof(dest));
                //if (model == null) throw new ArgumentNullException("model");
                this.dest = dest;
                ioBuffer = BufferPool.GetBuffer();
            }
            protected private override void Dispose()
            {
                base.Dispose();
                // importantly, this does **not** own the stream, and does not dispose it
                dest = null;
                BufferPool.ReleaseBufferToPool(ref ioBuffer);
            }

            private static void IncrementedAndReset(int length, StreamProtoWriter writer)
            {
                Helpers.DebugAssert(length >= 0);
                writer.ioIndex += length;
                writer.Advance(length);
                writer.WireType = WireType.None;
            }

            /// <summary>
            /// Writes any buffered data (if possible) to the underlying stream.
            /// </summary>
            /// <param name="state">Wwriter state</param>
            /// <remarks>It is not always possible to fully flush, since some sequences
            /// may require values to be back-filled into the byte-stream.</remarks>
            internal override void Flush(ref State state)
            {
                if (flushLock == 0 && ioIndex != 0 && dest != null)
                {
                    dest.Write(ioBuffer, 0, ioIndex);
                    ioIndex = 0;
                }
            }

            private static void DemandSpace(int required, StreamProtoWriter writer, ref State state)
            {
                // check for enough space
                if ((writer.ioBuffer.Length - writer.ioIndex) < required)
                {
                    TryFlushOrResize(required, writer, ref state);
                }
            }

            private static void TryFlushOrResize(int required, StreamProtoWriter writer, ref State state)
            {
                if (writer.flushLock == 0)
                {
                    writer.Flush(ref state); // try emptying the buffer
                    if ((writer.ioBuffer.Length - writer.ioIndex) >= required) return;
                }

                // either can't empty the buffer, or that didn't help; need more space
                BufferPool.ResizeAndFlushLeft(ref writer.ioBuffer, required + writer.ioIndex, 0, writer.ioIndex);
            }

            private byte[] ioBuffer;
            private int ioIndex;

            protected private override void WriteBytes(ref State state, byte[] data, int offset, int length)
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                switch (WireType)
                {
                    case WireType.Fixed32:
                        if (length != 4) throw new ArgumentException(nameof(length));
                        goto CopyFixedLength;  // ugly but effective
                    case WireType.Fixed64:
                        if (length != 8) throw new ArgumentException(nameof(length));
                        goto CopyFixedLength;  // ugly but effective
                    case WireType.String:
                        WriteUInt32Varint(ref state, (uint)length);
                        WireType = WireType.None;
                        if (length == 0) return;
                        if (flushLock != 0 || length <= ioBuffer.Length) // write to the buffer
                        {
                            goto CopyFixedLength; // ugly but effective
                        }
                        // writing data that is bigger than the buffer (and the buffer
                        // isn't currently locked due to a sub-object needing the size backfilled)
                        Flush(ref state); // commit any existing data from the buffer
                                     // now just write directly to the underlying stream
                        dest.Write(data, offset, length);
                        Advance(length); // since we've flushed offset etc is 0, and remains
                                         // zero since we're writing directly to the stream
                        return;
                }
                throw CreateException(this);
                CopyFixedLength: // no point duplicating this lots of times, and don't really want another stackframe
                DemandSpace(length, this, ref state);
                Buffer.BlockCopy(data, offset, ioBuffer, ioIndex, length);
                IncrementedAndReset(length, this);
            }
            private protected override void WriteString(ref State state, string value)
            {
                if (WireType != WireType.String) throw CreateException(this);
                if (value == null) throw new ArgumentNullException(nameof(value)); // written header; now what?
                int len = value.Length;
                if (len == 0)
                {
                    WriteUInt32Varint(ref state, 0);
                    WireType = WireType.None;
                    return; // just a header
                }
                int predicted = encoding.GetByteCount(value);
                WriteUInt32Varint(ref state, (uint)predicted);
                DemandSpace(predicted, this, ref state);
                int actual = encoding.GetBytes(value, 0, value.Length, ioBuffer, ioIndex);
                Helpers.DebugAssert(predicted == actual);
                IncrementedAndReset(actual, this);
            }
            private protected override void WriteInt64(ref State state, long value)
            {
                byte[] buffer;
                int index;
                switch (WireType)
                {
                    case WireType.Fixed64:
                        DemandSpace(8, this, ref state);
                        buffer = ioBuffer;
                        index = ioIndex;

#if PLAT_SPANS
                        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(index, 8), value);
#else
                        buffer[index] = (byte)value;
                        buffer[index + 1] = (byte)(value >> 8);
                        buffer[index + 2] = (byte)(value >> 16);
                        buffer[index + 3] = (byte)(value >> 24);
                        buffer[index + 4] = (byte)(value >> 32);
                        buffer[index + 5] = (byte)(value >> 40);
                        buffer[index + 6] = (byte)(value >> 48);
                        buffer[index + 7] = (byte)(value >> 56);
#endif
                        IncrementedAndReset(8, this);
                        return;
                    case WireType.SignedVariant:
                        WriteUInt64Varint(ref state, Zig(value));
                        WireType = WireType.None;
                        return;
                    case WireType.Variant:
                        if (value >= 0)
                        {
                            WriteUInt64Varint(ref state, (ulong)value);
                            WireType = WireType.None;
                        }
                        else
                        {
                            DemandSpace(10, this, ref state);
                            buffer = ioBuffer;
                            index = ioIndex;
                            buffer[index] = (byte)(value | 0x80);
                            buffer[index + 1] = (byte)((int)(value >> 7) | 0x80);
                            buffer[index + 2] = (byte)((int)(value >> 14) | 0x80);
                            buffer[index + 3] = (byte)((int)(value >> 21) | 0x80);
                            buffer[index + 4] = (byte)((int)(value >> 28) | 0x80);
                            buffer[index + 5] = (byte)((int)(value >> 35) | 0x80);
                            buffer[index + 6] = (byte)((int)(value >> 42) | 0x80);
                            buffer[index + 7] = (byte)((int)(value >> 49) | 0x80);
                            buffer[index + 8] = (byte)((int)(value >> 56) | 0x80);
                            buffer[index + 9] = 0x01; // sign bit
                            IncrementedAndReset(10, this);
                        }
                        return;
                    case WireType.Fixed32:
                        checked { WriteInt32((int)value, this, ref state); }
                        return;
                    default:
                        throw CreateException(this);
                }
            }
            private protected override void CopyRawFromStream(ref State state, Stream source)
            {
                byte[] buffer = ioBuffer;
                int space = buffer.Length - ioIndex, bytesRead = 1; // 1 here to spoof case where already full

                // try filling the buffer first   
                while (space > 0 && (bytesRead = source.Read(buffer, ioIndex, space)) > 0)
                {
                    ioIndex += bytesRead;
                    Advance(bytesRead);
                    space -= bytesRead;
                }
                if (bytesRead <= 0) return; // all done using just the buffer; stream exhausted

                // at this point the stream still has data, but buffer is full; 
                if (flushLock == 0)
                {
                    // flush the buffer and write to the underlying stream instead
                    Flush(ref state);
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
                        // more space than this in the buffer)
                        DemandSpace(128, this, ref state);
                        if ((bytesRead = source.Read(ioBuffer, ioIndex,
                            ioBuffer.Length - ioIndex)) <= 0)
                        {
                            break;
                        }
                        Advance(bytesRead);
                        ioIndex += bytesRead;
                    }
                }
            }
            private protected override SubItemToken StartSubItem(ref State state, object instance, bool allowFixed)
            {
                if (++depth > RecursionCheckDepth)
                {
                    CheckRecursionStackAndPush(instance);
                }
                if (packedFieldNumber != 0) throw new InvalidOperationException("Cannot begin a sub-item while performing packed encoding");
                switch (WireType)
                {
                    case WireType.StartGroup:
                        WireType = WireType.None;
                        return new SubItemToken((long)(-fieldNumber));
                    case WireType.String:
#if DEBUG
                    if (writer.model != null && writer.model.ForwardsOnly)
                    {
                        throw new ProtoException("Should not be buffering data: " + instance ?? "(null)");
                    }
#endif
                        WireType = WireType.None;
                        DemandSpace(32, this, ref state); // make some space in anticipation...
                        flushLock++;
                        Advance(1);
                        return new SubItemToken((long)(ioIndex++)); // leave 1 space (optimistic) for length
                    case WireType.Fixed32:
                        {
                            if (!allowFixed) throw CreateException(this);
                            DemandSpace(32, this, ref state); // make some space in anticipation...
                            flushLock++;
                            SubItemToken token = new SubItemToken((long)ioIndex);
                            IncrementedAndReset(4, this); // leave 4 space (rigid) for length
                            return token;
                        }
                    default:
                        throw CreateException(this);
                }
            }

            private protected override void EndSubItem(ref State state, SubItemToken token, PrefixStyle style)
            {
                if (WireType != WireType.None) { throw CreateException(this); }
                int value = (int)token.value64;
                if (depth <= 0) throw CreateException(this);
                if (depth-- > RecursionCheckDepth)
                {
                    PopRecursionStack();
                }
                packedFieldNumber = 0; // ending the sub-item always wipes packed encoding
                if (value < 0)
                {   // group - very simple append
                    WriteHeaderCore(-value, WireType.EndGroup, this, ref state);
                    WireType = WireType.None;
                    return;
                }

                // so we're backfilling the length into an existing sequence
                int len;
                switch (style)
                {
                    case PrefixStyle.Fixed32:
                        len = (int)(ioIndex - value - 4);
                        ProtoWriter.WriteInt32ToBuffer(len, ioBuffer, value);
                        break;
                    case PrefixStyle.Fixed32BigEndian:
                        len = (int)(ioIndex - value - 4);
                        byte[] buffer = ioBuffer;
                        ProtoWriter.WriteInt32ToBuffer(len, buffer, value);
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
                        len = (int)(ioIndex - value - 1);
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
                            byte[] blob = ioBuffer;
                            Buffer.BlockCopy(blob, value + 1, blob, value + 1 + offset, len);
                            tmp = (uint)len;
                            do
                            {
                                blob[value++] = (byte)((tmp & 0x7F) | 0x80);
                            } while ((tmp >>= 7) != 0);
                            blob[value - 1] = (byte)(blob[value - 1] & ~0x80);
                            Advance(offset);
                            ioIndex += offset;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(style));
                }
                // and this object is no longer a blockage - also flush if sensible
                const int ADVISORY_FLUSH_SIZE = 1024;
                if (--flushLock == 0 && ioIndex >= ADVISORY_FLUSH_SIZE)
                {
                    Flush(ref state);
                }
            }

            private protected override void WriteInt32(ref State state, int value)
            {
                byte[] buffer;
                int index;

                switch (WireType)
                {
                    case WireType.Fixed32:
                        DemandSpace(4, this, ref state);
                        WriteInt32ToBuffer(value, ioBuffer, ioIndex);
                        IncrementedAndReset(4, this);
                        return;
                    case WireType.Fixed64:
                        DemandSpace(8, this, ref state);
                        buffer = ioBuffer;
                        index = ioIndex;
                        buffer[index] = (byte)value;
                        buffer[index + 1] = (byte)(value >> 8);
                        buffer[index + 2] = (byte)(value >> 16);
                        buffer[index + 3] = (byte)(value >> 24);
                        buffer[index + 4] = buffer[index + 5] =
                            buffer[index + 6] = buffer[index + 7] = 0;
                        IncrementedAndReset(8, this);
                        return;
                    case WireType.SignedVariant:
                        WriteUInt32Varint(ref state, Zig(value));
                        WireType = WireType.None;
                        return;
                    case WireType.Variant:
                        if (value >= 0)
                        {
                            WriteUInt32Varint(ref state, (uint)value);
                            WireType = WireType.None;
                        }
                        else
                        {
                            DemandSpace(10, this, ref state);
                            buffer = ioBuffer;
                            index = ioIndex;
                            buffer[index] = (byte)(value | 0x80);
                            buffer[index + 1] = (byte)((value >> 7) | 0x80);
                            buffer[index + 2] = (byte)((value >> 14) | 0x80);
                            buffer[index + 3] = (byte)((value >> 21) | 0x80);
                            buffer[index + 4] = (byte)((value >> 28) | 0x80);
                            buffer[index + 5] = buffer[index + 6] =
                                buffer[index + 7] = buffer[index + 8] = (byte)0xFF;
                            buffer[index + 9] = (byte)0x01;
                            IncrementedAndReset(10, this);
                        }
                        return;
                    default:
                        throw CreateException(this);
                }
            }

            private protected override void WriteUInt32Varint(ref State state, uint value)
            {
                DemandSpace(5, this, ref state);
                int count = 0;
                do
                {
                    ioBuffer[ioIndex++] = (byte)((value & 0x7F) | 0x80);
                    count++;
                } while ((value >>= 7) != 0);
                ioBuffer[ioIndex - 1] &= 0x7F;
                Advance(count);
            }

            private protected override void WriteUInt64Varint(ref State state, ulong value)
            {
                DemandSpace(10, this, ref state);
                int count = 0;
                do
                {
                    ioBuffer[ioIndex++] = (byte)((value & 0x7F) | 0x80);
                    count++;
                } while ((value >>= 7) != 0);
                ioBuffer[ioIndex - 1] &= 0x7F;
                Advance(count);
            }

            private protected override bool CheckDepthFlushlockImpl(ref State state)
                => base.CheckDepthFlushlockImpl(ref state) || flushLock != 0;
        }
    }
}
