using System;
using System.Diagnostics;


namespace ProtoBuf.Serializers
{
    sealed class StringSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(string); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteString((string)value);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }

        public object Read(object value, ProtoReader source)
        {
            Debug.Assert(value == null); // since replaces
            return source.ReadString();
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite("WriteString", typeof(string), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadString", ExpectedType);
        }
#endif
    }
}
