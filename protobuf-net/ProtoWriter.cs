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
        WireType wireType = WireType.Error;

        public void WriteFieldHeader(int fieldNumber, WireType wireType) {
            if(fieldNumber < 0) throw new ArgumentOutOfRangeException("fieldNumber");
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
            if(data == null) return;
            switch(wireType) {
                case WireType.String:
                    int len = data.Length;
                    WriteUInt32Variant((uint)len);
                    if (len == 0) return;
                    if (flushLock != 0 || data.Length <= ioBuffer.Length)
                    {
                        Ensure(len);
                        Helpers.BlockCopy(data, 0, ioBuffer, ioIndex, len);
                        ioIndex += len;
                        position += len;
                    }
                    else
                    {   // writing data that is bigger than the buffer
                        Flush(); // commit any existing data from the buffer
                        // now just write directly to the underlying stream
                        dest.Write(data, 0, data.Length);
                        position += len;
                    }
                    return;
                default:
                    BorkedIt();
                    return;
            }
        }
        int depth;
        const int RecursionCheckDepth = 25;
        internal int StartSubItem(object instance)
        {
            if (++depth > RecursionCheckDepth)
            {
                throw new NotImplementedException();
            }
            switch(wireType) {
                case WireType.StartGroup:
                    return -fieldNumber;
                case WireType.String:
                    Ensure(32);
                    flushLock++;
                    position++;
                    return ioIndex++; // leave 1 space (optimistic) for length
                default:
                    BorkedIt(); // throws
                    return 0;
            }
        }
        internal void EndSubItem(int token)
        {
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
            }
            else
            {
                BorkedIt();
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
                BorkedIt();
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
            if (wireType != WireType.String) BorkedIt();
            if (value == null) return; // do nothing
            int len = value.Length;
            if (len == 0)
            {
                WriteUInt32Variant(0);
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
            ioIndex += actual;
            position += actual;

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
                    return;
                case WireType.Fixed32:
                    checked { WriteUInt32((uint)value); }
                    return;
                default:
                    BorkedIt();
                    return;
            }
        }

        public void WriteInt64(long value)
        {
            switch (wireType)
            {
                case WireType.Fixed64:
                    Ensure(8);
                    ioBuffer[ioIndex++] = (byte)value;
                    ioBuffer[ioIndex++] = (byte)(value >> 8);
                    ioBuffer[ioIndex++] = (byte)(value >> 16);
                    ioBuffer[ioIndex++] = (byte)(value >> 24);
                    ioBuffer[ioIndex++] = (byte)(value >> 32);
                    ioBuffer[ioIndex++] = (byte)(value >> 40);
                    ioBuffer[ioIndex++] = (byte)(value >> 48);
                    ioBuffer[ioIndex++] = (byte)(value >> 56);
                    position += 8;
                    return;
                case WireType.SignedVariant:
                    WriteUInt64Variant(Zig(value));
                    return;
                case WireType.Variant:
                    if (value >= 0)
                    {
                        WriteUInt64Variant((ulong)value);
                    }
                    else
                    {
                        Ensure(10);
                        ioBuffer[ioIndex++] = (byte)(value | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 7) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 14) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 21) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 28) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 35) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 42) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 49) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((int)(value >> 56) | 0x80);
                        ioBuffer[ioIndex++] = 0x01; // sign bit
                        position += 10;
                    }
                    return;
                case WireType.Fixed32:
                    checked { WriteInt32((int)value); }
                    return;
                default:
                    BorkedIt();
                    return;
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
                    return;
                default:
                    BorkedIt();
                    return;
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
                    ioBuffer[ioIndex++] = (byte)value;
                    ioBuffer[ioIndex++] = (byte)(value >> 8);
                    ioBuffer[ioIndex++] = (byte)(value >> 16);
                    ioBuffer[ioIndex++] = (byte)(value >> 24);
                    position += 4;
                    return;
                case WireType.Fixed64:
                    Ensure(8);
                    ioBuffer[ioIndex++] = (byte)value;
                    ioBuffer[ioIndex++] = (byte)(value >> 8);
                    ioBuffer[ioIndex++] = (byte)(value >> 16);
                    ioBuffer[ioIndex++] = (byte)(value >> 24);
                    ioBuffer[ioIndex++] = ioBuffer[ioIndex++] =
                        ioBuffer[ioIndex++] = ioBuffer[ioIndex++] = 0;
                    position += 8;
                    return;
                case WireType.SignedVariant:
                    WriteUInt32Variant(Zig(value));
                    return;
                case WireType.Variant:
                    if (value >= 0)
                    {
                        WriteUInt32Variant((uint)value);
                    }
                    else
                    {
                        Ensure(10);
                        ioBuffer[ioIndex++] = (byte)(value | 0x80);
                        ioBuffer[ioIndex++] = (byte)((value >> 7) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((value >> 14) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((value >> 21) | 0x80);
                        ioBuffer[ioIndex++] = (byte)((value >> 28) | 0x80);
                        ioBuffer[ioIndex++] = ioBuffer[ioIndex++] =
                            ioBuffer[ioIndex++] = ioBuffer[ioIndex++] = (byte)0xFF;
                        ioBuffer[ioIndex++] = (byte)0x01;
                        position += 10;
                    }
                    return;
                default:
                    BorkedIt();
                    return;
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
                    BorkedIt();
                    return;
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
                    BorkedIt();
                    return;
            }
        }
        private void BorkedIt()
        {
            throw new ProtoException();
        }

        public void WriteBoolean(bool value)
        {
            WriteUInt32(value ? (uint)1 : (uint)0);
        }

    }
}
