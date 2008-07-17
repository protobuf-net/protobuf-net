
using System.Runtime.Serialization;
using System.IO;
namespace ProtoBuf
{
    internal static class Base128Variant
    {
        internal static void DecodeFromStream(SerializationContext context, int length)
        {
            DecodeFromStream(context, length, true);
        }
        internal static bool DecodeFromStream(SerializationContext context, int length, bool needData)
        {
            //context.Zero(length);
            Stream source = context.Stream;
            byte[] destination = context.Workspace;
            int index = context.WorkspaceIndex;

            int b = source.ReadByte();

            if (b < 0)
            {
                if (needData) throw new SerializationException();
                return false;
            }
            destination[index + --length] = (byte)(b & 127);
            int prevMask = 1, prevShift = 7, mask = 126, shift = 1;

            // note that single-byte numbers can skip the base-128 step entirely
            while ((b & 128) != 0)
            {
                b = source.ReadByte();
                if (b < 0 || length < 0) throw new SerializationException();
                destination[index + length] |= (byte)((b & prevMask) << prevShift);
                if (length-- != 0)
                {
                    destination[index + length] = (byte)((b & mask) >> shift);
                }
                prevShift--;
                prevMask = (prevMask << 1) | 1;
                shift++;
                mask = mask & ~prevMask;
            }
            // clear any remaining bytes in the 
            while (length-- != 0)
            {
                destination[index++] = 0;
            }
            return true;
        }
        internal static int EncodeToWorkspace(byte[] source, SerializationContext context)
        {
            // note: no need to zero the buffer; we initialize
            // each byte we use and only return that number

            byte[] destination = context.Workspace;
            int index = context.WorkspaceIndex;

            // skip empty groups
            int start = 0, bufferLength = source.Length;
            while (start < bufferLength && source[start] == 0) start++;

            int mask = 127, shift = 0, nextMask = 128, nextShift = 7;
            int count = 0;
            destination[index] = 128;
            for (int i = bufferLength - 1; i >= start; i--)
            {
                // combine main block (note first zerod initially)
                destination[index + count++] |= (byte)((source[i] & mask) << shift);
                // assign overhang (zeros accordingly)
                destination[index + count] = (byte)(128 | ((source[i] & nextMask) >> nextShift));

                nextMask = (nextMask >> 1) | 128;
                nextShift--;
                mask >>= 1;
                shift++;
            }
            // remove msb from final block
            while (count > 0 && destination[index + count] == 128) count--;
            destination[index + count] &= 127;

            return count + 1;
        }

        internal static int ReadRaw(SerializationContext context)
        {
            int b, index = context.WorkspaceIndex, len = 0;
            Stream source = context.Stream;
            do
            {
                b = source.ReadByte();
                if (b < 0) throw new SerializationException();
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
                if (b < 0) throw new SerializationException();
            } while ((b & 128) != 0);
        }
    }
}
