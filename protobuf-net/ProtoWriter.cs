using System;

using System.IO;
using System.Text;
using ProtoBuf.Meta;
#if MF
using OverflowException = System.ApplicationException;
#endif
namespace ProtoBuf
{
    public sealed class ProtoWriter : IDisposable
    {
        private Stream dest;
        TypeModel model;
        public static void WriteObject(object value, int key, ProtoWriter writer)
        {
            if (writer.model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }
            SubItemToken token = StartSubItem(value, writer);
            writer.model.Serialize(key, value, writer);
            EndSubItem(token, writer);
        }
        private int fieldNumber, flushLock;
        WireType wireType;

        public static void WriteFieldHeader(int fieldNumber, WireType wireType, ProtoWriter writer) {
            if (writer.wireType != WireType.None) throw new InvalidOperationException("Cannot write a " + wireType
                + " header until the " + writer.wireType + " data has been written");
            if(fieldNumber < 0) throw new ArgumentOutOfRangeException("fieldNumber");
#if DEBUG
            switch (wireType)
            {   // validate requested header-type
                case WireType.Fixed32:
                case WireType.Fixed64:
                case WireType.String:
                case WireType.StartGroup:
                case WireType.SignedVariant:
                case WireType.Variant:
                    break; // fine
                case WireType.None:
                case WireType.EndGroup:
                default:
                    throw new ArgumentException("Invalid wire-type: " + wireType, "wireType");                
            }
#endif
            writer.fieldNumber = fieldNumber;
            writer.wireType = wireType;

            WriteHeaderCore(fieldNumber, wireType, writer);
        }
        static void WriteHeaderCore(int fieldNumber, WireType wireType, ProtoWriter writer)
        {
            uint header = (((uint)writer.fieldNumber) << 3)
                | (((uint)writer.wireType) & 7);
            WriteUInt32Variant(header, writer);
        }

        public static void WriteBytes(byte[] data, ProtoWriter writer)
        {
            ProtoWriter.WriteBytes(data, 0, data.Length, writer);
        }
        public static void WriteBytes(byte[] data, int offset, int length, ProtoWriter writer)
        {
            if (data == null) throw new ArgumentNullException("blob");

            switch (writer.wireType)
            {
                case WireType.Fixed32:
                    if (length != 4) throw new ArgumentException("length");
                    goto CopyFixedLength;  // ugly but effective
                case WireType.Fixed64:
                    if (length != 8) throw new ArgumentException("length");
                    goto CopyFixedLength;  // ugly but effective
                case WireType.String:
                    WriteUInt32Variant((uint)length, writer);
                    if (length == 0) return;
                    if (writer.flushLock != 0 || length <= writer.ioBuffer.Length) // write to the buffer
                    {
                        goto CopyFixedLength; // ugly but effective
                    }
                    // writing data that is bigger than the buffer (and the buffer
                    // isn't currently locked due to a sub-object needing the size backfilled)
                    Flush(writer); // commit any existing data from the buffer
                    // now just write directly to the underlying stream
                    writer.dest.Write(data, offset, length);
                    writer.position += length; // since we've flushed offset etc is 0, and remains
                                        // zero since we're writing directly to the stream
                    writer.wireType = WireType.None;
                    return;
            }
            throw CreateException(writer);
        CopyFixedLength: // no point duplicating this lots of times, and don't really want another stackframe
            DemandSpace(length, writer);
            Helpers.BlockCopy(data, offset, writer.ioBuffer, writer.ioIndex, length);
            IncrementedAndReset(length, writer);
        }
        private static void IncrementedAndReset(int length, ProtoWriter writer)
        {
            Helpers.DebugAssert(length >= 0);
            writer.ioIndex += length;
            writer.position += length;
            writer.wireType = WireType.None;
        }
        int depth = 0;
        const int RecursionCheckDepth = 25;
        public static SubItemToken StartSubItem(object instance, ProtoWriter writer)
        {
            //Helpers.DebugWriteLine(writer.depth.ToString());
            //Helpers.DebugWriteLine("StartSubItem", instance);
            if (++writer.depth > RecursionCheckDepth)
            {
                Helpers.DebugWriteLine(writer.depth.ToString());
                throw new NotImplementedException();
            }
            switch (writer.wireType)
            {
                case WireType.StartGroup:
                    writer.wireType = WireType.None;
                    return new SubItemToken(-writer.fieldNumber);
                case WireType.String:
                    writer.wireType = WireType.None;
                    DemandSpace(32, writer); // make some space in anticipation...
                    writer.flushLock++;
                    writer.position++;
                    return new SubItemToken(writer.ioIndex++); // leave 1 space (optimistic) for length
                default:
                    throw CreateException(writer);
            }
        }
        public static void EndSubItem(SubItemToken token, ProtoWriter writer)
        {
            if (writer.wireType != WireType.None) { throw CreateException(writer); }
            int value = token.value;
            if (writer.depth-- > 0)
            {
                if (value < 0)
                { // group
                    WriteHeaderCore(-value, WireType.EndGroup, writer);
                }
                else
                { // string
                    int len = (int)((writer.position - value) - 1);
                    int offset = 0;
                    uint tmp = (uint)len;
                    while ((tmp >>= 7) != 0) offset++;
                    if (offset == 0)
                    {
                        writer.ioBuffer[value] = (byte)(len & 0x7F);
                    }
                    else
                    {
                        DemandSpace(offset, writer);
                        byte[] blob = writer.ioBuffer;
                        Helpers.BlockCopy(blob, value + 1, blob, value + 1 + offset, len);
                        tmp = (uint)len;
                        do
                        {
                            blob[value++] = (byte)((tmp & 0x7F) | 0x80);
                        } while ((tmp >>= 7) != 0);
                        writer.position += offset;
                        writer.ioIndex += offset;
                    }
                    writer.flushLock--;
                }                
                writer.wireType = WireType.None;
            }
            else
            {
                throw CreateException(writer);
            }
        }
        public ProtoWriter(Stream dest, TypeModel model)
        {
            if (dest == null) throw new ArgumentNullException("dest");
            if (!dest.CanWrite) throw new ArgumentException("Cannot write to stream", "dest");
            //if (model == null) throw new ArgumentNullException("model");
            this.dest = dest;
            this.ioBuffer = BufferPool.GetBuffer();
            this.model = model;
            this.wireType = WireType.None;
        }
        
        void IDisposable.Dispose()
        { // importantly, this does **not** own the stream, and does not dispose it
            if (dest != null)
            {
                try { Flush(this); }
#if CF
                catch {}
#else
                catch (Exception ex)
                {
                    Helpers.TraceWriteLine(ex.ToString()); // but swallow and keey runningf
                }
#endif
                dest = null;
            }
            model = null;
            BufferPool.ReleaseBufferToPool(ref ioBuffer);
        }

        private byte[] ioBuffer;
        private int ioIndex;
        internal static int GetPosition(ProtoWriter writer) { return writer.position; }
        private int position;
        private static void DemandSpace(int required, ProtoWriter writer)
        {
            // check for enough space
            if ((writer.ioBuffer.Length - writer.ioIndex) < required)
            {
                if (writer.flushLock == 0)
                {
                    Flush(writer); // try emptying the buffer
                    if ((writer.ioBuffer.Length - writer.ioIndex) >= required) return;
                }
                // either can't empty the buffer, or
                // that didn't help; try doubling it
                int newLen = writer.ioBuffer.Length * 2;
                if (newLen < (required + writer.ioIndex))
                {
                    // or just make it big enough!
                    newLen = required + writer.ioIndex;
                }

                byte[] newBuffer = new byte[newLen];
                Helpers.BlockCopy(writer.ioBuffer, 0, newBuffer, 0, writer.ioIndex);
                if (writer.ioBuffer.Length == BufferPool.BufferLength)
                {
                    BufferPool.ReleaseBufferToPool(ref writer.ioBuffer);
                }
                writer.ioBuffer = newBuffer;
            }
        }
        public void Close()
        {
            if (depth != 0 || flushLock != 0) throw new InvalidOperationException("Unable to close stream in an incomplete state");
            Flush(this);
        }
        private static void Flush(ProtoWriter writer)
        {
            if (writer.flushLock == 0)
            {
                if (writer.ioIndex != 0)
                {
                    writer.dest.Write(writer.ioBuffer, 0, writer.ioIndex);
                    writer.ioIndex = 0;
                }
            }
            else
            {
                throw CreateException(writer);
            }
        }
        private static void WriteUInt32Variant(uint value, ProtoWriter writer)
        {
            DemandSpace(5, writer);
            int count = 0;
            do {
                writer.ioBuffer[writer.ioIndex++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            writer.ioBuffer[writer.ioIndex - 1] &= 0x7F;
            writer.position += count;
        }
 
        static readonly UTF8Encoding encoding = new UTF8Encoding();

        internal static uint Zig(int value)
        {        
            return (uint)((value << 1) ^ (value >> 31));
        }
        internal static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }
        private static void WriteUInt64Variant(ulong value, ProtoWriter writer)
        {
            DemandSpace(10, writer);
            int count = 0;
            do
            {
                writer.ioBuffer[writer.ioIndex++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            writer.ioBuffer[writer.ioIndex - 1] &= 0x7F;
            writer.position += count;
        }
        public static void WriteString(string value, ProtoWriter writer)
        {
            if (writer.wireType != WireType.String) throw CreateException(writer);
            if (value == null) throw new ArgumentNullException("value"); // written header; now what?
            int len = value.Length;
            if (len == 0)
            {
                WriteUInt32Variant(0, writer);
                writer.wireType = WireType.None;
                return; // just a header
            }
#if MF
            byte[] bytes = encoding.GetBytes(value);
            int actual = bytes.Length;
            writer.WriteUInt32Variant((uint)actual);
            writer.Ensure(actual);
            Helpers.BlockCopy(bytes, 0, writer.ioBuffer, writer.ioIndex, actual);
#else
            int predicted = encoding.GetByteCount(value);
            WriteUInt32Variant((uint)predicted, writer);
            DemandSpace(predicted, writer);
            int actual = encoding.GetBytes(value, 0, value.Length, writer.ioBuffer, writer.ioIndex);
            Helpers.DebugAssert(predicted == actual);
#endif
            IncrementedAndReset(actual, writer);
        }
        public static void WriteUInt64(ulong value, ProtoWriter writer)
        {
            switch (writer.wireType)
            {
                case WireType.Fixed64:
                    ProtoWriter.WriteInt64((long)value, writer);
                    return;
                case WireType.Variant:
                    WriteUInt64Variant(value, writer);
                    writer.wireType = WireType.None;
                    return;
                case WireType.Fixed32:
                    checked { ProtoWriter.WriteUInt32((uint)value, writer); }
                    return;
                default:
                    throw CreateException(writer);
            }
        }

        public static void WriteInt64(long value, ProtoWriter writer)
        {
            byte[] buffer;
            int index;
            switch (writer.wireType)
            {
                case WireType.Fixed64:
                    DemandSpace(8, writer);
                    buffer = writer.ioBuffer;
                    index = writer.ioIndex;
                    buffer[index] = (byte)value;
                    buffer[index + 1] = (byte)(value >> 8);
                    buffer[index + 2] = (byte)(value >> 16);
                    buffer[index + 3] = (byte)(value >> 24);
                    buffer[index + 4] = (byte)(value >> 32);
                    buffer[index + 5] = (byte)(value >> 40);
                    buffer[index + 6] = (byte)(value >> 48);
                    buffer[index + 7] = (byte)(value >> 56);
                    IncrementedAndReset(8, writer);
                    return;
                case WireType.SignedVariant:
                    WriteUInt64Variant(Zig(value), writer);
                    writer.wireType = WireType.None;
                    return;
                case WireType.Variant:
                    if (value >= 0)
                    {
                        WriteUInt64Variant((ulong)value, writer);
                        writer.wireType = WireType.None;
                    }
                    else
                    {
                        DemandSpace(10, writer);
                        buffer = writer.ioBuffer;
                        index = writer.ioIndex;
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
                        IncrementedAndReset(10, writer);
                    }
                    return;
                case WireType.Fixed32:
                    checked { WriteInt32((int)value, writer); }
                    return;
                default:
                    throw CreateException(writer);
            }
        }

        public static void WriteUInt32(uint value, ProtoWriter writer)
        {
            switch (writer.wireType)
            {
                case WireType.Fixed32:
                    ProtoWriter.WriteInt32((int)value, writer);
                    return;
                case WireType.Fixed64:
                    ProtoWriter.WriteInt64((int)value, writer);
                    return;
                case WireType.Variant:
                    WriteUInt32Variant(value, writer);
                    writer.wireType = WireType.None;
                    return;
                default:
                    throw CreateException(writer);
            }
        }


        public static void WriteInt16(short value, ProtoWriter writer)
        {
            ProtoWriter.WriteInt32(value, writer);
        }
        public static void WriteUInt16(ushort value, ProtoWriter writer)
        {
            ProtoWriter.WriteUInt32(value, writer);
        }
        public static void WriteInt32(int value, ProtoWriter writer)
        {
            byte[] buffer;
            int index;
            switch (writer.wireType)
            {
                case WireType.Fixed32:
                    DemandSpace(4, writer);
                    buffer = writer.ioBuffer;
                    index = writer.ioIndex;
                    buffer[index] = (byte)value;
                    buffer[index + 1] = (byte)(value >> 8);
                    buffer[index + 2] = (byte)(value >> 16);
                    buffer[index + 3] = (byte)(value >> 24);
                    IncrementedAndReset(4, writer);
                    return;
                case WireType.Fixed64:
                    DemandSpace(8, writer);
                    buffer = writer.ioBuffer;
                    index = writer.ioIndex;
                    buffer[index] = (byte)value;
                    buffer[index + 1] = (byte)(value >> 8);
                    buffer[index + 2] = (byte)(value >> 16);
                    buffer[index + 3] = (byte)(value >> 24);
                    buffer[index + 4] = buffer[index + 5] =
                        buffer[index + 6] = buffer[index + 7] = 0;
                    IncrementedAndReset(8, writer);
                    return;
                case WireType.SignedVariant:
                    WriteUInt32Variant(Zig(value), writer);
                    writer.wireType = WireType.None;
                    return;
                case WireType.Variant:
                    if (value >= 0)
                    {
                        WriteUInt32Variant((uint)value, writer);
                        writer.wireType = WireType.None;
                    }
                    else
                    {
                        DemandSpace(10, writer);
                        buffer = writer.ioBuffer;
                        index = writer.ioIndex;
                        buffer[index] = (byte)(value | 0x80);
                        buffer[index + 1] = (byte)((value >> 7) | 0x80);
                        buffer[index + 2] = (byte)((value >> 14) | 0x80);
                        buffer[index + 3] = (byte)((value >> 21) | 0x80);
                        buffer[index + 4] = (byte)((value >> 28) | 0x80);
                        buffer[index + 5] = buffer[index + 6] =
                            buffer[index + 7] = buffer[index + 8] = (byte)0xFF;
                        buffer[index + 9] = (byte)0x01;
                        IncrementedAndReset(10, writer);
                    }
                    return;
                default:
                    throw CreateException(writer);
            }
            
        }

        public unsafe static void WriteDouble(double value, ProtoWriter writer)
        {
            switch (writer.wireType)
            {
                case WireType.Fixed32:
                    float f = (float)value;
                    if (Helpers.IsInfinity(f)
                        && !Helpers.IsInfinity(value))
                    {
                        throw new OverflowException();
                    }
                    ProtoWriter.WriteSingle(f, writer);
                    return;
                case WireType.Fixed64:
                    ProtoWriter.WriteInt64(*(long*)&value, writer);
                    return;
                default:
                    throw CreateException(writer);
            }
        }
        public unsafe static void WriteSingle(float value, ProtoWriter writer)
        {
            switch (writer.wireType)
            {
                case WireType.Fixed32:
                    ProtoWriter.WriteInt32(*(int*)&value, writer);
                    return;
                case WireType.Fixed64:
                    ProtoWriter.WriteDouble((double)value, writer);
                    return;
                default:
                    throw CreateException(writer);
            }
        }
        // general purpose serialization exception message
        private static Exception CreateException(ProtoWriter writer)
        {
            return new ProtoException("Invalid serialization operation with wire-type " + writer.wireType + " at position " + writer.position);
        }

        public static void WriteBoolean(bool value, ProtoWriter writer)
        {
            ProtoWriter.WriteUInt32(value ? (uint)1 : (uint)0, writer);
        }

        public static void SetSignedVariant(ProtoWriter writer)
        {
            switch (writer.wireType)
            {
                case WireType.Variant: writer.wireType = WireType.SignedVariant; break;
                case WireType.SignedVariant: break; // nothing to do
                default: throw CreateException(writer);
            }
        }
    }
}
