using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{

    internal sealed class SubTypeSerializer<TParent, TChild> : SubItemSerializer, IDirectWriteNode
        where TParent : class
        where TChild : class, TParent
    {
        public override bool IsSubType => true;

        public override Type ExpectedType => typeof(TChild);
        public override Type BaseType => typeof(TParent);

        public override void Write(ref ProtoWriter.State state, object value)
            => state.WriteSubType<TChild>((TChild)value);

        public override object Read(ref ProtoReader.State state, object value)
        {
            var ss = (SubTypeState<TParent>)value;
            ss.ReadSubType<TChild>(ref state);
            return ss;
        }

        public override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            // => ProtoWriter.WriteSubType<TChild>(value, writer, ref state, this);
            using var tmp = ctx.GetLocalWithValue(typeof(TChild), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(tmp);
            ctx.LoadSelfAsService<ISubTypeSerializer<TChild>, TChild>();
            ctx.EmitCall(s_WriteSubType[2].MakeGenericMethod(typeof(TChild)));
        }

        bool IDirectWriteNode.CanEmitDirectWrite(WireType wireType) => wireType == WireType.String;

        void IDirectWriteNode.EmitDirectWrite(int fieldNumber, WireType wireType, CompilerContext ctx, Local valueFrom)
        {
            using var tmp = ctx.GetLocalWithValue(typeof(TChild), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(fieldNumber);
            ctx.LoadValue(tmp);
            ctx.LoadSelfAsService<ISubTypeSerializer<TChild>, TChild>();
            ctx.EmitCall(s_WriteSubType[3].MakeGenericMethod(typeof(TChild)));
        }

        static readonly Dictionary<int, MethodInfo> s_WriteSubType =
            (from method in typeof(ProtoWriter.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
             where method.Name == nameof(ProtoWriter.State.WriteSubType) && method.IsGenericMethod
             select new { ArgCount = method.GetParameters().Length, Method = method }).ToDictionary(x => x.ArgCount, x => x.Method);


        public override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            // we expect the input here to be the SubTypeState<>
            // => state.ReadSubType<TActual>(ref state, serializer);
            var type = typeof(SubTypeState<TParent>);
            ctx.LoadAddress(valueFrom, type);
            ctx.LoadState();
            ctx.LoadSelfAsService<ISubTypeSerializer<TChild>, TChild>();
            ctx.EmitCall(type.GetMethod(nameof(SubTypeState<TParent>.ReadSubType)).MakeGenericMethod(typeof(TChild)));
        }
    }
    internal class SubValueSerializer<T> : SubItemSerializer, IDirectWriteNode
    {
        public override bool IsSubType => false;

        public override Type ExpectedType => typeof(T);


        public override void Write(ref ProtoWriter.State state, object value)
            => state.WriteMessage<T>((int)(default), TypeHelper<T>.FromObject(value));

        public override object Read(ref ProtoReader.State state, object value)
            => state.ReadMessage<T>(default, TypeHelper<T>.FromObject(value), null);

        public override void EmitWrite(CompilerContext ctx, Local valueFrom)
            => SubItemSerializer.EmitWriteMessage<T>(null, WireType.String, ctx, valueFrom);

        public override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            using var tmp = ctx.GetLocalWithValue(typeof(T), valueFrom);
            // and make sure we have a non-stack-based source
            SubItemSerializer.EmitReadMessage<T>(ctx, tmp);
        }

        bool IDirectWriteNode.CanEmitDirectWrite(WireType wireType)
            => wireType switch {
                WireType.String => true,
                WireType.StartGroup => true,
                _ => false
            };

        void IDirectWriteNode.EmitDirectWrite(int fieldNumber, WireType wireType, CompilerContext ctx, Local valueFrom)
            => SubItemSerializer.EmitWriteMessage<T>(fieldNumber, wireType, ctx, valueFrom);
    }


    internal abstract class SubItemSerializer : IProtoTypeSerializer
    {
        SerializerFeatures IProtoTypeSerializer.Features
        {
            get
            {
                ThrowHelper.ThrowNotImplementedException();
                return default;
            }
        }
        public abstract bool IsSubType { get; }
        public abstract Type ExpectedType { get; }
        public virtual Type BaseType => ExpectedType;
        bool IProtoTypeSerializer.HasInheritance => false;

        public abstract void Write(ref ProtoWriter.State state, object value);

        public abstract object Read(ref ProtoReader.State state, object value);

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

        object IProtoTypeSerializer.CreateInstance(ISerializationContext source)
        {
            return ((IProtoTypeSerializer)Proxy.Serializer).CreateInstance(source);
        }

        public static void EmitWriteMessage<T>(int? fieldNumber, WireType wireType, CompilerContext ctx, Local value = null,
            FieldInfo serializer = null, bool applyRecursionCheck = true)
        {
            using var tmp = ctx.GetLocalWithValue(typeof(T), value);
            ctx.LoadState();
            if (fieldNumber.HasValue) ctx.LoadValue(fieldNumber.Value);
            ctx.LoadValue((int)(applyRecursionCheck ? default : SerializerFeatures.OptionSkipRecursionCheck));
            ctx.LoadValue(tmp);
            if (serializer == null)
            {
                ctx.LoadSelfAsService<ISerializer<T>, T>();
            }
            else
            {
                ctx.LoadValue(serializer, checkAccessibility: false);
            }
            var methodFamily = wireType switch
            {
                WireType.StartGroup => s_WriteGroup,
                _ => s_WriteMessage,
            };
            ctx.EmitCall(methodFamily[fieldNumber.HasValue ? 4 : 3].MakeGenericMethod(typeof(T)));
        }

        public static void EmitReadMessage<T>(CompilerContext ctx, Local value = null, FieldInfo serializer = null)
        {
            // state.ReadMessage<T>(default, value, serializer);
            ctx.LoadState();
            ctx.LoadValue(0); // features
            if (value == null)
            {
                if (TypeHelper<T>.IsReferenceType)
                {
                    ctx.LoadNullRef();
                }
                else
                {
                    using var val = new Local(ctx, typeof(T));
                    ctx.InitLocal(typeof(T), val);
                    ctx.LoadValue(val);
                }
            }
            else
            {
                ctx.LoadValue(value);
            }
            if (serializer == null)
            {
                ctx.LoadSelfAsService<ISerializer<T>, T>();
            }
            else
            {
                ctx.LoadValue(serializer, checkAccessibility: false);
            }
            ctx.EmitCall(s_ReadMessage.MakeGenericMethod(typeof(T)));
        }

        private static readonly Dictionary<int, MethodInfo> s_WriteMessage =
            (from method in typeof(ProtoWriter.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
             where method.Name == nameof(ProtoWriter.State.WriteMessage)
                && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1
             select new { ArgCount = method.GetParameters().Length, Method = method }).ToDictionary(x => x.ArgCount, x => x.Method);

        private static readonly Dictionary<int, MethodInfo> s_WriteGroup =
            (from method in typeof(ProtoWriter.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
             where method.Name == nameof(ProtoWriter.State.WriteGroup)
                && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1
             select new { ArgCount = method.GetParameters().Length, Method = method }).ToDictionary(x => x.ArgCount, x => x.Method);

        private static readonly MethodInfo s_ReadMessage =
            (from method in typeof(ProtoReader.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
             where method.Name == nameof(ProtoReader.State.ReadMessage)
                && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 3
             select method).Single();

        protected ISerializerProxy Proxy { get; private set; }


        internal static IRuntimeProtoSerializerNode Create(Type type, ISerializerProxy proxy)
        {
            var obj = (SubItemSerializer)Activator.CreateInstance(typeof(SubValueSerializer<>).MakeGenericType(type), nonPublic: true);
            obj.Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            return (IRuntimeProtoSerializerNode)obj;
        }
        internal static IRuntimeProtoSerializerNode Create(Type actualType, ISerializerProxy proxy, Type parentType)
        {
            var obj = (SubItemSerializer)Activator.CreateInstance(typeof(SubTypeSerializer<,>).MakeGenericType(parentType, actualType), nonPublic: true);
            obj.Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            return (IRuntimeProtoSerializerNode)obj;
        }
    }
}