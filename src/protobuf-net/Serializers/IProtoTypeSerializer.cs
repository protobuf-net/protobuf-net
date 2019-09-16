using ProtoBuf.Meta;
using System;

namespace ProtoBuf.Serializers
{
    internal interface IProtoTypeSerializer : IRuntimeProtoSerializerNode
    {
        Type BaseType { get; }
        bool HasCallbacks(TypeModel.CallbackType callbackType);
        bool CanCreateInstance();
        object CreateInstance(ProtoReader source);
        void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context);

        void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType);
        void EmitCreateInstance(Compiler.CompilerContext ctx);
    }
}