using System;
using ProtoBuf.Meta;
using System.Reflection;
using System.Linq;

namespace ProtoBuf.Serializers
{
    internal sealed class SubItemSerializer<TBase, TActual> : SubItemSerializer, IProtoTypeSerializer
        where TActual : TBase
    { 
        bool IProtoTypeSerializer.HasCallbacks(TypeModel.CallbackType callbackType)
        {
            return ((IProtoTypeSerializer)Proxy.Serializer).HasCallbacks(callbackType);
        }

        bool IProtoTypeSerializer.CanCreateInstance()
        {
            return ((IProtoTypeSerializer)Proxy.Serializer).CanCreateInstance();
        }

        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).EmitCallback(ctx, valueFrom, callbackType);
        }

        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).EmitCreateInstance(ctx);
        }

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).Callback(value, callbackType, context);
        }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source)
        {
            return ((IProtoTypeSerializer)Proxy.Serializer).CreateInstance(source);
        }

        Type IProtoSerializer.ExpectedType => typeof(TActual);
        Type IProtoTypeSerializer.BaseType => typeof(TBase);

        bool IProtoSerializer.RequiresOldValue => true;

        bool IProtoSerializer.ReturnsValue => true;

        private bool ApplyRecursionCheck => typeof(TBase) == typeof(TActual);

        void IProtoSerializer.Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteSubItem<TBase, TActual>((TActual)value, dest, ref state, null, ApplyRecursionCheck);
        }

        object IProtoSerializer.Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            return source.ReadSubItem<TBase, TActual>(ref state, (TBase)value, null);
        }
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom); // since we're consuming this first, we don't need to capture it
            if (typeof(TBase) != typeof(TActual)) ctx.Cast(typeof(TActual));
            ctx.LoadWriter(true);
            ctx.LoadNullRef();
            ctx.LoadValue(ApplyRecursionCheck);
            ctx.EmitCall(WriteSubItem.MakeGenericMethod(typeof(TBase), typeof(TActual)));
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local val = ctx.GetLocalWithValue(typeof(TBase), valueFrom))
            {
                ctx.LoadReader(true);
                ctx.LoadValue(val);
                ctx.LoadNullRef();
                ctx.EmitCall(ReadSubItem.MakeGenericMethod(typeof(TBase), typeof(TActual)));
            }
        }
    }


    internal abstract class SubItemSerializer
    {
        public static readonly MethodInfo WriteSubItem =
            (from method in typeof(ProtoWriter).GetMethods(BindingFlags.Static | BindingFlags.Public)
             where method.Name == nameof(ProtoWriter.WriteSubItem)
                && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 5
             select method).Single();

        public static readonly MethodInfo ReadSubItem =
            (from method in typeof(ProtoReader).GetMethods(BindingFlags.Instance | BindingFlags.Public)
             where method.Name == nameof(ProtoReader.ReadSubItem)
                && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 3
             select method).Single();

        protected ISerializerProxy Proxy { get; private set; }

        internal static IProtoSerializer Create(Type baseType, Type actualType, ISerializerProxy proxy)
        {
            var obj = (SubItemSerializer)Activator.CreateInstance(typeof(SubItemSerializer<,>).MakeGenericType(baseType, actualType));
            obj.Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            return (IProtoSerializer)obj;
        }
    }
}