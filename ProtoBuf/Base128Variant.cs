
using System.Runtime.Serialization;
using System.IO;
using System;
namespace ProtoBuf
{
    internal static class Base128Variant
    {
        private const long  INT64_MSB = 1 << 63;
        private const int   INT32_MSB = 1 << 31;

        public static int EncodeInt32(int value, SerializationContext context)
        {
            if((value & INT32_MSB)==0) {
                // msb not set; just encode
                return EncodeInt64((long)value, context);
            } else {
                // need to treat as a large -ve long (move the msb)
                long lVal = (long)(value ^ INT32_MSB);
                return EncodeInt64(lVal | INT64_MSB, context);
            }
        }
        public static int DecodeInt32(SerializationContext context)
        {
            long lVal = DecodeInt64(context);
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
            }
        }
        internal static int EncodeInt64(long value, SerializationContext context)
        {
            byte[] buffer = context.Workspace;
            int index = context.WorkspaceIndex;
            if ((value & ~((long)127)) == 0)
            {
                buffer[index] = (byte)value;
                return 1;
            }            
            int lastByte = 0;
            for (int i = 0; i < 10; i++)
            {
                int v = ((int) value) & 127;
                if (v != 0) lastByte = i;
                buffer[index + i] = (byte)(v | 128);
                value >>= 7;
            }
            // byte 10 inly needs 1 bit (but if -ve backfills >> with 1s)
            buffer[index + 9] &= 0x01;
            buffer[lastByte] &= 127; // strip the msb
            return lastByte + 1;
        }
        internal static long DecodeInt64(SerializationContext context)
        {
            long value = 0;

            Stream source = context.Stream;
            int b, tuple = 0;
            do
            {
                b = source.ReadByte();
                if (b < 0)
                {
                    // raise eof only on the first byte
                    if (tuple == 0 && context.Eof == Eof.Expected)
                    {
                        context.Eof = Eof.Ended;
                        return 0;
                    }
                    throw new EndOfStreamException();
                }
                if (tuple++ == 9)
                {
                    if ((b & (byte)254) != 0)
                    {
                        throw new SerializationException("Overflow reading Int64");
                    }
                    // add the final bit (9*7=63; only 1 bit needed from last tuple)
                    long usefulBits = (long)(b & 1);
                    value = (value << 1) | usefulBits;
                    break;
                }
                else
                {
                    long usefulBits = (long)(b & 127);
                    value = (value << 7) | usefulBits;
                }
            } while ((b & 128) != 0);

            return value;
        }
        internal static int ReadRaw(SerializationContext context)
        {
            int b, index = context.WorkspaceIndex, len = 0;
            Stream source = context.Stream;
            do
            {
                b = source.ReadByte();
                if (b < 0) throw new EndOfStreamException();
                context.Workspace[index++] = (byte)b;
                len++;
            } while ((b & 128) != 0);
            return len;
        }

        public static void Skip(Stream source)
        {
            int b;
            do
            {
                b = source.ReadByte();
                if (b < 0) throw new EndOfStreamException();
            } while ((b & 128) != 0);
        }
    }
}
