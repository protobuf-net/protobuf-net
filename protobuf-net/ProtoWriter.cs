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
        public void WriteObject(object value, int key)
        {
            if (model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }
            int token = StartSubItem(value);
            model.Serialize(key, value, this);
            EndSubItem(token);
        }
        private int fieldNumber, flushLock;
        WireType wireType;

        public void WriteFieldHeader(int fieldNumber, WireType wireType) {
            if (this.wireType != WireType.None) throw new InvalidOperationException("Cannot write a " + wireType
                + " header until the " + this.wireType + " data has been written");
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
            this.fieldNumber = fieldNumber;
            this.wireType = wireType;

            WriteHeaderCore(fieldNumber, wireType);
        }
        void WriteHeaderCore(int fieldNumber, WireType wireType)
        {
            uint header = (((uint)fieldNumber) << 3)
                | (((uint)wireType) & 7);
            WriteUInt32Variant(header);
        }

        public void WriteBytes(byte[] data)
        {
            WriteBytes(data, 0, data.Length);
        }
        public void WriteBytes(byte[] data, int offset, int length)
        {
            if (data == null) throw new ArgumentNullException("blob");
            
            switch(wireType) {
                case WireType.Fixed32:
                    if (length != 4) throw new ArgumentException("length");
                    goto CopyFixedLength;  // ugly but effective
                case WireType.Fixed64:
                    if (length != 8) throw new ArgumentException("length");
                    goto CopyFixedLength;  // ugly but effective
                case WireType.String:
                    WriteUInt32Variant((uint)length);
                    if (length == 0) return;
                    if (flushLock != 0 || length <= ioBuffer.Length) // write to the buffer
                    {
                        goto CopyFixedLength; // ugly but effective
                    }
                    // writing data that is bigger than the buffer (and the buffer
                    // isn't currently locked due to a sub-object needing the size backfilled)
                    Flush(); // commit any existing data from the buffer
                    // now just write directly to the underlying stream
                    dest.Write(data, offset, length);
                    position += length; // since we've flushed offset etc is 0, and remains
                                        // zero since we're writing directly to the stream
                    wireType = WireType.None;
                    return;
            }
            throw CreateException();
        CopyFixedLength: // no point duplicating this lots of times, and don't really want another stackframe
            Ensure(length);
            Helpers.BlockCopy(data, offset, ioBuffer, ioIndex, length);
            IncrementedAndReset(length);
        }
        private void IncrementedAndReset(int length)
        {
            Helpers.DebugAssert(length >= 0);
            ioIndex += length;
            position += length;
            wireType = WireType.None;
        }
        int depth;
        const int RecursionCheckDepth = 25;
        public int StartSubItem(object instance)
        {
            if (++depth > RecursionCheckDepth)
            {
                throw new NotImplementedException();
            }
            switch(wireType) {
                case WireType.StartGroup:
                    wireType = WireType.None;
                    return -fieldNumber;
                case WireType.String:
                    wireType = WireType.None;
                    Ensure(32); // make some space in anticipation...
                    flushLock++;
                    position++;                    
                    return ioIndex++; // leave 1 space (optimistic) for length
                default:
                    throw CreateException();
                    return 0;
            }
        }
        public void EndSubItem(int token)
        {
            if (wireType != WireType.None) { throw CreateException(); }
            if (flushLock > 0)
            {
                depth--;
                if (token < 0)
                {
                    WriteHeaderCore(-token, WireType.EndGroup);
                }
                else
                {   
                    int len = (int)((position - token) - 1);
                    int offset = 0;
                    uint tmp = (uint)len;
                    while ((tmp >>= 7) != 0) offset++;
                    if (offset == 0)
                    {
                        ioBuffer[token] = (byte)(len & 0x7F);
                    }
                    else
                    {
                        Ensure(offset);
                        Helpers.BlockCopy(ioBuffer, token + 1,
                            ioBuffer, token + 1 + offset, len);
                        tmp = (uint)len;
                        do
                        {
                            ioBuffer[token++] = (byte)((tmp & 0x7F) | 0x80);
                        } while ((tmp >>= 7) != 0);
                        position += offset;
                        ioIndex += offset;
                    }
                }
                flushLock--;
                wireType = WireType.None;
            }
            else
            {
                throw CreateException();
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
        
        public void Dispose()
        { // importantly, this does **not** own the stream, and does not dispose it
            if (dest != null)
            {
                try { Flush(); }
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
        internal int Position { get { return position; } }
        private int position;
        private void Ensure(int required)
        {
            if (Space < required)
            {
                if (flushLock == 0)
                {
                    Flush(); // try emptying the buffer
                    if (Space >= required) return;
                }
                // either can't empty the buffer, or
                // that didn't help; try doubling it
                int newLen = ioBuffer.Length * 2;
                if (newLen < (required + ioIndex)) {
                    // or just make it big enough!
                    newLen = required + ioIndex;
                }

                byte[] newBuffer = new byte[newLen];
                Helpers.BlockCopy(ioBuffer, 0, newBuffer, 0, ioIndex);
                if (ioBuffer.Length == BufferPool.BufferLength)
                {
                    BufferPool.ReleaseBufferToPool(ref ioBuffer);
                }
                ioBuffer = newBuffer;
            }
        }
        private void Flush()
        {
            if (flushLock == 0)
            {
                if (ioIndex != 0)
                {
                    dest.Write(ioBuffer, 0, ioIndex);
                    ioIndex = 0;
                }
            }
            else
            {
                throw CreateException();
            }
        }
        private void WriteUInt32Variant(uint value)
        {
            Ensure(5);
            int count = 0;
            do {
                ioBuffer[ioIndex++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            ioBuffer[ioIndex - 1] &= 0x7F;
            position += count;
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
        private void WriteUInt64Variant(ulong value)
        {
            Ensure(10);
            int count = 0;
            do
            {
                ioBuffer[ioIndex++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            ioBuffer[ioIndex - 1] &= 0x7F;
            position += count;
        }
        //private Encoder encoder = encoding.GetEncoder();
        private int Space
        {
            get { return ioBuffer.Length - ioIndex; }
        }
        public void WriteString(string value)
        {
            if (wireType != WireType.String) throw CreateException();
            if (value == null) throw new ArgumentNullException("value"); // written header; now what?
            int len = value.Length;
            if (len == 0)
            {
                WriteUInt32Variant(0);
                wireType = WireType.None;
                return; // just a header
            }
#if MF
            byte[] bytes = encoding.GetBytes(value);
            int actual = bytes.Length;
            WriteUInt32Variant((uint)actual);
            Ensure(actual);
            Helpers.BlockCopy(bytes, 0, ioBuffer, ioIndex, actual);
#else
            int predicted = encoding.GetByteCount(value);
            WriteUInt32Variant((uint)predicted);
            Ensure(predicted);
            int actual = encoding.GetBytes(value, 0, value.Length, ioBuffer, ioIndex);
            Helpers.DebugAssert(predicted == actual);
#endif
            IncrementedAndReset(actual);
            /*

            int maxNeededBytes = encoding.GetMaxByteCount(len), space = Space;
            if (maxNeededBytes > space && flushLock != 0) { Flush(); space = Space; }

            fixed (char* chars = value)
            fixed (byte* bytes = ioBuffer)
            {
                // if safe (note we can skip the check if it is short enough)
                int actualBytes;
                if (maxNeededBytes < space // not <= to allow for 1-byte length
                    && maxNeededBytes <= 127)
                {
                    actualBytes = encoder.GetBytes(chars, len, bytes + ioIndex + 1, space, true);
                    Helpers.DebugAssert(actualBytes <= 127);
                    bytes[ioIndex] = (byte)(actualBytes++); // backfill the length prefix
                    ioIndex += actualBytes;
                    position += actualBytes;
                } else {
                    // do it the hard way...
                    actualBytes = encoder.GetByteCount(chars, len, true);
                    WriteUInt32Variant((uint)actualBytes);
                    int maxBytesPerChar = ioBuffer.Length / encoding.GetMaxByteCount(1);
                    int bytesRead, charOffset = 0;
                    while ((bytesRead = PackBuffer(maxBytesPerChar,
                        chars, ref charOffset, ref len,
                        bytes, ref ioIndex, ioBuffer.Length - ioIndex)) > 0)
                    {
                        if (len > 0) Flush();
                        position += bytesRead;
                    }
                }
            }
             * */
        }
        /*
        private unsafe int PackBuffer(
            int maxBytesPerChar,
            char* chars, ref int charOffset, ref int charsRemaining,
            byte* bytes, ref int byteOffset, int bytesRemaining)
        {
            int charsToRead, totalBytes = 0;
            while (charsRemaining > 0
                && (charsToRead = bytesRemaining / maxBytesPerChar) > 0)
            {
                bool flush = false;
                if (charsToRead >= charsRemaining)
                {
                    // only flush at the end of the string; not just
                    // because the buffer is full...
                    charsToRead = charsRemaining;
                    flush = true;
                }
                int bytesRead = encoder.GetBytes(chars + charOffset, charsToRead, bytes + byteOffset, bytesRemaining, flush);
                charOffset += charsToRead;
                charsRemaining -= charsToRead;
                byteOffset += bytesRead;
                bytesRemaining -= bytesRead;
                totalBytes += bytesRead;
            }
            return totalBytes;
        }*/

        public void WriteUInt64(ulong value)
        {
            switch (wireType)
            {
                case WireType.Fixed64:
                    WriteInt64((long)value);
                    return;
                case WireType.Variant:
                    WriteUInt64Variant(value);
                    wireType = WireType.None;
                    return;
                case WireType.Fixed32:
                    checked { WriteUInt32((uint)value); }
                    return;
                default:
                    throw CreateException();
            }
        }

        public void WriteInt64(long value)
        {
            switch (wireType)
            {
                case WireType.Fixed64:
                    Ensure(8);
                    ioBuffer[ioIndex] = (byte)value;
                    ioBuffer[ioIndex+1] = (byte)(value >> 8);
                    ioBuffer[ioIndex+2] = (byte)(value >> 16);
                    ioBuffer[ioIndex+3] = (byte)(value >> 24);
                    ioBuffer[ioIndex+4] = (byte)(value >> 32);
                    ioBuffer[ioIndex+5] = (byte)(value >> 40);
                    ioBuffer[ioIndex+6] = (byte)(value >> 48);
                    ioBuffer[ioIndex+7] = (byte)(value >> 56);
                    IncrementedAndReset(8);
                    return;
                case WireType.SignedVariant:
                    WriteUInt64Variant(Zig(value));
                    wireType = WireType.None;
                    return;
                case WireType.Variant:
                    if (value >= 0)
                    {
                        WriteUInt64Variant((ulong)value);
                        wireType = WireType.None;
                    }
                    else
                    {
                        Ensure(10);
                        ioBuffer[ioIndex] = (byte)(value | 0x80);
                        ioBuffer[ioIndex+1] = (byte)((int)(value >> 7) | 0x80);
                        ioBuffer[ioIndex+2] = (byte)((int)(value >> 14) | 0x80);
                        ioBuffer[ioIndex+3] = (byte)((int)(value >> 21) | 0x80);
                        ioBuffer[ioIndex+4] = (byte)((int)(value >> 28) | 0x80);
                        ioBuffer[ioIndex+5] = (byte)((int)(value >> 35) | 0x80);
                        ioBuffer[ioIndex+6] = (byte)((int)(value >> 42) | 0x80);
                        ioBuffer[ioIndex+7] = (byte)((int)(value >> 49) | 0x80);
                        ioBuffer[ioIndex+8] = (byte)((int)(value >> 56) | 0x80);
                        ioBuffer[ioIndex+9] = 0x01; // sign bit
                        IncrementedAndReset(10);
                    }
                    return;
                case WireType.Fixed32:
                    checked { WriteInt32((int)value); }
                    return;
                default:
                    throw CreateException();
            }
        }

        public void WriteUInt32(uint value)
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    WriteInt32((int)value);
                    return;
                case WireType.Fixed64:
                    WriteInt64((int)value);
                    return;
                case WireType.Variant:
                    WriteUInt32Variant(value);
                    wireType = WireType.None;
                    return;
                default:
                    throw CreateException();
            }
        }


        public void WriteInt16(short value)
        {
            WriteInt32(value);
        }
        public void WriteUInt16(ushort value)
        {
            WriteUInt32(value);
        }
        public void WriteInt32(int value)
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    Ensure(4);
                    ioBuffer[ioIndex] = (byte)value;
                    ioBuffer[ioIndex+1] = (byte)(value >> 8);
                    ioBuffer[ioIndex+2] = (byte)(value >> 16);
                    ioBuffer[ioIndex+3] = (byte)(value >> 24);
                    IncrementedAndReset(4);
                    return;
                case WireType.Fixed64:
                    Ensure(8);
                    ioBuffer[ioIndex] = (byte)value;
                    ioBuffer[ioIndex+1] = (byte)(value >> 8);
                    ioBuffer[ioIndex+2] = (byte)(value >> 16);
                    ioBuffer[ioIndex+3] = (byte)(value >> 24);
                    ioBuffer[ioIndex+4] = ioBuffer[ioIndex+5] =
                        ioBuffer[ioIndex+6] = ioBuffer[ioIndex+7] = 0;
                    IncrementedAndReset(8);
                    return;
                case WireType.SignedVariant:
                    WriteUInt32Variant(Zig(value));
                    wireType = WireType.None;
                    return;
                case WireType.Variant:
                    if (value >= 0)
                    {
                        WriteUInt32Variant((uint)value);
                        wireType = WireType.None;
                    }
                    else
                    {
                        Ensure(10);
                        ioBuffer[ioIndex] = (byte)(value | 0x80);
                        ioBuffer[ioIndex+1] = (byte)((value >> 7) | 0x80);
                        ioBuffer[ioIndex+2] = (byte)((value >> 14) | 0x80);
                        ioBuffer[ioIndex+3] = (byte)((value >> 21) | 0x80);
                        ioBuffer[ioIndex+4] = (byte)((value >> 28) | 0x80);
                        ioBuffer[ioIndex+5] = ioBuffer[ioIndex+6] =
                            ioBuffer[ioIndex+7] = ioBuffer[ioIndex+8] = (byte)0xFF;
                        ioBuffer[ioIndex+9] = (byte)0x01;
                        IncrementedAndReset(10);
                    }
                    return;
                default:
                    throw CreateException();
            }
            
        }

        public unsafe void WriteDouble(double value)
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    float f = (float)value;
                    if (Helpers.IsInfinity(f)
                        && !Helpers.IsInfinity(value))
                    {
                        throw new OverflowException();
                    }
                    WriteSingle(f);
                    return;
                case WireType.Fixed64:
                    WriteInt64(*(long*)&value);
                    return;
                default:
                    throw CreateException();
            }
        }
        public unsafe void WriteSingle(float value)
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    WriteInt32(*(int*)&value);
                    return;
                case WireType.Fixed64:
                    WriteDouble((double)value);
                    return;
                default:
                    throw CreateException();
            }
        }
        // general purpose serialization exception message
        private Exception CreateException()
        {
            return new ProtoException("Invalid serialization operation with wire-type " + wireType + " at position " + position);
        }

        public void WriteBoolean(bool value)
        {
            WriteUInt32(value ? (uint)1 : (uint)0);
        }

    }
}
