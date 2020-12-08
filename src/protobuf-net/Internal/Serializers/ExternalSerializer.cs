using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;

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
            => Serializer is not IFactory<T> factory ? null : (object)factory.Create(context);

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
            // SerializerCache.Get<TProvider, T>().Write(ref state, value);
            // if TProvider is public, or
            // state.GetSerializer<T>().Write(ref state, value);
            using var loc = ctx.GetLocalWithValue(typeof(T), valueFrom);

            // get the serializer
            if (ctx.NonPublic || RuntimeTypeModel.IsFullyPublic(typeof(TProvider)))
            {
                ctx.EmitCall(typeof(SerializerCache).GetMethod(nameof(SerializerCache.Get)).MakeGenericMethod(typeof(TProvider), typeof(T)));
            }
            else
            {
                ctx.LoadState();
                ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.GetSerializer)).MakeGenericMethod(typeof(T)));
            }

            // invoke Write
            ctx.LoadState();
            ctx.LoadValue(loc); // value
            ctx.EmitCall(typeof(ISerializer<T>).GetMethod(nameof(ISerializer<T>.Write)));
        }

        void IRuntimeProtoSerializerNode.EmitRead(CompilerContext ctx, Local entity)
        {
            // entity = SerializerCache.Get<TProvider, T>().Read(ref state, value);
            // if TProvider is public, or
            // entity = state.GetSerializer<T>().Read(ref state, value);
            using var loc = ctx.GetLocalWithValue(typeof(T), entity);

            // get the serializer
            if (ctx.NonPublic || RuntimeTypeModel.IsFullyPublic(typeof(TProvider)))
            {
                ctx.EmitCall(typeof(SerializerCache).GetMethod(nameof(SerializerCache.Get)).MakeGenericMethod(typeof(TProvider), typeof(T)));
            }
            else
            {
                ctx.LoadState();
                ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.GetSerializer)).MakeGenericMethod(typeof(T)));
            }

            // invoke Read
            ctx.LoadState();
            ctx.LoadValue(loc);
            ctx.EmitCall(typeof(ISerializer<T>).GetMethod(nameof(ISerializer<T>.Read)));

            // store back
            ctx.StoreValue(entity);
        }
    }
}