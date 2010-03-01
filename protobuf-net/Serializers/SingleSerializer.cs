#if !NO_RUNTIME
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
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadSingle();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteSingle", typeof(float), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadSingle", ExpectedType);
        }
#endif
    }
}
#endif