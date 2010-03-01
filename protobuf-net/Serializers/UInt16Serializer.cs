#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class UInt16Serializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(ushort); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteUInt16((ushort)value);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadUInt16();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteUInt16", typeof(ushort), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadUInt16", ExpectedType);
        }
#endif
    }
}
#endif