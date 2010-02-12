using System;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    interface IProtoSerializer
    {
        Type ExpectedType { get; }
        void Write(object value, ProtoWriter dest);
        void Write(CompilerContext ctx);
    }
}
