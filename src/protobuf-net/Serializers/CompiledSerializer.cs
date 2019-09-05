#if FEAT_COMPILER
using System;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal sealed class CompiledSerializer<T> : CompiledSerializer, IProtoSerializer<T>
    {
        private readonly Compiler.ProtoSerializer serializer;
        private readonly Compiler.ProtoDeserializer deserializer;
        public CompiledSerializer(IProtoTypeSerializer head, TypeModel model)
            : base(head)
        {
            serializer = Compiler.CompilerContext.BuildSerializer(head, model);
            deserializer = Compiler.CompilerContext.BuildDeserializer(head, model);
        }

        public void Deserialize(ProtoReader reader, ref ProtoReader.State state, ref T obj)
        {
            object o = obj;
            o = deserializer(reader, ref state, o);
            obj = (T)o;
        }

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
            => deserializer(source, ref state, value);

        public void Serialize(ProtoWriter writer, ref ProtoWriter.State state, ref T obj)
            => serializer(writer, ref state, obj);

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
            => serializer(dest, ref state, value);
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

        public static CompiledSerializer Wrap(IProtoTypeSerializer head, TypeModel model)
        {
            if (!(head is CompiledSerializer result))
            {
                var ctor = Helpers.GetConstructor(typeof(CompiledSerializer<>).MakeGenericType(head.ExpectedType),
                    new Type[] { typeof(IProtoTypeSerializer), typeof(TypeModel) }, true);

                try
                {
                    result = (CompiledSerializer)ctor.Invoke(new object[] { head, model });
                }
                catch (System.Reflection.TargetInvocationException tie)
                {
                    throw tie.InnerException;
                }
                Helpers.DebugAssert(result.ExpectedType == head.ExpectedType);
            }
            return result;
        }

        private readonly IProtoTypeSerializer head;

        protected CompiledSerializer(IProtoTypeSerializer head)
        {
            this.head = head;
        }

        bool IProtoSerializer.RequiresOldValue => head.RequiresOldValue;

        bool IProtoSerializer.ReturnsValue => head.ReturnsValue;

        public Type ExpectedType => head.ExpectedType;

        public abstract void Write(ProtoWriter dest, ref ProtoWriter.State state, object value);

        public abstract object Read(ProtoReader source, ref ProtoReader.State state, object value);

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            head.EmitWrite(ctx, valueFrom);
        }

        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
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
#endif