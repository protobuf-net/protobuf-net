#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class SByteSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(sbyte); } }
        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteSByte((sbyte)value, dest);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return source.ReadSByte();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteSByte", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadSByte", ExpectedType);
        }
#endif

    }
}
#endif