#if FEAT_COMPILER && !FX11
using System;
using ProtoBuf.Meta;



namespace ProtoBuf.Serializers
{
    sealed class CompiledSerializer : IProtoTypeSerializer
    {
        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType)
        {
            return head.HasCallbacks(callbackType); // these routes only used when bits of the model not compiled
        }
        public void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            head.Callback(value, callbackType, context); // these routes only used when bits of the model not compiled
        }
        public static CompiledSerializer Wrap(IProtoTypeSerializer head)
        {
            CompiledSerializer result = head as CompiledSerializer;
            if (result == null)
            {
                result = new CompiledSerializer(head);
                Helpers.DebugAssert(((IProtoTypeSerializer)result).ExpectedType == head.ExpectedType);
            }
            return result;
        }
        private readonly IProtoTypeSerializer head;
        private readonly Compiler.ProtoSerializer serializer;
        private readonly Compiler.ProtoDeserializer deserializer;
        private CompiledSerializer(IProtoTypeSerializer head)
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
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            head.EmitCallback(ctx, valueFrom, callbackType);
        }
    }
}
#endif