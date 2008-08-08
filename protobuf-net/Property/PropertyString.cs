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
            int sLen = value.Length;
            if (sLen == 0)
            {
                int prefixLen = WritePrefix(context);
                context.WriteByte(0);
                return prefixLen + 1;
            }
            else if (sLen <= 127)
            {
                int prefixLen = WritePrefix(context),
                    byteCount = utf8.GetByteCount(value);

                context.CheckSpace(3 * sLen); // for utf8 encoding
                context.Workspace[0] = (byte)byteCount;
                utf8.GetBytes(value, 0, sLen, context.Workspace, 1);
                context.Write(++byteCount);
                return prefixLen + byteCount;
            }
            else
            {
                return WritePrefix(context)
                + context.WriteLengthPrefixed(value, value.Length, this);
            }
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
            context.Write(len);
            return len;
        }
    }
}
