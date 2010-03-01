#if !NO_RUNTIME
using System;
#if FEAT_COMPILER
using System.Reflection.Emit;
#endif



namespace ProtoBuf.Serializers
{
    sealed class BooleanSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(bool); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteBoolean((bool)value);
        }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadBoolean();
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteBoolean", typeof(bool), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadBoolean", ExpectedType);
        }
#endif
    }
}
#endif