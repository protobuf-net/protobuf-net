#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class Int16Serializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(short); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteInt16((short)value);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadInt16();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteInt16", typeof(short), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadInt16", ExpectedType);
        }
#endif

    }
}
#endif