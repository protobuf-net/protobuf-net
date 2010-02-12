using System;


namespace ProtoBuf.Serializers
{
    sealed class SingleSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(float); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteSingle((float)value);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteSingle", typeof(float), valueFrom);
        }
#endif
    }
}
