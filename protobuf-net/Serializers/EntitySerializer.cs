
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
namespace ProtoBuf
{
    sealed class EntitySerializer<TEntity> : IGroupSerializer<TEntity> where TEntity : class, new()
    {
        public string DefinedType { get { return Serializer.GetDefinedTypeName<TEntity>(); } }
        public WireType WireType { get { return WireType.String; } }

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
            if (value == null) return 0;
            List<IProperty<TEntity>> candidateProperties = new List<IProperty<TEntity>>();
            int expectedLen = Serializer<TEntity>.GetLength(value, context, candidateProperties);
            int preambleLen = TwosComplementSerializer.WriteToStream(expectedLen, context);

            int actualLen = Serializer<TEntity>.Serialize(value, context, candidateProperties.ToArray());
            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            return preambleLen + actualLen;
        }
        /// <summary>
        /// Regular serialization is length-prefixed
        /// </summary>
        public int GetLength(TEntity value, SerializationContext context)
        {
            if (value == null) return 0;
            int len = Serializer<TEntity>.GetLength(value, context, null);
            return TwosComplementSerializer.GetLength(len) + len;
        }
    }
}
