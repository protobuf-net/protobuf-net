using System;


namespace ProtoBuf.Serializers
{
    sealed class Int64Serializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(long); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteInt64((long)value);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteInt64", typeof(long), valueFrom);
        }
#endif
    }
}
