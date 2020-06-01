using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Internal.Serializers
{
    internal interface IProtoTypeSerializer : IRuntimeProtoSerializerNode
    {
        Type BaseType { get; }
        bool HasCallbacks(TypeModel.CallbackType callbackType);
        bool CanCreateInstance();
        object CreateInstance(ISerializationContext context);
        void Callback(object value, TypeModel.CallbackType callbackType, ISerializationContext context);

        void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType);
        void EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject = true);
        bool ShouldEmitCreateInstance { get; }

        void EmitReadRoot(Compiler.CompilerContext ctx, Compiler.Local entity);
        void EmitWriteRoot(Compiler.CompilerContext ctx, Compiler.Local entity);

        bool HasInheritance { get; }

        bool IsSubType { get; }

        SerializerFeatures Features { get; }
    }
}