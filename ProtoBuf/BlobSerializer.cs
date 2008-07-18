using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.IO;

namespace ProtoBuf
{
    sealed class BlobSerializer : ISerializer<byte[]>
    {
        private BlobSerializer() { }
        public static readonly BlobSerializer Default = new BlobSerializer();
        internal static void ReadBlock(SerializationContext context, int length)
        {
            ReadBlock(context.Stream, context.Workspace, context.WorkspaceIndex, length);
        }
        private static void ReadBlock(Stream stream, byte[] buffer, int index, int length)
        {
            int read;
            while ((length > 0) && ((read = stream.Read(buffer, index, length)) > 0))
            {
                index += read;
                length -= read;
            }
            if (length != 0) throw new EndOfStreamException();
        }

        public string DefinedType { get { return ProtoFormat.BYTES; } }
        public WireType WireType { get { return WireType.String; } }
        public int Serialize(byte[] value, SerializationContext context)
        {
            if (value == null) return 0;
            int preambleLen = TwosComplementSerializer.WriteToStream(value.Length, context);
            if (value.Length > 0)
            {
                context.Stream.Write(value, 0, value.Length);
            }
            return preambleLen + value.Length;
        }
        public int GetLength(byte[] value, SerializationContext context)
        {
            if (value == null) return 0;
            return TwosComplementSerializer.GetLength(value.Length) + value.Length;
        }
        public byte[] Deserialize(byte[] value, SerializationContext context)
        {
            int len = TwosComplementSerializer.ReadInt32(context);
            if (value == null || value.Length != len)
            { // re-use the existing buffer if it is the same size
                value = new byte[len];
            }
            if (len > 0)
            {
                BlobSerializer.ReadBlock(context.Stream, value, 0, len);
            }
            return value;
        }

        public static void Reverse(byte[] buffer, int index, int length)
        {
            byte tmp;
            switch (length)
            { //unwrapped reverse
                case 0:
                case 1:
                    break;
                case 2:
                    tmp = buffer[index + 0];
                    buffer[index + 0] = buffer[index + 1];
                    buffer[index + 1] = tmp;
                    break;
                case 4:
                    tmp = buffer[index + 0];
                    buffer[index + 0] = buffer[index + 3];
                    buffer[index + 3] = tmp;
                    tmp = buffer[1];
                    buffer[index + 1] = buffer[index + 2];
                    buffer[index + 2] = tmp;
                    break;
                case 8:
                    tmp = buffer[index + 0];
                    buffer[index + 0] = buffer[index + 7];
                    buffer[index + 7] = tmp;
                    tmp = buffer[index + 1];
                    buffer[index + 1] = buffer[index + 6];
                    buffer[index + 6] = tmp;
                    tmp = buffer[index + 2];
                    buffer[index + 2] = buffer[index + 5];
                    buffer[index + 5] = tmp;
                    tmp = buffer[index + 3];
                    buffer[index + 3] = buffer[index + 4];
                    buffer[index + 4] = tmp;
                    break;
                default:
                    Array.Reverse(buffer, index, length);
                    break;
            }
        }
    }
}
