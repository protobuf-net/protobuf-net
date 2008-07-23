using System;

namespace ProtoBuf
{
    internal sealed class NullableSerializer<T> : ISerializer<Nullable<T>> where T : struct
    {
        private readonly ISerializer<T> innerSerializer;
        public NullableSerializer(ISerializer<T> innerSerializer)
        {
            if (innerSerializer == null)
            {
                throw new ArgumentNullException("innerSerializer");
            }
            this.innerSerializer = innerSerializer;
        }

        public int GetLength(T? value, SerializationContext context)
        {
            return value.HasValue ?
                innerSerializer.GetLength(value.GetValueOrDefault(), context) : 0;
        }

        public int Serialize(T? value, SerializationContext context)
        {
            return value.HasValue ?
                innerSerializer.Serialize(value.GetValueOrDefault(), context) : 0;
        }

        public T? Deserialize(T? value, SerializationContext context)
        {
            return innerSerializer.Deserialize(value.GetValueOrDefault(), context);
        }

        public WireType WireType
        {
            get { return innerSerializer.WireType; }
        }

        public string DefinedType
        {
            get { return innerSerializer.DefinedType; }
        }
    }
}
