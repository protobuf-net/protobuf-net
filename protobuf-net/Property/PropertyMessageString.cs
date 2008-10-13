
using System.IO;
namespace ProtoBuf.Property
{
    internal sealed class PropertyMessageString<TSource, TValueBase, TValueActual> : Property<TSource, TValueBase>, ILengthProperty<TValueActual>
        where TValueBase : class
        where TValueActual : class, TValueBase
    {

        public override System.Collections.Generic.IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield return CreateAlternative<PropertyMessageGroup<TSource, TValueBase, TValueActual>>(DataFormat.Group);
        }

        public override string DefinedType
        {
            get { return Serializer.GetDefinedTypeName<TValueBase>(); }
        }

        public override WireType WireType { get { return WireType.String; } }

        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            Serializer<TValueActual>.Build();
        }

        public override int Serialize(TSource source, SerializationContext context)
        {
            TValueActual value = (TValueActual) GetValue(source);
            if (value == null) return 0;

            return WritePrefix(context)
                + context.WriteLengthPrefixed(value, 0, this);
        }

        public override TValueBase DeserializeImpl(TSource source, SerializationContext context)
        {
            TValueActual value = Serializer<TValueBase>.CheckSubType<TValueActual>(GetValue(source));

            long restore = context.LimitByLengthPrefix();
            Serializer<TValueActual>.Deserialize(ref value, context);
            // restore the max-pos
            context.MaxReadPosition = restore;

            return value;
        }

        int ILengthProperty<TValueActual>.Serialize(TValueActual value, SerializationContext context)
        {
            return Serializer<TValueActual>.Serialize(value, context);
        }
    }
}
