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
        void EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject = true);
        bool ShouldEmitCreateInstance { get; }

        void EmitReadRoot(Compiler.CompilerContext ctx, Compiler.Local entity);
        void EmitWriteRoot(Compiler.CompilerContext ctx, Compiler.Local entity);

        bool HasInheritance { get; }
    }
}