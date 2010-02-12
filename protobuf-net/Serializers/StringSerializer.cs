using System;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    sealed class StringSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(string); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteString((string)value);
        }
        void IProtoSerializer.Write(CompilerContext ctx)
        {
            ctx.EmitWrite("WriteString", typeof(string));
        }
    }
}
