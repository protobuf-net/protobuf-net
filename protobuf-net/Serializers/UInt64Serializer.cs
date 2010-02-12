using System;


namespace ProtoBuf.Serializers
{
    sealed class UInt64Serializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(ulong); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteUInt64((ulong)value);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteUInt64", typeof(ulong), valueFrom);
        }
#endif
    }
}
