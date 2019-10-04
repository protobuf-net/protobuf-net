using System;
using ProtoBuf.Compiler;
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

    internal sealed class ExternalSerializer<TProvider, T> : CompiledSerializer, IExternalSerializer // just to prevent it constantly being checked for compilation
        where TProvider : class
    {
        object IExternalSerializer.Service => Serializer;
        private static ISerializer<T> Serializer => SerializerCache<TProvider, T>.InstanceField;
        public ExternalSerializer() : base(new DummySerializer()) { }

        public override object Read(ref ProtoReader.State state, object value)
            => Serializer.Read(ref state, (T)value);

        public override void Write(ref ProtoWriter.State state, object value)
            => Serializer.Write(ref state, (T)value);

        sealed class DummySerializer : IProtoTypeSerializer
        {
            public object Read(ref ProtoReader.State state, object value)
                => Serializer.Read(ref state, (T)value);

            public void Write(ref ProtoWriter.State state, object value)
                => Serializer.Write(ref state, (T)value);

            public Type BaseType => null;

            public bool ShouldEmitCreateInstance => false;

            public bool HasInheritance => false;

            public bool IsSubType => false;

            public SerializerFeatures Features => Serializer.Features;

            public Type ExpectedType => typeof(T);

            public bool RequiresOldValue => true;

            public bool ReturnsValue => true;

            public void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context) { }

            public bool CanCreateInstance() => Serializer is IFactory<T>;

            public object CreateInstance(ISerializationContext context)
            {
                if (Serializer is IFactory<T> factory) return factory.Create(context);
                return null;
            }

            public void EmitCallback(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType) { }

            public void EmitCreateInstance(CompilerContext ctx, bool callNoteObject = true) { }

            public void EmitRead(CompilerContext ctx, Local entity) { }

            public void EmitReadRoot(CompilerContext ctx, Local entity) { }

            public void EmitWrite(CompilerContext ctx, Local valueFrom) { }

            public void EmitWriteRoot(CompilerContext ctx, Local entity) { }

            public bool HasCallbacks(TypeModel.CallbackType callbackType) => false;
        }
    }
}
