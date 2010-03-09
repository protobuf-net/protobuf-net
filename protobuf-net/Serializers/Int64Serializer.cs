#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class Int64Serializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(long); } }
        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteInt64((long)value, dest);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadInt64();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteInt64", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadInt64", ExpectedType);
        }
#endif
    }
}
#endif