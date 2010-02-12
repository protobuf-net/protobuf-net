using System;


namespace ProtoBuf.Serializers
{
    sealed class DoubleSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(double); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteDouble((double)value);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteDouble", typeof(double), valueFrom);
        }
#endif
    }
}
