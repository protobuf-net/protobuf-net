using System;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    sealed class Int32Serializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(int); } }
        public void Write(object value, ProtoWriter dest)
        {
            dest.WriteInt32((int)value);
        }
        void IProtoSerializer.Write(CompilerContext ctx)
        {
            ctx.EmitWrite("WriteInt32", typeof(int));
        }
    }
}
