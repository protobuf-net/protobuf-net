using System.Reflection;
using System.Text;

namespace ProtoBuf.Property
{
    internal sealed class PropertyString<TSource> : Property<TSource, string>, ILengthProperty<string>
    {
        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false, false);

        public override string DefinedType { get { return ProtoFormat.STRING; } }
        public override WireType WireType { get { return WireType.String; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            string value = GetValue(source);
            if (value == null || (IsOptional && value == DefaultValue)) return 0;

            int charCount = value.Length, underEstimate = charCount;
            if (charCount == 0)
            {
                int prefixLen = WritePrefix(context);
                context.WriteByte(0);
                return prefixLen + 1;
            }
            else if (charCount <= 42) {
                // guaranteed to have a byte length at most 127, so single byte;
                // any text up to 42 chars will take at most 126 bytes
                context.CheckSpace((3 * charCount) + 1);
                int prefixLen = WritePrefix(context),
                    byteCount = utf8.GetBytes(value, 0, charCount, context.Workspace, 1);
                context.Workspace[0] = (byte)byteCount;
                context.WriteBlock(context.Workspace, 0, ++byteCount);
                return prefixLen + byteCount;    

            } else if (charCount <= 127) {
                // common text in many locales will /tend/ to be single-byte. We'll
                // absorb the cost of checking the actual length, since we know it
                // is only a short string.
                underEstimate = utf8.GetByteCount(value);
                if(underEstimate <= 127) {
                    context.CheckSpace(underEstimate + 1);
                    int prefixLen = WritePrefix(context),
                    byteCount = utf8.GetBytes(value, 0, charCount, context.Workspace, 1);
                    context.Workspace[0] = (byte)byteCount;
                    context.WriteBlock(context.Workspace, 0, ++byteCount);
                    return prefixLen + byteCount;    
                }
                // note also that we update "underEstimate"; this means that even
                // if we find a 100-char string actually needs multiple bytes
                // (and so we'll use the callback below), we at least start the
                // callback with the correct length, avoiding the need to
                // encode it twice.
            }
            
            // when all else fails (longer strings), use a callback
            // to to a length prefix using our estimated length...
            return WritePrefix(context)
                + context.WriteLengthPrefixed(value, underEstimate, this);
        }

        public override string DeserializeImpl(TSource source, SerializationContext context)
        {
            int len = Base128Variant.DecodeInt32(context);
            string value;
            if (len == 0)
            {
                value = "";
            }
            else 
            {
                if (len > SerializationContext.InitialBufferLength) context.CheckSpace(len);
                context.ReadBlock(len);
                value = utf8.GetString(context.Workspace, 0, len);
            }
            return value;

        }
        int ILengthProperty<string>.Serialize(string value, SerializationContext context)
        {
            context.CheckSpace(Encoding.UTF8.GetMaxByteCount(value.Length));
            int len = utf8.GetBytes(value, 0, value.Length, context.Workspace, 0);
            context.WriteBlock(context.Workspace, 0, len);
            return len;
        }
    }
}
