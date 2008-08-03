using System.Text;

namespace ProtoBuf
{
    internal sealed class StringSerializer : ISerializer<string>, ILengthSerializer<string>
    {
        private StringSerializer() { }

        public bool CanBeGroup { get { return false; } }
        public static readonly StringSerializer Default = new StringSerializer();
        public string DefinedType { get { return ProtoFormat.STRING; } }
        public WireType WireType { get { return WireType.String; } }

        public static int Serialize(string value, SerializationContext context)
        {
            if (string.IsNullOrEmpty(value)) return 0;

            const int MAX_CHARS = 512;
            int charsRemaining = value.Length, charIndex = 0, totalLength = 0;

            context.CheckSpace(Encoding.UTF8.GetMaxByteCount(
                charsRemaining > MAX_CHARS ? MAX_CHARS : charsRemaining));

            while(charsRemaining > MAX_CHARS)
            {
                int len = Encoding.UTF8.GetBytes(value, charIndex, MAX_CHARS, context.Workspace, 0);
                context.Write(len);
                totalLength += len;
                charIndex += MAX_CHARS;
            }
            if(charsRemaining > 0)
            {
                int len = Encoding.UTF8.GetBytes(value, charIndex, charsRemaining, context.Workspace, 0);
                context.Write(len);
                totalLength += len;                
            }
            return totalLength;
        }
        int ISerializer<string>.Serialize(string value, SerializationContext context)
        {
            return Serialize(value, context);
        }
        public static string Deserialize(string value, SerializationContext context)
        {
            int len = (int)(context.MaxReadPosition - context.Position);
            if (len == 0) return "";

            context.CheckSpace(len);
            context.ReadBlock(len);
            return Encoding.UTF8.GetString(context.Workspace, 0, len);
        }
        string ISerializer<string>.Deserialize(string value, SerializationContext context)
        {
            return Deserialize(value, context);
        }

        public static int UnderestimateLength(string value)
        {
            return value == null ? 0 : value.Length;
        }
        int ILengthSerializer<string>.UnderestimateLength(string value)
        {
            return UnderestimateLength(value);
        }
    }
}
