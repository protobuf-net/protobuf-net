using System;
using System.Diagnostics;


namespace ProtoBuf.Serializers
{
    sealed class DoubleSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(double); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteDouble((double)value);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Debug.Assert(value == null); // since replaces
            return source.ReadDouble();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteDouble", typeof(double), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadDouble", ExpectedType);
        }
#endif
    }
}
