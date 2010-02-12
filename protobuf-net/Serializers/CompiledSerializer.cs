#if !FX11
using System;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    sealed class CompiledSerializer : IProtoSerializer
    {
        public static CompiledSerializer Wrap(IProtoSerializer head)
        {
            return (head as CompiledSerializer) ?? new CompiledSerializer(head);
        }
        private readonly IProtoSerializer head; 
        private readonly ProtoSerializer serializer;
        private readonly ProtoDeserializer deserializer;

        private CompiledSerializer(IProtoSerializer head)
        {
            this.head = head;
            serializer = CompilerContext.BuildSerializer(head);
        }

        Type IProtoSerializer.ExpectedType
        {
            get { return head.ExpectedType; }
        }

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            serializer(value, dest);
        }

        void IProtoSerializer.Write(CompilerContext ctx)
        {
            head.Write(ctx);
        }
    }
}
#endif