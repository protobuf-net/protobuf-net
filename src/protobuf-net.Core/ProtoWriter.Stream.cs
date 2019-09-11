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
            protected internal override State DefaultState() => default;

            private Stream dest;
            private int flushLock;

            private protected override bool ImplDemandFlushOnDispose => true;

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

            private protected override bool TryFlush(ref State state)
            {
                if (flushLock != 0) return false;
                if (ioIndex != 0 && dest != null)
                {
                    dest.Write(ioBuffer, 0, ioIndex);
                    ioIndex = 0;
                }
                return true;
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
                if (writer.TryFlush(ref state) // try emptying the buffer
                    && (writer.ioBuffer.Length - writer.ioIndex) >= required)
                {
                    return;
                }

                // either can't empty the buffer, or that didn't help; need more space
                BufferPool.ResizeAndFlushLeft(ref writer.ioBuffer, required + writer.ioIndex, 0, writer.ioIndex);
            }

            private byte[] ioBuffer;
            private int ioIndex;

            protected private override void ImplWriteBytes(ref State state, byte[] data, int offset, int length)
            {
                if (flushLock != 0 || length <= ioBuffer.Length) // write to the buffer
                {
                    DemandSpace(length, this, ref state);
                    Buffer.BlockCopy(data, offset, ioBuffer, ioIndex, length);
                    ioIndex += length;
                }
                else
                {
                    // writing data that is bigger than the buffer (and the buffer
                    // isn't currently locked due to a sub-object needing the size backfilled)
                    Flush(ref state); // commit any existing data from the buffer
                                      // now just write directly to the underlying stream
                    dest.Write(data, offset, length);
                    // since we've flushed offset etc is 0, and remains
                    // zero since we're writing directly to the stream
                }
            }

            private protected override void ImplWriteBytes(ref State state, System.Buffers.ReadOnlySequence<byte> data)
            {
                int length = checked((int)data.Length);
                if (length == 0) return;
                if (flushLock != 0 || length <= ioBuffer.Length) // write to the buffer
                {
                    DemandSpace(length, this, ref state);
                    System.Buffers.BuffersExtensions.CopyTo(data, new Span<byte>(ioBuffer, ioIndex, length));
                    ioIndex += length;
                }
                else
                {
                    // writing data that is bigger than the buffer (and the buffer
                    // isn't currently locked due to a sub-object needing the size backfilled)
                    Flush(ref state); // commit any existing data from the buffer
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

                    // since we've flushed offset etc is 0, and remains
                    // zero since we're writing directly to the stream
                }
            }

            private protected override void ImplWriteString(ref State state, string value, int expectedBytes)
            {
                DemandSpace(expectedBytes, this, ref state);
                int actualBytes = UTF8.GetBytes(value, 0, value.Length, ioBuffer, ioIndex);
                ioIndex += actualBytes;
                Helpers.DebugAssert(expectedBytes == actualBytes);
            }

            private static void WriteUInt32ToBuffer(uint value, byte[] buffer, int index)
            {
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(index, 4), value);
            }

            private protected override void ImplWriteFixed32(ref State state, uint value)
            {
                DemandSpace(4, this, ref state);
                WriteUInt32ToBuffer(value, ioBuffer, ioIndex);
                ioIndex += 4;
            }
            private protected override void ImplWriteFixed64(ref State state, ulong value)
            {
                DemandSpace(8, this, ref state);
                var buffer = ioBuffer;
                var index = ioIndex;

                System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(index, 8), value);
                ioIndex += 8;
            }

            private protected override int ImplWriteVarint64(ref State state, ulong value)
            {
                DemandSpace(10, this, ref state);
                int count = 0;
                do
                {
                    ioBuffer[ioIndex++] = (byte)((value & 0x7F) | 0x80);
                    count++;
                } while ((value >>= 7) != 0);
                ioBuffer[ioIndex - 1] &= 0x7F;
                return count;
            }

            private protected override int ImplWriteVarint32(ref State state, uint value)
            {
                DemandSpace(5, this, ref state);
                int count = 0;
                do
                {
                    ioBuffer[ioIndex++] = (byte)((value & 0x7F) | 0x80);
                    count++;
                } while ((value >>= 7) != 0);
                ioBuffer[ioIndex - 1] &= 0x7F;
                return count;
            }

            private protected override void ImplCopyRawFromStream(ref State state, Stream source)
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
            private protected override SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style)
            {
                switch (WireType)
                {
                    case WireType.String:
                        WireType = WireType.None;
                        DemandSpace(32, this, ref state); // make some space in anticipation...
                        flushLock++;
                        Advance(1);
                        return new SubItemToken((long)(ioIndex++)); // leave 1 space (optimistic) for length
                    case WireType.Fixed32:
                        DemandSpace(32, this, ref state); // make some space in anticipation...
                        flushLock++;
                        SubItemToken token = new SubItemToken((long)ioIndex);
                        IncrementedAndReset(4, this); // leave 4 space (rigid) for length
                        return token;
                    default:
                        throw CreateException(this);
                }
            }

            private protected override void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style)
            {
                // so we're backfilling the length into an existing sequence
                int len;
                int value = (int)token.value64;
                switch (style)
                {
                    case PrefixStyle.Fixed32:
                        len = (int)(ioIndex - value - 4);
                        WriteUInt32ToBuffer((uint)len, ioBuffer, value);
                        break;
                    case PrefixStyle.Fixed32BigEndian:
                        len = (int)(ioIndex - value - 4);
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
        }
    }
}