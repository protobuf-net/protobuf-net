using System;


namespace ProtoBuf.Serializers
{
    sealed class UInt32Serializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(uint); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteUInt32((uint)value);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteUInt32", typeof(uint), valueFrom);
        }
#endif
    }
}
