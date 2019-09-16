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

        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).EmitCreateInstance(ctx, callNoteObject);
        }
        bool IProtoTypeSerializer.ShouldEmitCreateInstance
            => ((IProtoTypeSerializer)Proxy.Serializer).ShouldEmitCreateInstance;

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            ((IProtoTypeSerializer)Proxy.Serializer).Callback(value, callbackType, context);
        }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source)
        {
            return ((IProtoTypeSerializer)Proxy.Serializer).CreateInstance(source);
        }

        Type IRuntimeProtoSerializerNode.ExpectedType => typeof(TActual);
        Type IProtoTypeSerializer.BaseType => typeof(TBase);

        bool IRuntimeProtoSerializerNode.RequiresOldValue => true;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        private bool ApplyRecursionCheck => typeof(TBase) == typeof(TActual);

        void IRuntimeProtoSerializerNode.Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteSubItem<TActual>((TActual)value, dest, ref state, null, ApplyRecursionCheck);
        }

        object IRuntimeProtoSerializerNode.Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            return source.ReadSubItem<TActual>(ref state, (TActual)value, null);
        }
        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom); // since we're consuming this first, we don't need to capture it
            if (typeof(TBase) != typeof(TActual)) ctx.Cast(typeof(TActual));
            SubItemSerializer.EmitWriteSubItem<TActual>(ctx, null, null, ApplyRecursionCheck);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local val = ctx.GetLocalWithValue(typeof(TBase), valueFrom))
            {
                SubItemSerializer.EmitReadSubItem<TActual>(ctx, val, null);
            }
        }
    }


    internal abstract class SubItemSerializer
    {
        public static void EmitWriteSubItem<T>(Compiler.CompilerContext ctx, Compiler.Local value = null, FieldInfo serializer = null, bool applyRecursionCheck = true)
        {
            ctx.LoadValue(value);
            ctx.LoadWriter(true);
            if (serializer == null)
            {
                ctx.LoadNullRef();
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

        internal static IRuntimeProtoSerializerNode Create(Type baseType, Type actualType, ISerializerProxy proxy)
        {
            var obj = (SubItemSerializer)Activator.CreateInstance(typeof(SubItemSerializer<,>).MakeGenericType(baseType, actualType));
            obj.Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            return (IRuntimeProtoSerializerNode)obj;
        }
    }
}