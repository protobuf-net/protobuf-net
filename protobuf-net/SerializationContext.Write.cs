using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ProtoBuf.Property;

namespace ProtoBuf
{
    internal sealed partial class SerializationContext
    {
        public void WriteByte(byte value)
        {
            Flush(1);
            ioBuffer[ioBufferIndex++] = value;
            position++;
        }
        
        
        public void WriteBlock(byte[] buffer, int offset, int count)
        {
            if (ioBufferIndex + count < IO_BUFFER_SIZE)
            { // enough space in the buffer
                Buffer.BlockCopy(buffer, offset, ioBuffer, ioBufferIndex, count);
                ioBufferIndex += count;
            }
            else
            { // not enough space; flush and write directly to the output
                // (assume blobs are big)
                Flush();
                stream.Write(buffer, offset, count);
            }
            position += count;
        }
        public void WriteFrom(Stream source, int length)
        {
            CheckSpace(length > BLIT_BUFFER_SIZE ? BLIT_BUFFER_SIZE : length);
            int max = workspace.Length, read;
            while ((length >= max) && (read = source.Read(workspace, 0, max)) > 0)
            {
                WriteBlock(workspace, 0, read);
                length -= read;
            }
            while ((length >= 0) && (read = source.Read(workspace, 0, length)) > 0)
            {
                WriteBlock(workspace, 0, read);
                length -= read;
            }
            if (length != 0) throw new EndOfStreamException();
        }
        public void Flush(int count)
        {
            if (ioBufferIndex + count >= IO_BUFFER_SIZE) Flush();
        }
        public void Flush()
        {
            if (ioBufferIndex > 0)
            {
                stream.Write(ioBuffer, 0, ioBufferIndex);
                ioBufferIndex = 0;
            }
        }

        internal int WriteLengthPrefixed<TValue>(TValue value, int underEstimatedLength, ILengthProperty<TValue> property)
        {
            Flush(); // commit to the stream before monkeying with the buffers...

            MemoryStream ms = stream as MemoryStream;
            if (ms != null)
            {
                // we'll write to out current stream, optimising
                // for the case when the length-prefix is 1-byte;
                // if not we'll have to BlockCopy
                int startIndex = (int)ms.Position,
                    guessLength = underEstimatedLength,
                    guessPrefixLength = this.EncodeInt32(guessLength),
                    actualLength = property.Serialize(value, this);

                if (guessLength == actualLength)
                { // good guess! nothing to do...
                    return guessPrefixLength + actualLength;
                }

                int actualPrefixLength = Base128Variant.GetLength(actualLength);

                Flush(); // commit to the stream before we start messing with it... 

                if (actualPrefixLength < guessPrefixLength)
                {
                    throw new ProtoException("Internal error; the serializer over-estimated the length. Sorry, but this shouldn't have happened.");
                }
                else if (actualPrefixLength > guessPrefixLength)
                {
                    // our guess of the length turned out to be bad; we need to
                    // fix things...

                    // extend the buffer to ensure we have space
                    for (int i = actualPrefixLength - guessPrefixLength; i > 0; i--)
                    {
                        ms.WriteByte(0);
                        position++;
                    }

                    // move the data
                    // (note; we MUST call GetBuffer *after* extending it,
                    // otherwise there the buffer might change if we extend
                    // over a boundary)
                    byte[] buffer = ms.GetBuffer();
                    Buffer.BlockCopy(buffer, startIndex + guessPrefixLength,
                        buffer, startIndex + actualPrefixLength, actualLength);
                }

                // back-fill the actual length into the buffer
                SerializationContext.EncodeUInt64((ulong)actualLength, ms.GetBuffer(), startIndex);
                return actualPrefixLength + actualLength;

            }
            else
            {
                // create a temporary stream and write the final result
                using (ms = new MemoryStream())
                {
                    SerializationContext ctx = new SerializationContext(ms, this);
                    int len = property.Serialize(value, ctx);
                    ctx.Flush();
                    this.ReadFrom(ctx);

                    int preambleLen = this.EncodeInt32(len);
                    byte[] buffer = ms.GetBuffer();
                    this.WriteBlock(buffer, 0, len);
                    return preambleLen + len;
                }
            }
        }
        public void WriteTo(Stream destination, int length)
        {
            CheckSpace(length > BLIT_BUFFER_SIZE ? BLIT_BUFFER_SIZE : length);
            int max = workspace.Length, read;
            position += length;
            while ((length >= max) && (read = stream.Read(workspace, 0, max)) > 0)
            {
                destination.Write(workspace, 0, read);
                length -= read;
            }
            while ((length > 0) && (read = stream.Read(workspace, 0, length)) > 0)
            {
                destination.Write(workspace, 0, read);
                length -= read;
            }
            if (length != 0) throw new EndOfStreamException();
        }
        public void WriteTo(SerializationContext destination, int length)
        {
            destination.Flush();
            WriteTo(destination.stream, length);
            destination.position += length;
        }


        public int EncodeUInt32(uint value)
        {
            if (value < 128)
            {
                Flush(1);
                ioBuffer[ioBufferIndex++] = (byte)value;
                position++;
                return 1;
            }
            
            Flush(5);
            int count = 0;
            do
            {
                ioBuffer[ioBufferIndex++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
                count++;
            } while (value != 0);
            ioBuffer[ioBufferIndex - 1] &= 0x7F;
            position += count;
            return count;
        }

        internal int EncodeInt64(long value)
        {
            return EncodeUInt64((ulong)value);
        }
        public static int EncodeUInt64(ulong value, byte[] buffer, int index)
        {
            int count = 0;
            do
            {
                buffer[index++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
                count++;
            } while (value != 0);
            buffer[index - 1] &= 0x7F;
            return count;
        }
        public int EncodeUInt64(ulong value)
        {
            if (value < 0x80)
            {
                Flush(1);
                ioBuffer[ioBufferIndex++] = (byte)value;
                return 1;
            }
            Flush(10);
            int count = EncodeUInt64(value, ioBuffer, ioBufferIndex);
            ioBufferIndex += count;
            position += count;
            return count;
        }
        public int EncodeInt32Fixed(int value)
        {
            Flush(4);
            ioBuffer[ioBufferIndex++] = (byte)(value & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 8) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 16) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 24) & 0xFF);
            position += 4;
            return 4;
        }
        public int EncodeInt64Fixed(long value)
        {
            Flush(8);
            ioBuffer[ioBufferIndex++] = (byte)(value & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 8) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 16) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 24) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 32) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 40) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 48) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)((value >> 56) & 0xFF);
            position += 8;
            return 8;
        }
        public int EncodeInt32(int value)
        {
            if (value >= 0) return EncodeUInt32((uint)value);
            
            Flush(10);
            ioBuffer[ioBufferIndex++] = (byte)((value | 0x80) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)(((value >> 7) | 0x80) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)(((value >> 14) | 0x80) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)(((value >> 21) | 0x80) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)(((value >> 28) | 0x80) & 0xFF);
            ioBuffer[ioBufferIndex++] = (byte)0xFF;
            ioBuffer[ioBufferIndex++] = (byte)0xFF;
            ioBuffer[ioBufferIndex++] = (byte)0xFF;
            ioBuffer[ioBufferIndex++] = (byte)0xFF;
            ioBuffer[ioBufferIndex++] = (byte)0x01;
            position += 10;
            return 10;
        }

        public int EncodeSingle(float value)
        {
            byte[] raw = BitConverter.GetBytes(value);
            Flush(4);
            if (BitConverter.IsLittleEndian)
            {
                ioBuffer[ioBufferIndex++] = raw[0];
                ioBuffer[ioBufferIndex++] = raw[1];
                ioBuffer[ioBufferIndex++] = raw[2];
                ioBuffer[ioBufferIndex++] = raw[3];
            }
            else
            {
                ioBuffer[ioBufferIndex++] = raw[3];
                ioBuffer[ioBufferIndex++] = raw[2];
                ioBuffer[ioBufferIndex++] = raw[1];
                ioBuffer[ioBufferIndex++] = raw[0];
            }
            position += 4;
            return 4;
        }
        public int EncodeDouble(double value)
        {
            byte[] raw = BitConverter.GetBytes(value);
            Flush(8);
            if (BitConverter.IsLittleEndian)
            {
                ioBuffer[ioBufferIndex++] = raw[0];
                ioBuffer[ioBufferIndex++] = raw[1];
                ioBuffer[ioBufferIndex++] = raw[2];
                ioBuffer[ioBufferIndex++] = raw[3];
                ioBuffer[ioBufferIndex++] = raw[4];
                ioBuffer[ioBufferIndex++] = raw[5];
                ioBuffer[ioBufferIndex++] = raw[6];
                ioBuffer[ioBufferIndex++] = raw[7];
            }
            else
            {
                ioBuffer[ioBufferIndex++] = raw[7];
                ioBuffer[ioBufferIndex++] = raw[6];
                ioBuffer[ioBufferIndex++] = raw[5];
                ioBuffer[ioBufferIndex++] = raw[4];
                ioBuffer[ioBufferIndex++] = raw[3];
                ioBuffer[ioBufferIndex++] = raw[2];
                ioBuffer[ioBufferIndex++] = raw[1];
                ioBuffer[ioBufferIndex++] = raw[0];
            }
            position += 8;
            return 8;
        }

        
    }
}
