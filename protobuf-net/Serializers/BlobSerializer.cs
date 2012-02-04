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
            ProtoWriter.WriteBytes((byte[])value, dest);
        }
        public BlobSerializer(bool overwriteList)
        {
            this.overwriteList = overwriteList;
        }
        private readonly bool overwriteList;
        public object Read(object value, ProtoReader source)
        {
            return ProtoReader.AppendBytes(overwriteList ? null : (byte[])value, source);
        }
        bool IProtoSerializer.RequiresOldValue { get { return !overwriteList; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteBytes", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (overwriteList)
            {
                ctx.LoadNullRef();
            }
            else
            {
                ctx.LoadValue(valueFrom);
            }
            ctx.LoadReaderWriter();
            ctx.EmitCall(typeof(ProtoReader).GetMethod("AppendBytes"));
        }
#endif
    }
}
#endif