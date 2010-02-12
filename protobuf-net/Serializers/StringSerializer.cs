using System;


namespace ProtoBuf.Serializers
{
    sealed class StringSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(string); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteString((string)value);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteString", typeof(string), valueFrom);
        }
#endif
    }
}
