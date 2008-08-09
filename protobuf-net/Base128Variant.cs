
using System;
using System.IO;
namespace ProtoBuf
{
    internal static class Base128Variant
    {
        internal const long Int64Msb = ((long)1) << 63;
        internal const int Int32Msb = ((int)1) << 31;

        
        public static int EncodeUInt32(uint value, SerializationContext context)
        {
            if (value < 0x80)
            {
                context.WriteByte((byte)value);
                return 1;
            }
            byte[] buffer = context.Workspace;
            int index = -1;
            do
            {
                buffer[++index] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            } while (value != 0);
            buffer[index++] ^= 0x80;
            context.Write(index);
            return index;
        }
        public static int EncodeInt32(int value, SerializationContext context)
        {
            byte[] buffer;
            
            if (value < 0)
            {
                buffer = context.Workspace;
                buffer[0] = (byte)((value | 0x80) & 0xFF);
                buffer[1] = (byte)(((value >> 7) | 0x80) & 0xFF);
                buffer[2] = (byte)(((value >> 14) | 0x80) & 0xFF);
                buffer[3] = (byte)(((value >> 21) | 0x80) & 0xFF);
                buffer[4] = (byte)(((value >> 28) | 0x80) & 0xFF);
                buffer[5] = (byte)0xFF;
                buffer[6] = (byte)0xFF;
                buffer[7] = (byte)0xFF;
                buffer[8] = (byte)0xFF;
                buffer[9] = (byte)0x01;
                context.Write(10);
                return 10;
            } else if(value < 0x80) {
                context.WriteByte((byte)value);
                return 1;
            }
            else
            {
                buffer = context.Workspace;
                int index = -1;
                do
                {
                    buffer[++index] = (byte)((value & 0x7F) | 0x80);
                    value >>= 7;
                } while (value != 0);
                buffer[index++] ^= 0x80;
                context.Write(index);
                return index;
            }

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

        public static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }
        public static int Zag(uint ziggedValue)
        {
            int value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Base128Variant.Int32Msb);
        }
        public static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }
        public static long Zag(ulong ziggedValue)
        {
            long value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~Base128Variant.Int64Msb);
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


        public static uint DecodeUInt32(SerializationContext context)
        {
            return (uint)(int)DecodeInt64(context);
        }
        public static int EncodeUInt64(ulong value, byte[] buffer, int index)
        {
            if (value < 0x80)
            {
                buffer[index] = (byte)value;
                return 1;
            }
            do
            {
                buffer[index++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            } while (value != 0);
            buffer[index-1] ^= 0x80;
            return index;
        }
        public static int EncodeUInt64(ulong value, SerializationContext context)
        {
            if (value < 0x80)
            {
                context.WriteByte((byte)value);
                return 1;
            }
            byte[] buffer = context.Workspace;
            int index = -1;
            do
            {
                buffer[++index] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            } while (value != 0);
            buffer[index++] ^= 0x80;
            context.Write(index);
            return index;
        }
        public static ulong DecodeUInt64(SerializationContext context)
        {
            return (ulong)DecodeInt64(context);
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
            return EncodeUInt64((ulong)value, context);
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
                    if (shift == 0 && context.IsEofExpected)
                    {
                        context.CheckNoRemainingGroups(); // if EOF then groups should be clean!
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
