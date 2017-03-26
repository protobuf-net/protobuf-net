using System;
using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal sealed class CustomSerializer<T> : IProtoTypeSerializer
    {
        private ISerializer<T> serializer;
        private readonly Type serializerType;
        public CustomSerializer(Type type)
        {
            this.serializerType = type;
        }
#pragma warning disable 0618
        private ISerializer<T> GetSerializer(TypeModel model)
            => serializer ?? (serializer = (ISerializer<T>)model.GetSerializerInstance(serializerType));
#pragma warning restore 0618
        public Type ExpectedType => typeof(T);

        public bool RequiresOldValue => true;

        public bool ReturnsValue => true;

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local entity)
        {
            using (var loc = ctx.GetLocalWithValue(ExpectedType, entity))
            {
                ctx.LoadReaderWriter(); // reader
                ctx.EmitCall(typeof(ProtoReader).GetProperty(nameof(ProtoReader.Model)).GetGetMethod()); // model
                ctx.LoadValue(serializerType); // model, type
                ctx.EmitCall(typeof(TypeModel).GetMethod(nameof(TypeModel.GetSerializerInstance), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)); // object
                ctx.Cast(typeof(ISerializer<T>)); // serializer
                ctx.LoadReaderWriter(); // serializer, reader
                ctx.LoadAddress(loc, ExpectedType, evenForReferenceType: true); // serializer, reader, ref value
                ctx.EmitCall(typeof(ISerializer<T>).GetMethod("Read")); // void
                ctx.LoadValue(loc); // value
            }
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            using (var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                ctx.LoadReaderWriter(); // reader
                ctx.EmitCall(typeof(ProtoWriter).GetProperty(nameof(ProtoReader.Model)).GetGetMethod()); // model
                ctx.LoadValue(serializerType); // model, type
                ctx.EmitCall(typeof(TypeModel).GetMethod(nameof(TypeModel.GetSerializerInstance), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)); // object
                ctx.Cast(typeof(ISerializer<T>)); // serializer
                ctx.LoadReaderWriter(); // serializer, writer
                ctx.LoadAddress(loc, ExpectedType, evenForReferenceType: true); // serializer, writer, ref value
                ctx.EmitCall(typeof(ISerializer<T>).GetMethod("Write")); // void
            }
        }

        object IProtoSerializer.Read(object value, ProtoReader source)
        {
            T t = (T)value;
            GetSerializer(source.Model).Read(source, ref t);
            return t;
        }

        void IProtoSerializer.Write(object value, ProtoWriter dest)
        {
            T t = (T)value;
            GetSerializer(dest.Model).Write(dest, ref t);
        }

        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType) => false;

        bool IProtoTypeSerializer.CanCreateInstance() => false;

        object IProtoTypeSerializer.CreateInstance(ProtoReader source) => throw new NotSupportedException();

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
            => throw new NotSupportedException();
        void IProtoTypeSerializer.EmitCallback(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType)
            => throw new NotSupportedException();

        void IProtoTypeSerializer.EmitCreateInstance(CompilerContext ctx)
            => throw new NotSupportedException();
    }
}
