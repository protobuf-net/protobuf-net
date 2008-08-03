
using System.Collections.Generic;
using System.IO;
namespace ProtoBuf
{
    internal sealed class EntitySerializer<TEntity> : ILengthSerializer<TEntity> where TEntity : class, new()
    {
        public string DefinedType { get { return Serializer.GetDefinedTypeName<TEntity>(); } }
        public bool CanBeGroup { get { return true; } }
        public WireType WireType { get { return WireType.String; } }

        public EntitySerializer()
        {
            Serializer<TEntity>.Build();
        }

        int ILengthSerializer<TEntity>.UnderestimateLength(TEntity value)
        {
            return 0; // single byte
        }

        /// <summary>
        /// Regular deserialization is length-prefixed
        /// </summary>
        public TEntity Deserialize(TEntity value, SerializationContext context)
        {
            if (value == null) value = new TEntity();
            Serializer<TEntity>.Deserialize(value, context);
            return value;
        }

        public int Serialize(TEntity value, SerializationContext context)
        {
            return Serializer<TEntity>.Serialize(value, context, null);
        }
    }
}
