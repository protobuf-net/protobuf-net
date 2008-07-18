
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
        internal static long ReverseInt64(long value)
        {
            byte tmp;
            byte[] buffer = BitConverter.GetBytes(value);
            tmp = buffer[0];
            buffer[0] = buffer[7];
            buffer[7] = tmp;
            tmp = buffer[1];
            buffer[1] = buffer[6];
            buffer[6] = tmp;
            tmp = buffer[2];
            buffer[2] = buffer[5];
            buffer[5] = tmp;
            tmp = buffer[3];
            buffer[3] = buffer[4];
            buffer[4] = tmp;
            return BitConverter.ToInt64(buffer, 0);
        }
        internal static int EncodeInt64(long value, SerializationContext context)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = ReverseInt64(value);
            }
            byte[] buffer = context.Workspace;
            int index = context.WorkspaceIndex;
            int lastByte = 0;
            for (int i = 0; i < 10; i++)
            {
                int v = ((int) value) & 127;
                if (v != 0) lastByte = i;
                buffer[index + i] = (byte)(v | 128);
                value >>= 7;
            }
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
                if (tuple++ == 10)
                {
                    throw new SerializationException("Overflow deserializing Int64");
                }

                long usefulBits = (long)(b & 127);
                value = (value << 7) | usefulBits;
            } while ((b & 128) != 0);

            return BitConverter.IsLittleEndian ? value : ReverseInt64(value);
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
