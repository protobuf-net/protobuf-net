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
            if (value.Length == 0)
            {
                return TwosComplementSerializer.WriteToStream(0, context);
            }
            
            // check buffer space
            int expectedLen = Encoding.UTF8.GetByteCount(value);
            context.CheckSpace(expectedLen);

            // do for real
            int actualLen = Encoding.UTF8.GetBytes(value, 0, value.Length, context.Workspace, 0);
            int preambleLen = TwosComplementSerializer.WriteToStream(actualLen, context);

            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            context.Stream.Write(context.Workspace, 0, actualLen);
            return preambleLen + actualLen;
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
            return Encoding.UTF8.GetString(context.Workspace, 0, len);
        }
    }
}
