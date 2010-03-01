#if !NO_RUNTIME
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
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadUInt32();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteUInt32", typeof(uint), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadUInt32", ExpectedType);
        }
#endif
    }
}
#endif