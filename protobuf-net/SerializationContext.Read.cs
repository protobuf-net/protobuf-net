
using System;
using System.IO;
namespace ProtoBuf
{
    internal sealed partial class SerializationContext
    {



        public static int Zag(uint ziggedValue)
        {
            int value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Base128Variant.Int32Msb);
        }

        public static long Zag(ulong ziggedValue)
        {
            long value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~Base128Variant.Int64Msb);
        }

        /// <summary>
        /// Slow (unbuffered) read from a stream; used to avoid issues
        /// with buffers when performing network IO.
        /// </summary>
        public static uint DecodeUInt32Fixed(Stream source)
        {
            byte[] buffer = new byte[4];
            int offset = 0, read;
            while(offset < 3 && (read = source.Read(buffer, offset, 4-offset)) > 0)
            {
                offset += read;
            }
            if (offset != 4) throw new EndOfStreamException();

            return ((uint)buffer[0])
            | (((uint)buffer[1]) << 8)
            | (((uint)buffer[2]) << 16)
            | (((uint)buffer[3]) << 24);

        }

        /// <summary>
        /// Slow (unbuffered) read from a stream; used to avoid issues
        /// with buffers when performing network IO.
        /// </summary>
        public static uint DecodeUInt32(Stream source)
        {
            if (source == null) throw new ArgumentNullException("source");

            int b = source.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            if ((b & 0x80) == 0) return (uint)b; // single-byte

            int shift = 7;

            uint value = (uint)(b & 0x7F);
            bool keepGoing;
            int i = 0;
            do
            {
                b = source.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                i++;
                keepGoing = (b & 0x80) != 0;
                value |= ((uint)(b & 0x7F)) << shift;
                shift += 7;
            } while (keepGoing && i < 4);
            if (keepGoing && i == 4)
            {
                throw new OverflowException();
            }
            return value;
        }

        public uint DecodeUInt32()
        {
            if (ioBufferIndex == ioBufferEffectiveSize)
            {
                if(inputStreamAvailable) Fill();
                if (ioBufferIndex == ioBufferEffectiveSize)
                {
                    if (this.IsEofExpected)
                    {
                        this.CheckNoRemainingGroups(); // if EOF then groups should be clean!
                        return 0;
                    }
                    throw new EndOfStreamException();
                }
            }
            byte b = ioBuffer[ioBufferIndex++];
            position++;
            if ((b & 0x80) == (byte)0) return b;

            Fill(4,false);
            int shift = 7;

            uint value = (uint)(b & 0x7F);
            bool keepGoing;
            int i = 0;
            do
            {
                if (ioBufferIndex == ioBufferEffectiveSize) throw new EndOfStreamException();
                b = ioBuffer[ioBufferIndex++];
                i++;
                keepGoing = (b & 0x80) != 0;
                value |= ((uint)(b & 0x7F)) << shift;
                shift += 7;
            } while (keepGoing && i < 4);
            position += i;
            if (keepGoing && i == 4)
            {
                throw new OverflowException();
            }
            return value;
        }

        public ulong DecodeUInt64()
        {
            return (ulong)DecodeInt64();
        }
        public int DecodeInt32()
        {
            return (int)DecodeInt64();
            /*long lVal = DecodeInt64(context);
            if ((lVal & INT64_MSB) == 0)
            {
                // msb not set; just cast
                return (int)lVal;
            }
            else
            {
                // treat large -ve long as -ve int (move the msb)
                int iVal = (int)(lVal ^ INT64_MSB);
                return iVal | INT32_MSB;
            }*/
        }

        private bool inputStreamAvailable = true;
        private int ioBufferEffectiveSize;
        /// <summary>
        /// Fills the IO buffer if there is not enough data buffered to complete the current operation.
        /// </summary>
        /// <param name="required">The maximum number of bytes required by the current operation.</param>
        /// <param name="demand">Should an exception be thrown if the data is not available?</param>
        void Fill(int required, bool demand)
        {
            int overflowIndex = ioBufferEffectiveSize - required;
            if (inputStreamAvailable && (ioBufferIndex > overflowIndex)) Fill();
            if (demand && (ioBufferIndex > overflowIndex))
            {
                throw new EndOfStreamException();
            }
        }

        /// <summary>
        /// Fills the IO buffer, moving any un-consumed data to the beginning of the cache.
        /// </summary>
        void Fill()
        {
            if (inputStreamAvailable)
            {
                int writeIndex = ioBufferEffectiveSize - ioBufferIndex;
                if (ioBufferIndex > 0)
                {
                    // copy any un-consumed data to the start of the stream
                    int destIndex = 0;
                    for (; ioBufferIndex < ioBufferEffectiveSize; ioBufferIndex++)
                    {
                        ioBuffer[destIndex++] = ioBuffer[ioBufferIndex];
                    }
                    ioBufferIndex = 0;
                }
                // read new data into the buffer (after any unconsumed data)
                int bytesRead = 1;
                while(writeIndex < IO_BUFFER_SIZE && 
                    (bytesRead = stream.Read(ioBuffer, writeIndex, IO_BUFFER_SIZE - writeIndex)) > 0) {

                    writeIndex += bytesRead;
                }
                // set the end conditions for the buffer
                if(bytesRead <= 0) inputStreamAvailable = false;
                ioBufferEffectiveSize = writeIndex;
            }
        }

        internal long DecodeInt64()
        {
            long value = 0; // the result (treated as binary
            int b, // the byte we read from the stream
                shift = 0; // the offset of the read data into the result binary
            do
            {
                b = this.ReadByte();
                if (b < 0)
                {
                    throw new EndOfStreamException();
                }

                if (shift == 63)
                {
                    // check that only the lsb is set in the final block
                    if ((b & (byte)254) != 0)
                    {
                        throw new OverflowException("Overflow reading Int64");
                    }

                    // add the final bit
                    long usefulBits = (long)(b & 1);
                    value |= (usefulBits << shift);
                    break;
                }
                else
                {
                    // received little-endian, so shift the data into place
                    long usefulBits = (long)(b & 127);
                    value |= (usefulBits << shift);
                    shift += 7;
                }
            }
            while ((b & 128) != 0);

            return value;
        }

        internal int ReadRawVariant()
        {
            int b, index = 0;
            do
            {
                b = this.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                workspace[index++] = (byte)b;
            }
            while ((b & 128) != 0 && index <= 10);
            if (index == 10 && (b & 128) != 0)
            {
                throw new OverflowException();
            }
            return index;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int cached = ioBufferEffectiveSize - ioBufferIndex;
            
            if (count <= cached)
            {   // all available from cache
                if (count <= 8)
                {
                    // copy manually for small BLOBs
                    int tmp = count;
                    while (--tmp >= 0)
                    {
                        buffer[offset++] = ioBuffer[ioBufferIndex++];
                    }
                }
                else
                {
                    // blit
                    Buffer.BlockCopy(ioBuffer, ioBufferIndex, buffer, offset, count);
                    ioBufferIndex += count;
                }
                position += count;
                return count;
            }

            int totalRead = 0;
            // check if some available from cache
            if (cached > 8)
            {
                // blit    
                Buffer.BlockCopy(ioBuffer, ioBufferIndex, buffer, offset, cached);
                count -= cached;
                ioBufferIndex += cached;
                totalRead += cached;
                offset += cached;
            }
            else if(cached > 0)
            {
                // copy manually for small BLOBs
                totalRead += cached;
                count -= cached;
                while (--cached >= 0)
                {
                    buffer[offset++] = ioBuffer[ioBufferIndex++];
                }
            }

            int bytesRead = 1;
            while (count > 0 && (bytesRead = stream.Read(buffer, offset, count)) > 0)
            {
                count -= bytesRead;
                totalRead += bytesRead;
                offset += bytesRead;
            }
            if (bytesRead <= 0) inputStreamAvailable = false;
            position += totalRead;
            return totalRead;
        }

        public int ReadByte()
        {
            Fill(1, false);
            if (ioBufferIndex < ioBufferEffectiveSize)
            {
                byte b = ioBuffer[ioBufferIndex++];
                position++;
                return b;
            }
            else
            {
                return -1;
            }
        }


        

        public float DecodeSingle()
        {
            Fill(4, true);
            if (!BitConverter.IsLittleEndian)
            {
                Reverse4(ioBuffer, ioBufferIndex);
            }
            float value = BitConverter.ToSingle(ioBuffer, ioBufferIndex);
            ioBufferIndex += 4;
            position += 4;
            return value;            
        }
        public double DecodeDouble()
        {
            Fill(8, true);
            if (!BitConverter.IsLittleEndian)
            {
                Reverse8(ioBuffer, ioBufferIndex);
            }
            double value = BitConverter.ToDouble(ioBuffer, ioBufferIndex);
            ioBufferIndex += 8;
            position += 8;
            return value;
        }
        public int DecodeInt32Fixed()
        {
            Fill(4, true);
            
            int value = 
                   ((int)ioBuffer[ioBufferIndex++])
                | (((int)ioBuffer[ioBufferIndex++]) << 8)
                | (((int)ioBuffer[ioBufferIndex++]) << 16)
                | (((int)ioBuffer[ioBufferIndex++]) << 24);
            position += 4;
            return value;
        }

        public long DecodeInt64Fixed()
        {
            Fill(8, true);
            uint lo =
                   ((uint)ioBuffer[ioBufferIndex++])
                | (((uint)ioBuffer[ioBufferIndex++]) << 8)
                | (((uint)ioBuffer[ioBufferIndex++]) << 16)
                | (((uint)ioBuffer[ioBufferIndex++]) << 24),
                hi =
                   ((uint)ioBuffer[ioBufferIndex++])
                | (((uint)ioBuffer[ioBufferIndex++]) << 8)
                | (((uint)ioBuffer[ioBufferIndex++]) << 16)
                | (((uint)ioBuffer[ioBufferIndex++]) << 24);

            position += 8;
            ulong loL = (ulong)lo, hiL = (ulong)hi;
            return (long)((hiL << 32) | loL);
        }
        public int Read(int count)
        {
            return Read(workspace, 0, count);
        }
        public void ReadBlock(int count)
        {
            if (Read(workspace, 0, count) != count)
            {
                throw new EndOfStreamException();
            }
        }
        public void ReadBlock(byte[] buffer, int index, int count)
        {
            if (Read(buffer, index, count) != count)
            {
                throw new EndOfStreamException();
            }
        }

    }
}
