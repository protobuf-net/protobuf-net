using System;
using ProtoBuf.Meta;
using System.Reflection;
using System.Linq;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{

    internal sealed class SubTypeSerializer<TParent, TChild> : SubItemSerializer
        where TParent : class
        where TChild : class, TParent
    {
        public override bool IsSubType => true;

        public override Type ExpectedType => typeof(TChild);
        public override Type BaseType => typeof(TParent);

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
            => ProtoWriter.WriteSubType<TChild>((TChild)value, dest, ref state);

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            var ss = (SubTypeState<TParent>)value;
            ss.ReadSubType<TChild>(source, ref state);
            return ss;
        }

        public override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            // => ProtoWriter.WriteSubType<TChild>(value, writer, ref state, this);
            ctx.LoadValue(valueFrom);
            ctx.LoadWriter(true);
            ctx.LoadSelfAsService<IProtoSubTypeSerializer<TChild>>(assertImplemented: true);
            ctx.EmitCall(typeof(ProtoWriter).GetMethod(nameof(ProtoWriter.WriteSubType), BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(typeof(TChild)));
        }
        public override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            // we expect the input here to be the SubTypeState<>
            // => state.ReadSubType<TActual>(reader, ref state, serializer);
            var type = typeof(SubTypeState<TParent>);
            ctx.LoadAddress(valueFrom, type);
            ctx.LoadReader(true);
            ctx.LoadSelfAsService<IProtoSubTypeSerializer<TChild>>(assertImplemented: true);
            ctx.EmitCall(type.GetMethod(nameof(SubTypeState<TParent>.ReadSubType)).MakeGenericMethod(typeof(TChild)));
        }
    }
    internal class SubValueSerializer<T> : SubItemSerializer
    {
        public override bool IsSubType => false;

        public override Type ExpectedType => typeof(T);


        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
            => ProtoWriter.WriteSubItem<T>((T)value, dest, ref state);

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
            => source.ReadSubItem<T>(ref state, (T) value, null);

        public override void EmitWrite(CompilerContext ctx, Local valueFrom)
            => SubItemSerializer.EmitWriteSubItem<T>(ctx, valueFrom);

        public override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            using(var tmp = ctx.GetLocalWithValue(typeof(T), valueFrom))
            {   // value==null means something else to EmitReadSubItem, so: capture it
                // and make sure we have a non-stack-based source
                SubItemSerializer.EmitReadSubItem<T>(ctx, tmp);
            }
        }
    }


    internal abstract class SubItemSerializer : IProtoTypeSerializer
    {
        public abstract bool IsSubType { get; }
        public abstract Type ExpectedType { get; }
        public virtual Type BaseType => ExpectedType;
        bool IProtoTypeSerializer.HasInheritance => false;

        public abstract void Write(ProtoWriter dest, ref ProtoWriter.State state, object value);

        public abstract object Read(ProtoReader source, ref ProtoReader.State state, object value);

        public abstract void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
        public abstract void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom);

        void IProtoTypeSerializer.EmitReadRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitRead(ctx, valueFrom);

        void IProtoTypeSerializer.EmitWriteRoot(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitWrite(ctx, valueFrom);

        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType)
            => Proxy.Serializer is IProtoTypeSerializer pts && pts.HasCallbacks(callbackType);

        bool IProtoTypeSerializer.CanCreateInstance()
            => Proxy.Serializer is IProtoTypeSerializer pts && pts.CanCreateInstance();

        bool IProtoTypeSerializer.ShouldEmitCreateInstance
            => Proxy.Serializer is IProtoTypeSerializer pts && pts.ShouldEmitCreateInstance;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => true;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).EmitCallback(ctx, valueFrom, callbackType);
        }

        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).EmitCreateInstance(ctx, callNoteObject);
        }

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).Callback(value, callbackType, context);
        }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source)
        {
            return ((IProtoTypeSerializer)Proxy.Serializer).CreateInstance(source);
        }

        public static void EmitWriteSubItem<T>(Compiler.CompilerContext ctx, Compiler.Local value = null,
            FieldInfo serializer = null, bool applyRecursionCheck = true, bool assertImplemented = true)
        {
            ctx.LoadValue(value);
            ctx.LoadWriter(true);
            if (serializer == null)
            {
                ctx.LoadSelfAsService<IProtoSerializer<T>>(assertImplemented);
            }
            else
            {
                ctx.LoadValue(serializer, checkAccessibility: false);
            }
            ctx.LoadValue(applyRecursionCheck);
            ctx.EmitCall(s_WriteSubItem.MakeGenericMethod(typeof(T)));
        }

        public static void EmitReadSubItem<T>(Compiler.CompilerContext ctx, Compiler.Local value = null, FieldInfo serializer = null)
        {
            // reader.ReadSubItem<TBase, TActual>(ref state, value, serializer);
            ctx.LoadReader(true);
            if (value == null)
            {
                if (TypeHelper<T>.IsObjectType)
                {
                    ctx.LoadNullRef();
                }
                else
                {
                    using (var val = new Compiler.Local(ctx, typeof(T)))
                    {
                        ctx.InitLocal(typeof(T), val);
                        ctx.LoadValue(val);
                    }
                }
            }
            else
            {
                ctx.LoadValue(value);
            }
            if (serializer == null)
            {
                ctx.LoadNullRef();
            }
            else
            {
                ctx.LoadValue(serializer, checkAccessibility: false);
            }
            ctx.EmitCall(s_ReadSubItem.MakeGenericMethod(typeof(T)));
        }

        private static readonly MethodInfo s_WriteSubItem =
            (from method in typeof(ProtoWriter).GetMethods(BindingFlags.Static | BindingFlags.Public)
             where method.Name == nameof(ProtoWriter.WriteSubItem)
                && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 5
             select method).Single();

        private static readonly MethodInfo s_ReadSubItem =
            (from method in typeof(ProtoReader).GetMethods(BindingFlags.Instance | BindingFlags.Public)
             where method.Name == nameof(ProtoReader.ReadSubItem)
                && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 3
             select method).Single();

        protected ISerializerProxy Proxy { get; private set; }


        internal static IRuntimeProtoSerializerNode Create(Type type, ISerializerProxy proxy)
        {
            var obj = (SubItemSerializer)Activator.CreateInstance(typeof(SubValueSerializer<>).MakeGenericType(type));
            obj.Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            return (IRuntimeProtoSerializerNode)obj;
        }
        internal static IRuntimeProtoSerializerNode Create(Type actualType, ISerializerProxy proxy, Type parentType)
        {
            var obj = (SubItemSerializer)Activator.CreateInstance(typeof(SubTypeSerializer<,>).MakeGenericType(parentType, actualType));
            obj.Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            return (IRuntimeProtoSerializerNode)obj;
        }
    }
}