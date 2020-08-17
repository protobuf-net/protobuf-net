using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Linq;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
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

    internal sealed class ExternalSerializer<TProvider, T>
        : IRuntimeProtoSerializerNode, IExternalSerializer,
        ICompiledSerializer, // just to prevent it constantly being checked for compilation
        IProtoTypeSerializer // needed to make some internal bits happy
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

        SerializerFeatures IProtoTypeSerializer.Features => Serializer.Features;


        Type IProtoTypeSerializer.BaseType => ExpectedType;

        bool IProtoTypeSerializer.CanCreateInstance() => Serializer is IFactory<T>;

        object IProtoTypeSerializer.CreateInstance(ISerializationContext context)
        {
            return !(Serializer is IFactory<T> factory) ? null : (object)factory.Create(context);
        }
        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, ISerializationContext context)
        { }

        bool IProtoTypeSerializer.ShouldEmitCreateInstance => false;
        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType) => false;

        bool IProtoTypeSerializer.HasInheritance => false;
        bool IProtoTypeSerializer.IsSubType => false;

        void IProtoTypeSerializer.EmitCreateInstance(CompilerContext ctx, bool callNoteObject)
            => ThrowHelper.ThrowNotSupportedException();
        void IProtoTypeSerializer.EmitCallback(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType)
            => ThrowHelper.ThrowNotSupportedException();

        void IProtoTypeSerializer.EmitReadRoot(CompilerContext ctx, Local entity)
            => ThrowHelper.ThrowNotSupportedException();
        void IProtoTypeSerializer.EmitWriteRoot(CompilerContext ctx, Local entity)
            => ThrowHelper.ThrowNotSupportedException();

        void IRuntimeProtoSerializerNode.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            AssertCanEmit();
            using var loc = ctx.GetLocalWithValue(typeof(T), valueFrom);
            ctx.LoadState();
            ctx.LoadValue((int)Serializer.Features); // features
            ctx.LoadValue(loc); // value
            ctx.LoadNullRef(); // serializer
            ctx.EmitCall(typeof(ProtoWriter.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(m => m.Name == nameof(ProtoWriter.State.WriteMessage) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 3)
                .MakeGenericMethod(typeof(T)));
        }

        void IRuntimeProtoSerializerNode.EmitRead(CompilerContext ctx, Local entity)
        {
            AssertCanEmit();
            using var loc = ctx.GetLocalWithValue(typeof(T), entity);
            ctx.LoadState();
            ctx.LoadValue((int)Serializer.Features); // features
            ctx.LoadValue(loc); // value
            ctx.LoadNullRef(); // serializer
            ctx.EmitCall(typeof(ProtoReader.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(m => m.Name == nameof(ProtoReader.State.ReadMessage) && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 3)
                .MakeGenericMethod(typeof(T)));
            ctx.StoreValue(entity);
        }

        void AssertCanEmit()
        {
            if (Serializer.Features.GetCategory() != SerializerFeatures.CategoryMessage)
            {
                throw new NotSupportedException("Only message types are supported for Emit");
            }
        }
    }
}