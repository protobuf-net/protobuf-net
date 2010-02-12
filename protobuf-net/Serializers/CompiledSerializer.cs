#if FEAT_COMPILER && !FX11
using System;
using System.Diagnostics;


namespace ProtoBuf.Serializers
{
    sealed class CompiledSerializer : IProtoSerializer
    {
        public static CompiledSerializer Wrap(IProtoSerializer head)
        {
            CompiledSerializer result = head as CompiledSerializer;
            if (result == null)
            {
                result = new CompiledSerializer(head);
                Debug.Assert(((IProtoSerializer)result).ExpectedType == head.ExpectedType);
            }
            return result;
        }
        private readonly IProtoSerializer head;
        private readonly Compiler.ProtoSerializer serializer;
        private readonly Compiler.ProtoSerializer deserializer;

        private CompiledSerializer(IProtoSerializer head)
        {
            this.head = head;
            serializer = Compiler.CompilerContext.BuildSerializer(head);
        }

        Type IProtoSerializer.ExpectedType
        {
            get { return head.ExpectedType; }
        }

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            serializer(value, dest);
        }

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            head.EmitWrite(ctx, valueFrom);
        }
    }
}
#endif