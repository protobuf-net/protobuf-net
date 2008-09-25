using System;
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

        public static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }
        public static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
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
        /// <summary>
        /// Flushes the IO buffer if there is not enough space to complete the current operation.
        /// </summary>
        /// <param name="spaceRequired">The maximum number of bytes required by the current operation.</param>
        public void Flush(int spaceRequired)
        {
            if (ioBufferIndex + spaceRequired >= IO_BUFFER_SIZE) Flush();
        }
        /// <summary>
        /// Flushes the IO buffer, writing any cached data to the underlying stream and resetting the cache.
        /// </summary>
        public void Flush()
        {
            if (ioBufferIndex > 0)
            {
                stream.Write(ioBuffer, 0, ioBufferIndex);
                ioBufferIndex = 0;
            }
        }

        internal int WriteLengthPrefixed<TValue>(TValue value, uint underEstimatedLength, ILengthProperty<TValue> property)
        {
            Flush(); // commit to the stream before monkeying with the buffers...

            MemoryStream ms = stream as MemoryStream;
            if (ms != null)
            {
                // we'll write to out current stream, optimising
                // for the case when the length-prefix is 1-byte;
                // if not we'll have to BlockCopy
                int startIndex = (int)ms.Position;
                uint guessLength = underEstimatedLength,
                    guessPrefixLength = (uint) this.EncodeUInt32(guessLength),
                    actualLength = (uint) property.Serialize(value, this);

                if (guessLength == actualLength)
                { // good guess! nothing to do...
                    return (int)(guessPrefixLength + actualLength);
                }

                uint actualPrefixLength = (uint)SerializationContext.GetLength(actualLength);

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
                    for (uint i = actualPrefixLength - guessPrefixLength; i > 0; i--)
                    {
                        ms.WriteByte(0);
                        position++;
                    }

                    // move the data
                    // (note; we MUST call GetBuffer *after* extending it,
                    // otherwise there the buffer might change if we extend
                    // over a boundary)
                    byte[] buffer = ms.GetBuffer();
                    Buffer.BlockCopy(buffer, (int)(startIndex + guessPrefixLength),
                        buffer, (int)(startIndex + actualPrefixLength), (int)actualLength);
                }

                // back-fill the actual length into the buffer
                SerializationContext.EncodeUInt32(actualLength, ms.GetBuffer(), startIndex);
                return (int)(actualPrefixLength + actualLength);

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
            while ((length >= max) && (read = Read(workspace, 0, max)) > 0)
            {
                destination.Write(workspace, 0, read);
                length -= read;
            }
            while ((length > 0) && (read = Read(workspace, 0, length)) > 0)
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

        public static int EncodeUInt32(uint value, byte[] buffer, int index)
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

        public static int GetLength(uint value)
        {
            value >>= 7;
            if (value == 0) return 1;
            value >>= 7;
            if (value == 0) return 2;
            value >>= 7;
            if (value == 0) return 3;
            value >>= 7;
            return value == 0 ? 4 : 5;
        }
        public static int GetLength(int value)
        {
            if (value < 0) return 10;
            value >>= 7;
            if (value == 0) return 1;
            value >>= 7;
            if (value == 0) return 2;
            value >>= 7;
            if (value == 0) return 3;
            value >>= 7;
            return value == 0 ? 4 : 5;
        }

        public static int GetLength(long value)
        {
            if (value < 0) return 10;
            value >>= 7;
            if (value == 0) return 1;
            value >>= 7;
            if (value == 0) return 2;
            value >>= 7;
            if (value == 0) return 3;
            value >>= 7;
            if (value == 0) return 4;
            value >>= 7;
            if (value == 0) return 5;
            value >>= 7;
            if (value == 0) return 6;
            value >>= 7;
            if (value == 0) return 7;
            value >>= 7;
            if (value == 0) return 8;
            value >>= 7;
            return value == 0 ? 9 : 10;
        }
        public static int GetLength(ulong value)
        {
            return GetLength((long)value);
        }
        
    }
}
