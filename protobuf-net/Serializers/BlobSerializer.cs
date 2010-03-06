#if !NO_RUNTIME
using System;
#if FEAT_COMPILER
using System.Reflection.Emit;
#endif



namespace ProtoBuf.Serializers
{
    sealed class BlobSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(byte[]); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteBytes((byte[])value);
        }
        public object Read(object value, ProtoReader source)
        {
            return source.AppendBytes((byte[])value);
        }
        bool IProtoSerializer.RequiresOldValue { get { return true; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteBytes", typeof(byte[]), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadReaderWriter();
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(typeof(ProtoReader).GetMethod("AppendBytes"));
        }
#endif
    }
}
#endif