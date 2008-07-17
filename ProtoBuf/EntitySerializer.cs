
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
namespace ProtoBuf
{
    sealed class EntitySerializer<TEntity> : ISerializer<TEntity> where TEntity : class, new()
    {
        public string DefinedType { get { return Serializer<TEntity>.GetDefinedTypeName(); } }
        public WireType WireType { get { return WireType.String; } }

        public TEntity Deserialize(TEntity value, SerializationContext context)
        {
            if (value == null) value = new TEntity();
            int len = Int32VariantSerializer.ReadFromStream(context);
            if (len > 0)
            { // TODO: remove the need for the mem-stream
                context.CheckSpace(len);
                BlobSerializer.ReadBlock(context, len);
                using (MemoryStream ms = new MemoryStream(context.Workspace, context.WorkspaceIndex, len))
                {
                    SerializationContext ctx = new SerializationContext(ms);
                    // give our existing workspace to this sub-context
                    ctx.ReadWorkspaceFrom(context);
                    Serializer<TEntity>.Deserialize(value, ctx);
                    // grab the workspace back in case it was increased
                    context.ReadWorkspaceFrom(ctx);
                }
            }
            return value;
        }
        public int Serialize(TEntity value, SerializationContext context)
        {
            if (value == null) return 0;
            var candidateProperties = new List<IProperty<TEntity>>();
            int expectedLen = Serializer<TEntity>.GetLength(value, context, candidateProperties);
            int preambleLen = Int32VariantSerializer.WriteToStream(expectedLen, context);

            int actualLen = Serializer<TEntity>.Serialize(value, context, candidateProperties);
            Serializer.VerifyBytesWritten(expectedLen, actualLen);
            return preambleLen + actualLen;
        }
        public int GetLength(TEntity value, SerializationContext context)
        {
            if (value == null) return 0;
            int len = Serializer<TEntity>.GetLength(value, context, null);
            return Int32VariantSerializer.GetLength(len) + len;
        }
    }
}
