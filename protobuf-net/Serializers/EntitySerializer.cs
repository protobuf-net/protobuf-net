
using System.Collections.Generic;
namespace ProtoBuf
{
    internal sealed class EntitySerializer<TEntity> : IGroupSerializer<TEntity> where TEntity : class, new()
    {
        public string DefinedType { get { return Serializer.GetDefinedTypeName<TEntity>(); } }
        
        public WireType WireType { get { return WireType.String; } }

        public EntitySerializer()
        {
            Serializer<TEntity>.Build();
        }

        /// <summary>
        /// Regular deserialization is length-prefixed
        /// </summary>
        public TEntity Deserialize(TEntity value, SerializationContext context)
        {
            if (value == null) value = new TEntity();

            int len = TwosComplementSerializer.ReadInt32(context);
            if (len > 0)
            {
                using (SubStream subStream = new SubStream(context.Stream, len, false))
                {
                    SerializationContext ctx = new SerializationContext(subStream);

                    // give our existing workspace to this sub-context
                    ctx.ReadFrom(context);
                    Serializer<TEntity>.Deserialize(value, ctx);

                    // grab the workspace back in case it was increased
                    context.ReadFrom(ctx);
                }
            }

            return value;
        }

        /// <summary>
        /// Group deserialization is group-terminated
        /// </summary>
        public TEntity DeserializeGroup(TEntity value, SerializationContext context)
        {
            // no need to sub-stream; group will indicate when complete
            if (value == null) value = new TEntity();
            Serializer<TEntity>.Deserialize(value, context);
            return value;
        }

        /// <summary>
        /// Regular serialization is length-prefixed
        /// </summary>
        public int Serialize(TEntity value, SerializationContext context)
        {
            if (value == null)
            {
                return TwosComplementSerializer.WriteToStream(0, context);
            }

            List<IProperty<TEntity>> candidateProperties = new List<IProperty<TEntity>>();
            int expectedLen = Serializer<TEntity>.GetLength(value, context, candidateProperties),
                preambleLen = TwosComplementSerializer.WriteToStream(expectedLen, context),
                actualLen = expectedLen == 0 ? 0 : Serializer<TEntity>.Serialize(value, context, candidateProperties.ToArray());

            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            return preambleLen + actualLen;
        }

        public int SerializeGroup(TEntity value, SerializationContext context)
        {
            return Serializer<TEntity>.Serialize(value, context, null);
        }

        /// <summary>
        /// Regular serialization is length-prefixed
        /// </summary>
        public int GetLength(TEntity value, SerializationContext context)
        {
            if (value == null) return 0;
            int len = Serializer<TEntity>.GetLength(value, context, null);
            if (len >= 0) len += TwosComplementSerializer.GetLength(len);
            return len;
        }

        public int GetLengthGroup(TEntity value, SerializationContext context)
        {
            return value == null ? 0 : Serializer<TEntity>.GetLength(value, context, null);
        }
    }
}
