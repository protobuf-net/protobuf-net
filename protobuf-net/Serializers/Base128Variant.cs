
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

        internal static int EncodeInt64(long value, SerializationContext context)
        {
            byte[] buffer = context.Workspace;
            if ((value & AllButFirstChunk) == 0)
            {
                context.Stream.WriteByte((byte)value);
                return 1;
            }
            else if ((value & AllButFirstTwoChunks) == 0)
            {
                int val = (int)value;
                buffer[0] = (byte)((val & 0x7F) | 0x80);
                buffer[1] = (byte)(val >> 7);
                context.Stream.Write(buffer, 0, 2);
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
            context.Stream.Write(buffer, 0, lastByte);
            return lastByte;
            
        }

        internal static long DecodeInt64(SerializationContext context)
        {
            long value = 0; // the result (treated as binary
            Stream source = context.Stream;
            int b, // the byte we read from the stream
                shift = 0; // the offset of the read data into the result binary
            do
            {
                b = source.ReadByte();
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
            Stream source = context.Stream;
            do
            {
                b = source.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                context.Workspace[index++] = (byte)b;
                len++;
            }
            while ((b & 128) != 0);
            return len;
        }

        public static void Skip(Stream source)
        {
            int b;
            do
            {
                b = source.ReadByte();
                if (b < 0) throw new EndOfStreamException();
            }
            while ((b & 128) != 0);
        }
    }
}
