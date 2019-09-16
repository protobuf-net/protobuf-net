using System;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal sealed class CompiledSerializer<T> : CompiledSerializer,
        IProtoSerializer<T>, IProtoDeserializer<T>
    {
        private readonly Compiler.ProtoSerializer<T> serializer;
        private readonly Compiler.ProtoDeserializer<T> deserializer;
        public CompiledSerializer(IProtoTypeSerializer head, RuntimeTypeModel model)
            : base(head)
        {
            try
            {
                serializer = Compiler.CompilerContext.BuildSerializer<T>(model.Scope, head, model);
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Unable to bind serializer: " + ex.Message, ex);
            }
            try
            {
                deserializer = Compiler.CompilerContext.BuildDeserializer<T>(model.Scope, head, model);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to bind deserializer: " + ex.Message, ex);
            }
        }

        T IProtoDeserializer<T>.Deserialize(ProtoReader reader, ref ProtoReader.State state, T value)
            => deserializer(reader, ref state, value);

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
            => deserializer(source, ref state, (T)value);

        void IProtoSerializer<T>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, T value)
            => serializer(writer, ref state, value);

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
            => serializer(dest, ref state, (T)value);
    }
    internal abstract class CompiledSerializer : IProtoTypeSerializer
    {
        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType)
        {
            return head.HasCallbacks(callbackType); // these routes only used when bits of the model not compiled
        }

        bool IProtoTypeSerializer.CanCreateInstance()
        {
            return head.CanCreateInstance();
        }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source)
        {
            return head.CreateInstance(source);
        }

        public void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            head.Callback(value, callbackType, context); // these routes only used when bits of the model not compiled
        }

        public static CompiledSerializer Wrap(IProtoTypeSerializer head, RuntimeTypeModel model)
        {
            if (!(head is CompiledSerializer result))
            {
                var ctor = Helpers.GetConstructor(typeof(CompiledSerializer<>).MakeGenericType(head.BaseType),
                    new Type[] { typeof(IProtoTypeSerializer), typeof(RuntimeTypeModel) }, true);

                try
                {
                    result = (CompiledSerializer)ctor.Invoke(new object[] { head, model });
                }
                catch (System.Reflection.TargetInvocationException tie)
                {
                    throw new InvalidOperationException($"Unable to wrap {head.BaseType.Name}/{head.ExpectedType.Name}: {tie.InnerException.Message}", tie.InnerException);
                }
                Helpers.DebugAssert(result.ExpectedType == head.ExpectedType);
            }
            return result;
        }

        private readonly IProtoTypeSerializer head;
        Type IProtoTypeSerializer.BaseType => head.BaseType;
        protected CompiledSerializer(IProtoTypeSerializer head)
        {
            this.head = head;
        }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => head.RequiresOldValue;

        bool IRuntimeProtoSerializerNode.ReturnsValue => head.ReturnsValue;

        public Type ExpectedType => head.ExpectedType;

        public abstract void Write(ProtoWriter dest, ref ProtoWriter.State state, object value);

        public abstract object Read(ProtoReader source, ref ProtoReader.State state, object value);

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            head.EmitWrite(ctx, valueFrom);
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            head.EmitRead(ctx, valueFrom);
        }

        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            head.EmitCallback(ctx, valueFrom, callbackType);
        }

        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx)
        {
            head.EmitCreateInstance(ctx);
        }
    }
}