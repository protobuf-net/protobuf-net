
using System;
using System.IO;
namespace ProtoBuf
{
    internal static class Base128Variant
    {
        internal const long Int64Msb = ((long)1) << 63;
        internal const int Int32Msb = ((int)1) << 31;

        public static int EncodeInt32(int value, SerializationContext context)
        {
            return EncodeInt64((long)value, context);

            #region unrolled, but doesn't help...
            //if(value < 0) return EncodeInt64((long)value, context);
            //byte[] buffer = context.Workspace;
            //buffer[0] = (byte)(value | 0x80);
            //if (value >= (1 << 7))
            //{
            //    buffer[1] = (byte)((value >> 7) | 0x80);
            //    if (value >= (1 << 14))
            //    {
            //        buffer[2] = (byte)((value >> 14) | 0x80);
            //        if (value >= (1 << 21))
            //        {
            //            buffer[3] = (byte)((value >> 21) | 0x80);
            //            if (value >= (1 << 28))
            //            {
            //                buffer[4] = (byte)(value >> 28); // last byte; no need to clear
            //                context.Stream.Write(buffer, 0, 5);
            //                return 5;
            //            }
            //            else
            //            {
            //                buffer[3] &= 0x7F;
            //                context.Stream.Write(buffer, 0, 4);
            //                return 4;
            //            }
            //        }
            //        else
            //        {
            //            buffer[2] &= 0x7F;
            //            context.Stream.Write(buffer, 0, 3);
            //            return 3;
            //        }
            //    }
            //    else
            //    {
            //        buffer[1] &= 0x7F;
            //        context.Stream.Write(buffer, 0, 2);
            //        return 2;
            //    }
            //}
            //else
            //{
            //    context.Stream.WriteByte(buffer[0] &= 0x7F);
            //    return 1;
            //}
#endregion
        }

        public static int DecodeInt32(SerializationContext context)
        {
            return (int)DecodeInt64(context);
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

        private const long AllButFirstChunk = ~((long)127);
        private const long AllButFirstTwoChunks = AllButFirstChunk << 7;

        internal static int EncodeInt64(long value, byte[] buffer)
        {
            if ((value & AllButFirstChunk) == 0)
            {
                buffer[0] = (byte)value;
                return 1;
            }
            else if ((value & AllButFirstTwoChunks) == 0)
            {
                int val = (int)value;
                buffer[0] = (byte)((val & 0x7F) | 0x80);
                buffer[1] = (byte)(val >> 7);
                return 2;
            }
            int lastByte = 0;
            for (int i = 0; i < 10; i++)
            {
                int v = ((int)value) & 127;
                if (v != 0) lastByte = i;
                buffer[i] = (byte)(v | 128);
                value >>= 7;
            }

            // byte 10 inly needs 1 bit (but if -ve backfills >> with 1s)
            buffer[9] &= 0x01;
            buffer[lastByte++] &= 127; // strip the msb
            return lastByte;
        }
        internal static int EncodeInt64(long value, SerializationContext context)
        {
            byte[] buffer = context.Workspace;
            if ((value & AllButFirstChunk) == 0)
            {
                context.WriteByte((byte)value);
                return 1;
            }
            else if ((value & AllButFirstTwoChunks) == 0)
            {
                int val = (int)value;
                buffer[0] = (byte)((val & 0x7F) | 0x80);
                buffer[1] = (byte)(val >> 7);
                context.Write(2);
                return 2;
            }
            int lastByte = 0;
            for (int i = 0; i < 10; i++)
            {
                int v = ((int)value) & 127;
                if (v != 0) lastByte = i;
                buffer[i] = (byte)(v | 128);
                value >>= 7;
            }

            // byte 10 inly needs 1 bit (but if -ve backfills >> with 1s)
            buffer[9] &= 0x01;
            buffer[lastByte++] &= 127; // strip the msb
            context.Write(lastByte);
            return lastByte;            
        }

        internal static long DecodeInt64(SerializationContext context)
        {
            long value = 0; // the result (treated as binary
            int b, // the byte we read from the stream
                shift = 0; // the offset of the read data into the result binary
            do
            {
                b = context.ReadByte();
                if (b < 0)
                {
                    // raise eof only on the first byte
                    if (shift == 0 && context.Eof == Eof.Expected)
                    {
                        context.CheckNoRemainingGroups(); // if EOF then groups should be clean!
                        context.Eof = Eof.Ended;
                        return 0;
                    }

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

        internal static int ReadRaw(SerializationContext context)
        {
            int b, index = 0, len = 0;
            byte[] buffer = context.Workspace;
            do
            {
                b = context.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                buffer[index++] = (byte)b;
                len++;
            }
            while ((b & 128) != 0);
            return len;
        }

        public static void Skip(SerializationContext context)
        {
            int b;
            do
            {
                b = context.ReadByte();
                if (b < 0) throw new EndOfStreamException();
            }
            while ((b & 128) != 0);
        }
    }
}
