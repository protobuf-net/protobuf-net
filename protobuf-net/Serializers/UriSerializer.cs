using System;

namespace ProtoBuf
{
    internal sealed class UriSerializer : ISerializer<Uri>, ILengthSerializer<Uri>
    {
        private UriSerializer() { }
        public bool CanBeGroup { get { return false; } }
        public static UriSerializer Default = new UriSerializer();
        Uri ISerializer<Uri>.Deserialize(Uri value, SerializationContext context)
        {
            string uri = StringSerializer.Deserialize(null, context);
            return string.IsNullOrEmpty(uri) ? null : new Uri(uri);
        }

        int ISerializer<Uri>.Serialize(Uri value, SerializationContext context)
        {
            return value == null ? 0 : StringSerializer.Serialize(value.ToString(), context);
        }

        WireType ISerializer<Uri>.WireType
        {
            get { return WireType.String; }
        }

        string ISerializer<Uri>.DefinedType
        {
            get { return ProtoFormat.STRING; }
        }
        
        int ILengthSerializer<Uri>.UnderestimateLength(Uri value)
        {
            return value == null ? 0 : StringSerializer.UnderestimateLength(value.ToString());
        }
    }
}
