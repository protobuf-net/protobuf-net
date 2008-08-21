using System;
using System.Reflection;
using System.Text;

namespace ProtoBuf.Property
{
    internal sealed class PropertyParseable<TSource, TValue> : Property<TSource, TValue>
    {
        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false, false);
        private static readonly Getter<string, TValue> parse;

        static PropertyParseable()
        {
            MethodInfo method = PropertyFactory.GetParseMethod(typeof(TValue));
            
#if CF2
            parse = delegate(string s) { return (TValue)method.Invoke(null, new object[] { s }); };
#else
            parse = (Getter<string, TValue>) Delegate.CreateDelegate(
                typeof(Getter<string, TValue>), null, method);
#endif
        }

        public override string DefinedType { get { return ProtoFormat.STRING; } }
        public override WireType WireType { get { return WireType.String; } }

        private Property<string, string> innerSerializer;
        protected override void OnBeforeInit(int tag, ref DataFormat format)
        {
            innerSerializer = PropertyFactory.CreatePassThru<string>(tag, ref format);
            base.OnBeforeInit(tag, ref format);
        }
        public override int Serialize(TSource source, SerializationContext context)
        {
            TValue value = GetValue(source);
            if (value == null) return 0;
            return innerSerializer.Serialize(value.ToString(), context);
        }

        public override TValue DeserializeImpl(TSource source, SerializationContext context)
        {
            string value = innerSerializer.DeserializeImpl(null, context);
            return value == null ? DefaultValue : parse(value);
        }
    }
}
