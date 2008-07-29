using System;

namespace ProtoBuf
{
    internal sealed class UriSerializer : ISerializer<Uri>
    {
        private UriSerializer() { }
        public static UriSerializer Default = new UriSerializer();
        Uri ISerializer<Uri>.Deserialize(Uri value, SerializationContext context)
        {
            string uri = StringSerializer.Deserialize(null, context);
            return string.IsNullOrEmpty(uri) ? null : new Uri(uri);
        }

        int ISerializer<Uri>.Serialize(Uri value, SerializationContext context)
        {
            if (value == null)
            {
                context.Stream.WriteByte(0);
                return 1;
            }
            return StringSerializer.Serialize(value.ToString(), context);
        }

        int ISerializer<Uri>.GetLength(Uri value, SerializationContext context)
        {
            return value == null ? 1 : StringSerializer.GetLength(value.ToString());
        }

        WireType ISerializer<Uri>.WireType
        {
            get { return WireType.String; }
        }

        string ISerializer<Uri>.DefinedType
        {
            get { return ProtoFormat.STRING; }
        }
    }
}
