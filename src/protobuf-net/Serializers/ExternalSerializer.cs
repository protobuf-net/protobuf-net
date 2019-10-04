using System;
using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal class ExternalSerializer
    {
        internal static IProtoTypeSerializer Create(Type target, Type serializer)
        {
            return (IProtoTypeSerializer)Activator.CreateInstance(
                typeof(ExternalSerializer<,>).MakeGenericType(serializer, target), nonPublic: true);
        }
    }

    interface IExternalSerializer
    {
        object Service { get; }
    }

    internal sealed class ExternalSerializer<TProvider, T> : IRuntimeProtoSerializerNode, IExternalSerializer, ICompiledSerializer // just to prevent it constantly being checked for compilation
        where TProvider : class
    {
        object IExternalSerializer.Service => Serializer;
        private static ISerializer<T> Serializer => SerializerCache<TProvider, T>.InstanceField;

        public Type ExpectedType => typeof(T);

        bool IRuntimeProtoSerializerNode.RequiresOldValue => true;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public ExternalSerializer() { }

        void IRuntimeProtoSerializerNode.Write(ref ProtoWriter.State state, object value)
            => Serializer.Write(ref state, TypeHelper<T>.FromObject(value));

        object IRuntimeProtoSerializerNode.Read(ref ProtoReader.State state, object value)
            => Serializer.Read(ref state, TypeHelper<T>.FromObject(value));

        void IRuntimeProtoSerializerNode.EmitWrite(CompilerContext ctx, Local valueFrom)
            => ThrowHelper.ThrowNotSupportedException();

        void IRuntimeProtoSerializerNode.EmitRead(CompilerContext ctx, Local entity)
            => ThrowHelper.ThrowNotSupportedException();
    }
}