using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ProtoBuf
{
    internal sealed class BlobSerializer : ISerializer<byte[]>, ILengthSerializer<byte[]>
    {
        private BlobSerializer() { }
        public bool CanBeGroup { get { return false; } }
        public static readonly BlobSerializer Default = new BlobSerializer();

        /*
        internal static void ReadBlock(SerializationContext context, int length)
        {
            ReadBlock(context.Stream, context.Workspace, 0, length);
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
        */
        public string DefinedType { get { return ProtoFormat.BYTES; } }
        public WireType WireType { get { return WireType.String; } }
        public int Serialize(byte[] value, SerializationContext context)
        {
            if (value == null || value.Length == 0) return 0;
            
            context.Write(value, 0, value.Length);

            return value.Length;
        }
        
        public byte[] Deserialize(byte[] value, SerializationContext context)
        {

            int len = (int)(context.MaxReadPosition - context.Position);
            if (value == null || value.Length != len)
            { // re-use the existing buffer if it is the same size
                value = new byte[len];
            }
            if (len > 0)
            {
                context.ReadBlock(value, len);
            }
            return value;
        }

        public int UnderestimateLength(byte[] value)
        {
            return value == null ? 0 : value.Length;
        }
    }
}
