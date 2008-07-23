using System.Text;

namespace ProtoBuf
{
    internal sealed class StringSerializer : ISerializer<string>
    {
        private StringSerializer() { }
        public static readonly StringSerializer Default = new StringSerializer();
        public string DefinedType { get { return ProtoFormat.STRING; } }
        public WireType WireType { get { return WireType.String; } }
        public int Serialize(string value, SerializationContext context)
        {
            if (value == null) return 0;
            int preambleLen = TwosComplementSerializer.WriteToStream(value.Length, context);
            if (value.Length == 0) return preambleLen;

            // check buffer space
            int expectedLen = Encoding.UTF8.GetByteCount(value);
            context.CheckSpace(expectedLen);

            // do for real
            int actualLen = Encoding.UTF8.GetBytes(value, 0, value.Length, context.Workspace, context.WorkspaceIndex);
            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            return preambleLen + context.Write(actualLen);
        }

        public int GetLength(string value, SerializationContext context)
        {
            if (value == null) return 0;
            int preambleLen = TwosComplementSerializer.GetLength(value.Length);
            if (value.Length == 0) return preambleLen;
            return preambleLen + Encoding.UTF8.GetByteCount(value);
        }

        public string Deserialize(string value, SerializationContext context)
        {
            int len = TwosComplementSerializer.ReadInt32(context);
            if (len == 0) return "";

            context.CheckSpace(len);
            BlobSerializer.ReadBlock(context, len);
            return Encoding.UTF8.GetString(context.Workspace, context.WorkspaceIndex, len);
        }
    }
}
