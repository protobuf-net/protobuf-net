using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class EnumTypeSerializer<T> : EnumMemberSerializer, IProtoTypeSerializer
        where T : struct
    {
        public EnumTypeSerializer(EnumMemberSerializer.EnumPair[] map) : base(typeof(T), map) { }

        public SerializerFeatures Features => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;

        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType) => false;

        bool IProtoTypeSerializer.CanCreateInstance() => true;

        object IProtoTypeSerializer.CreateInstance(ISerializationContext context) => default;

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context) { }

        void IProtoTypeSerializer.EmitCallback(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType)
            => ThrowHelper.ThrowNotSupportedException();

        void IProtoTypeSerializer.EmitCreateInstance(CompilerContext ctx, bool callNoteObject)
            => ThrowHelper.ThrowNotSupportedException();

        void IProtoTypeSerializer.EmitReadRoot(CompilerContext ctx, Local entity)
            => ThrowHelper.ThrowNotSupportedException();

        void IProtoTypeSerializer.EmitWriteRoot(CompilerContext ctx, Local entity)
            => ThrowHelper.ThrowNotSupportedException();

        Type IProtoTypeSerializer.BaseType => typeof(T);

        bool IProtoTypeSerializer.ShouldEmitCreateInstance => false;

        bool IProtoTypeSerializer.HasInheritance => false;

        bool IProtoTypeSerializer.IsSubType => false;
    }
}
