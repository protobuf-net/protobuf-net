#if FEAT_COMPILER && !FX11
using System;



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
                Helpers.DebugAssert(((IProtoSerializer)result).ExpectedType == head.ExpectedType);
            }
            return result;
        }
        private readonly IProtoSerializer head;
        private readonly Compiler.ProtoSerializer serializer;
        private readonly Compiler.ProtoDeserializer deserializer;
        private CompiledSerializer(IProtoSerializer head)
        {
            this.head = head;
            serializer = Compiler.CompilerContext.BuildSerializer(head);
            deserializer = Compiler.CompilerContext.BuildDeserializer(head);
        }
        bool IProtoSerializer.RequiresOldValue { get { return head.RequiresOldValue; } }
        bool IProtoSerializer.ReturnsValue { get { return head.ReturnsValue; } }

        Type IProtoSerializer.ExpectedType { get { return head.ExpectedType; } }

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            serializer(value, dest);
        }
        object IProtoSerializer.Read(object value, ProtoReader source)
        {
            return deserializer(value, source);
        }

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            head.EmitWrite(ctx, valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            head.EmitRead(ctx, valueFrom);
        }
    }
}
#endif