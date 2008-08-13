using System.Reflection;
using System.Text;
using System;

namespace ProtoBuf.Property
{
    internal sealed class PropertyUri<TSource> : Property<TSource, Uri>
    {
        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false, false);

        public override string DefinedType { get { return ProtoFormat.STRING; } }
        public override WireType WireType { get { return WireType.String; } }

        private Property<string, string> innerSerializer;
        protected override void OnBeforeInit(int tag)
        {
            innerSerializer = PropertyFactory.CreatePassThru<string>(tag, DataFormat);
            base.OnBeforeInit(tag);
        }
        public override int Serialize(TSource source, SerializationContext context)
        {
            Uri value = GetValue(source);
            if (value == null || (IsOptional && value == DefaultValue)) return 0;
            return innerSerializer.Serialize(value.ToString(), context);
        }

        public override Uri DeserializeImpl(TSource source, SerializationContext context)
        {
            string value = innerSerializer.DeserializeImpl(null, context);
            return string.IsNullOrEmpty(value) ? null : new Uri(value);
        }
    }
}
