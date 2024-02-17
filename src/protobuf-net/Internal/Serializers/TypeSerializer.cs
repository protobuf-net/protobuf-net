using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace ProtoBuf.Internal.Serializers
{
    internal abstract class TypeSerializer
    {
        public static IProtoTypeSerializer Create(Type forType, int[] fieldNumbers, IRuntimeProtoSerializerNode[] serializers, MethodInfo[] baseCtorCallbacks, bool isRootType, bool useConstructor, bool assertKnownType, CallbackSet callbacks, Type constructType, MethodInfo factory, Type rootType, SerializerFeatures features)
        {
            var obj = (TypeSerializer)(rootType is not null
                ? Activator.CreateInstance(typeof(InheritanceTypeSerializer<,>).MakeGenericType(rootType, forType), nonPublic: true)
                : Activator.CreateInstance(typeof(TypeSerializer<>).MakeGenericType(forType), nonPublic: true));
            
            obj.Init(fieldNumbers, serializers, baseCtorCallbacks, isRootType, useConstructor, assertKnownType, callbacks, constructType, factory, features);
            return (IProtoTypeSerializer)obj;
        }
        abstract internal void Init(int[] fieldNumbers, IRuntimeProtoSerializerNode[] serializers, MethodInfo[] baseCtorCallbacks, bool isRootType, bool useConstructor, bool assertKnownType, CallbackSet callbacks, Type constructType, MethodInfo factory, SerializerFeatures features);
    }

    internal sealed class InheritanceTypeSerializer<TBase, T> : TypeSerializer<T>, ISubTypeSerializer<T>
        where TBase : class
        where T : class, TBase
    {
        public override bool HasInheritance => true;

        internal override Type BaseType => typeof(TBase);

        public override void Write(ref ProtoWriter.State state, T value)
            => state.WriteBaseType<TBase>(value);

        public override T Read(ref ProtoReader.State state, T value)
            => state.ReadBaseType<TBase, T>(value);

        T ISubTypeSerializer<T>.ReadSubType(ref ProtoReader.State state, SubTypeState<T> value)
        {
            value.OnBeforeDeserialize(_subTypeOnBeforeDeserialize);
            DeserializeBody(ref state, ref value, (ref SubTypeState<T> s) => s.Value, (ref SubTypeState<T> s, T v) => s.Value = v);
            var val = value.Value;
            Callback(ref val, TypeModel.CallbackType.AfterDeserialize, state.Context);
            return val;
        }

        void ISubTypeSerializer<T>.WriteSubType(ref ProtoWriter.State state, T value)
            => SerializeImpl(ref state, value);

        public override void EmitReadRoot(CompilerContext context, Local valueFrom)
        {   // => (T)((IProtoSubTypeSerializer<TBase>)this).ReadSubType(reader, ref state, SubTypeState<TBase>.Create<T>(state.Context, value));
            // or
            // => state.ReadBaseType<TBase, T>(value, this);
            if (context.IsService)
            {
                using var tmp = context.GetLocalWithValue(typeof(T), valueFrom);
                context.LoadSelfAsService<ISubTypeSerializer<TBase>, TBase>(default, default);
                context.LoadState();

                // sub-state
                context.LoadSerializationContext(typeof(ISerializationContext));
                context.LoadValue(tmp);
                context.EmitCall(typeof(SubTypeState<TBase>)
                    .GetMethod(nameof(SubTypeState<string>.Create), BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(typeof(T)));
                context.EmitCall(typeof(ISubTypeSerializer<TBase>)
                    .GetMethod(nameof(ISubTypeSerializer<string>.ReadSubType), BindingFlags.Public | BindingFlags.Instance));
                if (typeof(T) != typeof(TBase)) context.Cast(typeof(T));
            }
            else
            {
                context.LoadState();
                context.LoadValue(valueFrom);
                context.LoadSelfAsService<ISubTypeSerializer<TBase>, TBase>(default, default);
                context.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.ReadBaseType), BindingFlags.Public | BindingFlags.Instance)
                    .MakeGenericMethod(typeof(TBase), typeof(T)));
            }
        }
        public override void EmitWriteRoot(CompilerContext context, Local valueFrom)
        {   // => ((IProtoSubTypeSerializer<TBase>)this).WriteSubType(writer, ref state, value);
            // or
            // => ProtoWriter.WriteBaseType<TBase>(value, writer, ref state, this);

            using var tmp = context.GetLocalWithValue(typeof(T), valueFrom);
            if (context.IsService)
            {
                context.LoadSelfAsService<ISubTypeSerializer<TBase>, TBase>(default, default);
                context.LoadState();
                context.LoadValue(tmp);
                context.EmitCall(typeof(ISubTypeSerializer<TBase>)
                    .GetMethod(nameof(ISubTypeSerializer<string>.WriteSubType), BindingFlags.Public | BindingFlags.Instance));
            }
            else
            {
                context.LoadState();
                context.LoadValue(tmp);
                context.LoadSelfAsService<ISubTypeSerializer<TBase>, TBase>(default, default);
                context.EmitCall(typeof(ProtoWriter).GetMethod(nameof(ProtoWriter.State.WriteBaseType), BindingFlags.Public | BindingFlags.Instance)
                    .MakeGenericMethod(typeof(TBase)));
            }
        }

        public override bool IsSubType => true;
    }
    internal class TypeSerializer<T> : TypeSerializer, ISerializer<T>, IFactory<T>, IProtoTypeSerializer
    {
        bool IRuntimeProtoSerializerNode.IsScalar => false;
        public virtual bool HasInheritance => false;
        public virtual void EmitReadRoot(CompilerContext context, Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitRead(context, valueFrom);
        public virtual void EmitWriteRoot(CompilerContext context, Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitWrite(context, valueFrom);

        T IFactory<T>.Create(ISerializationContext context) => (T)CreateInstance(context);

        public virtual void Write(ref ProtoWriter.State state, T value)
            => SerializeImpl(ref state, value);

        public virtual T Read(ref ProtoReader.State state, T value)
        {
            value ??= (T)CreateInstance(state.Context);

            Callback(ref value, TypeModel.CallbackType.BeforeDeserialize, state.Context);
            DeserializeBody(ref state, ref value, (ref T o) => o, (ref T o, T v) => o = v);
            Callback(ref value, TypeModel.CallbackType.AfterDeserialize, state.Context);
            return value;
        }
        public virtual bool IsSubType => false;

        void IRuntimeProtoSerializerNode.Write(ref ProtoWriter.State state, object value)
            => Write(ref state, TypeHelper<T>.FromObject(value));

        object IRuntimeProtoSerializerNode.Read(ref ProtoReader.State state, object value)
            => Read(ref state, TypeHelper<T>.FromObject(value));

        public bool HasCallbacks(TypeModel.CallbackType callbackType)
        {
            if (!GetFlag(StateFlags.IsRootType)) return false;
            if (callbacks is not null && callbacks[callbackType] is not null) return true;
            for (int i = 0; i < serializers.Length; i++)
            {
                if (serializers[i].ExpectedType != ExpectedType && ((IProtoTypeSerializer)serializers[i]).HasCallbacks(callbackType)) return true;
            }
            return false;
        }

        private Type constructType;
        public Type ExpectedType => typeof(T);

        internal virtual Type BaseType => typeof(T);
        Type IProtoTypeSerializer.BaseType => BaseType;
        private IRuntimeProtoSerializerNode[] serializers;
        private int[] fieldNumbers;

        enum StateFlags
        {
            None = 0,
            IsRootType = 1 << 0,
            HasConstructor = 1 << 1,
            UseConstructor = 1 << 2,
            IsExtensible = 1 << 3,
            IsTypedExtensible = 1 << 4,
            AssertKnownType = 1 << 5,
        }
        private StateFlags flags;
        private void SetFlag(StateFlags flag, bool value)
        {
            if (value)
            {
                flags |= flag;
            }
            else
            {
                flags &= ~flag;
            }
        }
        private bool GetFlag(StateFlags flag)
            => (flags & flag) != 0;

        private CallbackSet callbacks;
        private MethodInfo[] baseCtorCallbacks;
        private MethodInfo factory;

        public SerializerFeatures Features { get; private set; }

        internal override void Init(int[] fieldNumbers, IRuntimeProtoSerializerNode[] serializers, MethodInfo[] baseCtorCallbacks,
            bool isRootType, bool useConstructor, bool assertKnownType,
            CallbackSet callbacks, Type constructType, MethodInfo factory, SerializerFeatures features)
        {
            Debug.Assert(fieldNumbers is not null);
            Debug.Assert(serializers is not null);
            Debug.Assert(fieldNumbers.Length == serializers.Length);

            Array.Sort(fieldNumbers, serializers);
            Features = features;
            bool hasSubTypes = false;
            var forType = ExpectedType;
            for (int i = 0; i < fieldNumbers.Length; i++)
            {
                if (i != 0 && fieldNumbers[i] == fieldNumbers[i - 1])
                {
                    throw new InvalidOperationException("Duplicate field-number detected; " +
                              fieldNumbers[i].ToString() + " on: " + forType.FullName);
                }
                if (!hasSubTypes && serializers[i].ExpectedType != forType)
                {
                    hasSubTypes = true;
                }
            }
            this.factory = factory;

            if (constructType is null)
            {
                constructType = forType;
            }
            else
            {
                if (!forType.IsAssignableFrom(constructType))
                {
                    throw new InvalidOperationException(forType.FullName + " cannot be assigned from " + constructType.FullName);
                }
            }
            this.constructType = constructType;
            this.serializers = serializers;
            this.fieldNumbers = fieldNumbers;
            this.callbacks = callbacks;
            SetFlag(StateFlags.IsRootType, isRootType);
            SetFlag(StateFlags.UseConstructor, useConstructor);
            SetFlag(StateFlags.AssertKnownType, assertKnownType);

            if (baseCtorCallbacks is not null)
            {
                foreach (var cb in baseCtorCallbacks)
                {
                    if (!cb.ReflectedType.IsAssignableFrom(forType))
                        throw new InvalidOperationException("Trying to assign incompatible callback to " + forType.FullName);
                }
                if (baseCtorCallbacks.Length == 0)
                    baseCtorCallbacks = null;
            }

            this.baseCtorCallbacks = baseCtorCallbacks;

            if (Nullable.GetUnderlyingType(forType) is not null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly - this is contextually fine
                throw new ArgumentException("Cannot create a TypeSerializer for nullable types", nameof(forType));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            bool isExtensible = TypeSerializerMethodCache.Type_IExtensible.IsAssignableFrom(forType);
            bool isTypedExtensible = TypeSerializerMethodCache.Type_ITypedIExtensible.IsAssignableFrom(forType);
            if (isExtensible || isTypedExtensible)
            {
                if (forType.IsValueType || ((!isRootType || hasSubTypes) && !isTypedExtensible))
                {
                    throw new NotSupportedException(nameof(IExtensible) + " is not supported in structs or classes with inheritance without " + nameof(ITypedExtensible));
                }
                SetFlag(StateFlags.IsExtensible, isExtensible);
                SetFlag(StateFlags.IsTypedExtensible, isTypedExtensible);
            }
            bool hasConstructor = !constructType.IsAbstract && Helpers.GetConstructor(constructType, Type.EmptyTypes, true) is not null;
            SetFlag(StateFlags.HasConstructor, hasConstructor);
            if (constructType != forType && useConstructor && !hasConstructor)
            {
                throw new ArgumentException("The supplied default implementation cannot be created: " + constructType.FullName, nameof(constructType));
            }

            if (HasInheritance && callbacks is not null)
            {
                _subTypeOnBeforeDeserialize = (val, ctx) =>
                {   // note: since this only applies when we have inheritance, we don't need to worry about
                    // unobserved side-effects to structs
                    Callback(ref val, TypeModel.CallbackType.BeforeDeserialize, ctx);
                };
            }
        }

        private bool CanHaveInheritance
        {
            get
            {
                return (ExpectedType.IsClass || ExpectedType.IsInterface) && !ExpectedType.IsSealed;
            }
        }

        bool IProtoTypeSerializer.CanCreateInstance() { return true; }

        object IProtoTypeSerializer.CreateInstance(ISerializationContext context) => CreateInstance(context);

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, ISerializationContext context)
        {
            if (GetFlag(StateFlags.IsRootType) && callbacks is not null)
                InvokeCallback(callbacks[callbackType], value, context);
        }
        public void Callback(ref T value, TypeModel.CallbackType callbackType, ISerializationContext context)
        {
            if (GetFlag(StateFlags.IsRootType) && callbacks is not null)
            {
                object boxed = value;
                InvokeCallback(callbacks[callbackType], boxed, context);
                value = (T)boxed; // make sure we reflect any changes re value-types
            }
            //IProtoTypeSerializer ser = (IProtoTypeSerializer)GetMoreSpecificSerializer(value);
            //if (ser is object) ser.Callback(value, callbackType, context);
        }
        private IRuntimeProtoSerializerNode GetMoreSpecificSerializer(object value)
        {
            if (!CanHaveInheritance) return null;
            Type actualType = value.GetType();
            if (actualType == ExpectedType) return null;

            for (int i = 0; i < serializers.Length; i++)
            {
                IRuntimeProtoSerializerNode ser = serializers[i];
                if (ser is IProtoTypeSerializer ts && ts.IsSubType && ser.ExpectedType.IsAssignableFrom(actualType))
                {
                    return ser;
                }
            }
            if (actualType == constructType) return null; // needs to be last in case the default concrete type is also a known sub-type
            if (GetFlag(StateFlags.AssertKnownType))
            {
                TypeModel.ThrowUnexpectedSubtype(ExpectedType, actualType); // might throw (if not a proxy)
            }
            return null;
        }

        protected void SerializeImpl(ref ProtoWriter.State state, T value)
        {
            Callback(ref value, TypeModel.CallbackType.BeforeSerialize, state.Context);

            // write inheritance first
            if (CanHaveInheritance)
            {
                IRuntimeProtoSerializerNode next = GetMoreSpecificSerializer(value);
                if (next is object) next.Write(ref state, value);
            }

            // write all actual fields
            //Debug.WriteLine(">> Writing fields for " + forType.FullName);
            for (int i = 0; i < serializers.Length; i++)
            {
                IRuntimeProtoSerializerNode ser = serializers[i];
                if (!(ser is IProtoTypeSerializer ts && ts.IsSubType))
                {
                    //Debug.WriteLine(": " + ser.ToString());
                    ser.Write(ref state, value);
                }
            }
            //Debug.WriteLine("<< Writing fields for " + forType.FullName);

            if (UseTypedExtensible)
            {
                state.AppendExtensionData((ITypedExtensible)value, ExpectedType);
            }
            else if (GetFlag(StateFlags.IsExtensible))
            {
                state.AppendExtensionData((IExtensible)value);
            }
            Callback(ref value, TypeModel.CallbackType.AfterSerialize, state.Context);
        }

        protected Action<T, ISerializationContext> _subTypeOnBeforeDeserialize;
        protected delegate T StateGetter<TState>(ref TState state);
        protected delegate void StateSetter<TState>(ref TState state, T value);
        protected void DeserializeBody<TState>(ref ProtoReader.State state, ref TState bodyState, StateGetter<TState> getter, StateSetter<TState> setter)
        {
            int fieldNumber, lastFieldNumber = 0, lastFieldIndex = 0;
            bool fieldHandled;

            //Debug.WriteLine(">> Reading fields for " + forType.FullName);
            while ((fieldNumber = state.ReadFieldHeader()) > 0)
            {
                fieldHandled = false;
                if (fieldNumber < lastFieldNumber)
                {
                    lastFieldNumber = lastFieldIndex = 0;
                }
                for (int i = lastFieldIndex; i < fieldNumbers.Length; i++)
                {
                    if (fieldNumbers[i] == fieldNumber)
                    {
                        IRuntimeProtoSerializerNode ser = serializers[i];
                        //Debug.WriteLine(": " + ser.ToString());
                        if (ser is IProtoTypeSerializer ts && ts.IsSubType)
                        {
                            // sub-types are implemented differently; pass the entire
                            // state through and unbox again to observe any changes
                            bodyState = (TState)ser.Read(ref state, bodyState);
                        }
                        else
                        {
                            var value = getter(ref bodyState);
                            object boxed = value;
                            object result = ser.Read(ref state, boxed);
                            if (ser.ReturnsValue)
                            {
                                setter(ref bodyState, (T)result);
                            }
                            else if (ExpectedType.IsValueType)
                            {   // make sure changes to structs are preserved
                                setter(ref bodyState, (T)boxed);
                            }
                        }

                        lastFieldIndex = i;
                        lastFieldNumber = fieldNumber;
                        fieldHandled = true;
                        break;
                    }
                }
                if (!fieldHandled)
                {
                    //Debug.WriteLine(": [" + fieldNumber + "] (unknown)");
                    if (UseTypedExtensible)
                    {
                        var val = getter(ref bodyState);
                        state.AppendExtensionData((ITypedExtensible)val, ExpectedType);
                    }
                    else if (GetFlag(StateFlags.IsExtensible))
                    {
                        var val = getter(ref bodyState);
                        state.AppendExtensionData((IExtensible)val);
                    }
                    else
                    {
                        state.SkipField();
                    }
                }
            }
        }

        private object InvokeCallback(MethodInfo method, object obj, ISerializationContext serializationContext)
        {
            object result = null;
            object[] args;
            if (method is not null)
            {   // pass in a streaming context if one is needed, else null
                bool handled;
                ParameterInfo[] parameters = method.GetParameters();
                switch (parameters.Length)
                {
                    case 0:
                        args = null;
                        handled = true;
                        break;
                    default:
                        args = new object[parameters.Length];
                        handled = true;
                        for (int i = 0; i < args.Length; i++)
                        {
                            object val;
                            Type paramType = parameters[i].ParameterType;
                            if (paramType == typeof(ISerializationContext)) { val = serializationContext; }
                            else if (paramType == typeof(SerializationContext)) { val = SerializationContext.AsSerializationContext(serializationContext); }
                            else if (paramType == typeof(StreamingContext)) { val = SerializationContext.AsStreamingContext(serializationContext); }
                            else if (paramType == typeof(Type)) { val = constructType; }
                            else
                            {
                                val = null;
                                handled = false;
                            }
                            args[i] = val;
                        }
                        break;
                }
                if (handled)
                {
                    result = method.Invoke(obj, args);
                }
                else
                {
                    throw Meta.CallbackSet.CreateInvalidCallbackSignature(method);
                }
            }
            return result;
        }
        private object CreateInstance(ISerializationContext context)
        {
            //Debug.WriteLine("* creating : " + forType.FullName);
            object obj;
            if (factory is not null)
            {
                obj = InvokeCallback(factory, null, context);
            }
            else if (GetFlag(StateFlags.UseConstructor))
            {
                if (!GetFlag(StateFlags.HasConstructor)) TypeModel.ThrowCannotCreateInstance(constructType);
                obj = Activator.CreateInstance(constructType, nonPublic: true);
            }
            else
            {
                obj = BclHelpers.GetUninitializedObject(constructType);
            }
#if FEAT_DYNAMIC_REF
            if (context is ProtoReader reader) ProtoReader.NoteObject(obj, reader);
#endif
            return obj;
        }

        bool IRuntimeProtoSerializerNode.RequiresOldValue { get { return true; } }
        bool IRuntimeProtoSerializerNode.ReturnsValue { get { return false; } } // updates field directly

        private void LoadFromState(CompilerContext ctx, Local value)
        {
            if (HasInheritance)
            {
                var stateType = typeof(SubTypeState<>).MakeGenericType(typeof(T));
                var stateProp = stateType.GetProperty(nameof(SubTypeState<string>.Value));
                ctx.LoadAddress(value, stateType);
                ctx.EmitCall(stateProp.GetGetMethod());
            }
            else
            {
                ctx.LoadValue(value);
            }
        }

        private void WriteToState(CompilerContext ctx, Local state, Local value, Type type)
        {
            if (HasInheritance)
            {
                var stateType = typeof(SubTypeState<>).MakeGenericType(typeof(T));
                var stateProp = stateType.GetProperty(nameof(SubTypeState<string>.Value));

                if (value is null)
                {
                    using var tmp = new Local(ctx, type);
                    ctx.LoadValue(value);
                    ctx.StoreValue(tmp);
                    ctx.LoadAddress(state, stateType);
                    ctx.LoadValue(tmp);
                    ctx.EmitCall(stateProp.GetSetMethod());
                }
                else
                {
                    ctx.LoadAddress(state, stateType);
                    ctx.LoadValue(value);
                    ctx.EmitCall(stateProp.GetSetMethod());
                }
            }
            else
            {
                ctx.LoadValue(value);
                ctx.StoreValue(state);
            }
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            using Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom);
            // pre-callbacks
            EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeSerialize);

            Compiler.CodeLabel startFields = ctx.DefineLabel();
            // inheritance
            if (CanHaveInheritance)
            {
                // if we expect sub-types: do if (IsSubType()) and a switch inside that eventually calls ThrowUnexpectedSubtype
                // otherwise, *just* call ThrowUnexpectedSubtype (it does the IsSubType test itself)
                if (serializers.Any(x => x is IProtoTypeSerializer pts && pts.IsSubType))
                {
                    ctx.LoadValue(loc);
                    ctx.EmitCall(typeof(TypeModel).GetMethod(nameof(TypeModel.IsSubType), BindingFlags.Static | BindingFlags.Public)
                        .MakeGenericMethod(typeof(T)));
                    ctx.BranchIfFalse(startFields, false);

                    for (int i = 0; i < serializers.Length; i++)
                    {
                        IRuntimeProtoSerializerNode ser = serializers[i];
                        Type serType = ser.ExpectedType;
                        if (ser is IProtoTypeSerializer ts && ts.IsSubType)
                        {
                            Compiler.CodeLabel nextTest = ctx.DefineLabel();
                            ctx.LoadValue(loc);
                            ctx.TryCast(serType);

                            using var typed = new Local(ctx, serType);
                            ctx.StoreValue(typed);

                            ctx.LoadValue(typed);
                            ctx.BranchIfFalse(nextTest, false);

                            if (serType.IsValueType)
                            {
                                ctx.LoadValue(loc);
                                ctx.CastFromObject(serType);
                                ser.EmitWrite(ctx, null);
                            }
                            else
                            {
                                ser.EmitWrite(ctx, typed);
                            }

                            ctx.Branch(startFields, false);
                            ctx.MarkLabel(nextTest);
                        }
                    }
                }

                if (GetFlag(StateFlags.AssertKnownType))
                {
                    MethodInfo method;
                    if (constructType is not null && constructType != ExpectedType)
                    {
                        method = TypeSerializerMethodCache.ThrowUnexpectedSubtype[2].MakeGenericMethod(ExpectedType, constructType);
                    }
                    else
                    {
                        method = TypeSerializerMethodCache.ThrowUnexpectedSubtype[1].MakeGenericMethod(ExpectedType);
                    }
                    ctx.LoadValue(loc);
                    ctx.EmitCall(method);
                }
            }
            // fields

            ctx.MarkLabel(startFields);
            for (int i = 0; i < serializers.Length; i++)
            {
                IRuntimeProtoSerializerNode ser = serializers[i];
                if (!(ser is IProtoTypeSerializer ts && ts.IsSubType))
                    ser.EmitWrite(ctx, loc);
            }

            // extension data
            if (UseTypedExtensible)
            {
                using var tmp = ctx.GetLocalWithValue(typeof(ITypedExtensible), loc);
                ctx.LoadState();
                ctx.LoadValue(tmp);
                ctx.LoadValue(ExpectedType);
                ctx.EmitCall(TypeSerializerMethodCache.Method_Write_AppendExtensionData_ITypedExtensible);
            }
            else if (GetFlag(StateFlags.IsExtensible))
            {
                using var tmp = ctx.GetLocalWithValue(typeof(IExtensible), loc);
                ctx.LoadState();
                ctx.LoadValue(tmp);
                ctx.EmitCall(TypeSerializerMethodCache.Method_Write_AppendExtensionData_IExtensible);
            }

            // post-callbacks
            EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.AfterSerialize);
        }

        private bool UseTypedExtensible => GetFlag(StateFlags.IsTypedExtensible) && (HasInheritance || !GetFlag(StateFlags.IsExtensible));

        private static void EmitInvokeCallback(Compiler.CompilerContext ctx, MethodInfo method, Type constructType, Type type, Local valueFrom)
        {
            if (method is not null)
            {
                if (method.IsStatic)
                {
                    // calling a static factory method
                    Debug.Assert(valueFrom is null);
                }
                else
                {
                    // here, we're calling a callback *on an instance*;
                    if (type.IsValueType)
                    {
                        Debug.Assert(valueFrom is not null); // can't do that for structs
                        ctx.LoadAddress(valueFrom, type);
                    }
                    else
                    {
                        ctx.LoadValue(valueFrom);
                    }
                    
                }

                ParameterInfo[] parameters = method.GetParameters();
                bool handled = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;
                    if (parameterType == typeof(ISerializationContext)
                        || parameterType == typeof(StreamingContext)
                        || parameterType == typeof(SerializationContext))
                    {
                        ctx.LoadSerializationContext(parameterType);
                    }
                    else if (parameterType == typeof(Type))
                    {
                        Type tmp = constructType ?? type;
                        ctx.LoadValue(tmp);
                    }
                    else
                    {
                        handled = false;
                    }
                }
                if (handled)
                {
                    ctx.EmitCall(method);
                    if (constructType is not null)
                    {
                        if (method.ReturnType == typeof(object))
                        {
                            ctx.CastFromObject(type);
                        }
                    }
                }
                else
                {
                    throw Meta.CallbackSet.CreateInvalidCallbackSignature(method);
                }
            }
        }

        private void EmitCallbackIfNeeded(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            Debug.Assert(valueFrom is not null);
            if (GetFlag(StateFlags.IsRootType) && ((IProtoTypeSerializer)this).HasCallbacks(callbackType))
            {
                if (HasInheritance && callbackType == TypeModel.CallbackType.BeforeDeserialize)
                {
                    ThrowHelper.ThrowInvalidOperationException("Should be using sub-type-state API");
                }
                else if (HasInheritance && callbackType == TypeModel.CallbackType.AfterDeserialize)
                {
                    LoadFromState(ctx, valueFrom);
                    ((IProtoTypeSerializer)this).EmitCallback(ctx, null, callbackType);
                }
                else
                {
                    ((IProtoTypeSerializer)this).EmitCallback(ctx, valueFrom, callbackType);
                }
            }
        }

        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            bool actuallyHasInheritance = false;
            if (CanHaveInheritance)
            {
                for (int i = 0; i < serializers.Length; i++)
                {
                    IRuntimeProtoSerializerNode ser = serializers[i];
                    if (ser.ExpectedType != ExpectedType && ((IProtoTypeSerializer)ser).HasCallbacks(callbackType))
                    {
                        actuallyHasInheritance = true;
                        break;
                    }
                }
            }

            Debug.Assert(((IProtoTypeSerializer)this).HasCallbacks(callbackType), "Shouldn't be calling this if there is nothing to do");
            MethodInfo method = callbacks?[callbackType];
            if (method is null && !actuallyHasInheritance)
            {
                return;
            }

            EmitInvokeCallback(ctx, method, null, ExpectedType, valueFrom);

            if (actuallyHasInheritance && BaseType != ExpectedType)
            {
                throw new NotSupportedException($"Currently, serializatation callbacks are limited to the base-type in a hierarchy, but {ExpectedType.NormalizeName()} defines callbacks; this may be resolved in later versions; it is recommended to make the serialization callbacks 'virtual' methods on {BaseType.NormalizeName()}; or for the best compatibility with other serializers (DataContractSerializer, etc) - make the callbacks non-virtual methods on {BaseType.NormalizeName()} that *call* protected virtual methods on {BaseType.NormalizeName()}");

                //Compiler.CodeLabel @break = ctx.DefineLabel();
                //for (int i = 0; i < serializers.Length; i++)
                //{
                //    IRuntimeProtoSerializerNode ser = serializers[i];
                //    IProtoTypeSerializer typeser;
                //    Type serType = ser.ExpectedType;
                //    if (serType != ExpectedType
                //        && (typeser = (IProtoTypeSerializer)ser).HasCallbacks(callbackType))
                //    {
                //        Compiler.CodeLabel ifMatch = ctx.DefineLabel(), nextTest = ctx.DefineLabel();
                //        ctx.CopyValue();
                //        ctx.TryCast(serType);
                //        ctx.CopyValue();
                //        ctx.BranchIfTrue(ifMatch, true);
                //        ctx.DiscardValue();
                //        ctx.Branch(nextTest, false);
                //        ctx.MarkLabel(ifMatch);
                //        typeser.EmitCallback(ctx, null, callbackType);
                //        ctx.Branch(@break, false);
                //        ctx.MarkLabel(nextTest);
                //    }
                //}
                //ctx.MarkLabel(@break);
                //ctx.DiscardValue();
            }
        }

        void IRuntimeProtoSerializerNode.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            Type inputType = HasInheritance ? typeof(SubTypeState<>).MakeGenericType(ExpectedType) : ExpectedType;
            Debug.Assert(valueFrom is not null);

            using Compiler.Local loc = ctx.GetLocalWithValue(inputType, valueFrom);
            using Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int));
            if (!ExpectedType.IsValueType && !HasInheritance)
            {   // we're writing a *basic* serializer for ref-type T; it could
                // be null
                EmitCreateIfNull(ctx, loc);
            }

            // pre-callbacks
            if (HasCallbacks(TypeModel.CallbackType.BeforeDeserialize))
            {
                if (HasInheritance)
                {
                    var method = callbacks?[TypeModel.CallbackType.BeforeDeserialize];
                    if (method is not null)
                    {
                        // subTypeState.OnBeforeDeserialize(callbackField);
                        ctx.LoadAddress(loc, inputType);
                        var callbackfield = ctx.Scope.DefineSubTypeStateCallbackField<T>(method);
                        ctx.LoadValue(callbackfield, checkAccessibility: false);
                        ctx.EmitCall(inputType.GetMethod(nameof(SubTypeState<string>.OnBeforeDeserialize)));
                    }
                }
                else
                {   // nice and simple; just call it
                    EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeDeserialize);
                }
            }

            Compiler.CodeLabel @continue = ctx.DefineLabel(), processField = ctx.DefineLabel();
            ctx.Branch(@continue, false);

            ctx.MarkLabel(processField);
            foreach (var group in BasicList.GetContiguousGroups(fieldNumbers, serializers))
            {
                Compiler.CodeLabel tryNextField = ctx.DefineLabel();
                int groupItemCount = group.Items.Count;
                if (groupItemCount == 1)
                {
                    // discreet group; use an equality test
                    ctx.LoadValue(fieldNumber);
                    ctx.LoadValue(group.First);
                    Compiler.CodeLabel processThisField = ctx.DefineLabel();
                    ctx.BranchIfEqual(processThisField, true);
                    ctx.Branch(tryNextField, false);
                    WriteFieldHandler(ctx, ExpectedType, loc, processThisField, @continue, group.Items[0]);
                }
                else
                {   // implement as a jump-table-based switch
                    ctx.LoadValue(fieldNumber);
                    ctx.LoadValue(group.First);
                    ctx.Subtract(); // jump-tables are zero-based
                    Compiler.CodeLabel[] jmp = new Compiler.CodeLabel[groupItemCount];
                    for (int i = 0; i < groupItemCount; i++)
                    {
                        jmp[i] = ctx.DefineLabel();
                    }
                    ctx.Switch(jmp);
                    // write the default...
                    ctx.Branch(tryNextField, false);
                    for (int i = 0; i < groupItemCount; i++)
                    {
                        WriteFieldHandler(ctx, ExpectedType, loc, jmp[i], @continue, group.Items[i]);
                    }
                }
                ctx.MarkLabel(tryNextField);
            }

            ctx.LoadState();
            if (UseTypedExtensible)
            {
                LoadFromState(ctx, loc);
                ctx.LoadValue(ExpectedType);
                ctx.EmitCall(TypeSerializerMethodCache.Method_Read_AppendExtensionData_ITypedExtensible);
            }
            else if (GetFlag(StateFlags.IsExtensible))
            {
                LoadFromState(ctx, loc);
                ctx.EmitCall(TypeSerializerMethodCache.Method_Read_AppendExtensionData_IExtensible);
            }
            else
            {
                ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.SkipField), Type.EmptyTypes));
            }
            ctx.MarkLabel(@continue);
            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadFieldHeader), typeof(int));
            ctx.CopyValue();
            ctx.StoreValue(fieldNumber);
            ctx.LoadValue(0);
            ctx.BranchIfGreater(processField, false);

            // post-callbacks
            if (HasCallbacks(TypeModel.CallbackType.AfterDeserialize))
            {
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.AfterDeserialize);
            }

            if (HasInheritance)
            {
                // in this scenario, before exiting, we'll leave the T on the stack
                LoadFromState(ctx, loc);
            }
            else if (valueFrom is not null && !loc.IsSame(valueFrom))
            {
                LoadFromState(ctx, loc);
                ctx.StoreValue(valueFrom);
            }
        }

        private void WriteFieldHandler(
#pragma warning disable IDE0060
            Compiler.CompilerContext ctx, Type expected, Compiler.Local loc,
#pragma warning restore IDE0060
            Compiler.CodeLabel handler, Compiler.CodeLabel @continue, IRuntimeProtoSerializerNode serializer)
        {
            ctx.MarkLabel(handler);

            //Type serType = serializer.ExpectedType;

            //if (serType == ExpectedType)
            //{
            //    if (canBeNull) EmitCreateIfNull(ctx, loc);
            //    serializer.EmitRead(ctx, loc);
            //}
            //else
            //{
            //    //RuntimeTypeModel rtm = (RuntimeTypeModel)ctx.Model;
            //    if (((IProtoTypeSerializer)serializer).CanCreateInstance())
            //    {
            //        Compiler.CodeLabel allDone = ctx.DefineLabel();

            //        ctx.LoadValue(loc);
            //        ctx.BranchIfFalse(allDone, false); // null is always ok

            //        ctx.LoadValue(loc);
            //        ctx.TryCast(serType);
            //        ctx.BranchIfTrue(allDone, false); // not null, but of the correct type

            //        // otherwise, need to convert it
            //        ctx.LoadReader(false);
            //        ctx.LoadValue(loc);
            //        ((IProtoTypeSerializer)serializer).EmitCreateInstance(ctx);

            //        ctx.EmitCall(typeof(ProtoReader).GetMethod("Merge",
            //            new[] { typeof(ProtoReader), typeof(object), typeof(object)}));
            //        ctx.Cast(expected);
            //        ctx.StoreValue(loc); // Merge always returns a value

            //        // nothing needs doing
            //        ctx.MarkLabel(allDone);
            //    }

            //    if (Helpers.IsValueType(serType))
            //    {
            //        Compiler.CodeLabel initValue = ctx.DefineLabel();
            //        Compiler.CodeLabel hasValue = ctx.DefineLabel();
            //        using (Compiler.Local emptyValue = new Compiler.Local(ctx, serType))
            //        {
            //            ctx.LoadValue(loc);
            //            ctx.BranchIfFalse(initValue, false);

            //            ctx.LoadValue(loc);
            //            ctx.CastFromObject(serType);
            //            ctx.Branch(hasValue, false);

            //            ctx.MarkLabel(initValue);
            //            ctx.InitLocal(serType, emptyValue);
            //            ctx.LoadValue(emptyValue);

            //            ctx.MarkLabel(hasValue);
            //        }
            //    }
            //    else
            //    {
            //        ctx.LoadValue(loc);
            //        ctx.Cast(serType);
            //    }

            //    serializer.EmitRead(ctx, null);
            //}

            bool isSubtype = false;
            if (HasInheritance)
            {
                if (serializer is IProtoTypeSerializer pts && pts.IsSubType)
                {
                    // special-cased; we don't access .Value here, but instead
                    // pass the state down
                    isSubtype = true;
                    serializer.EmitRead(ctx, loc);
                }
                else
                {
                    LoadFromState(ctx, loc);
                    serializer.EmitRead(ctx, null);
                }
            }
            else
            {
                serializer.EmitRead(ctx, loc);
            }

            if (!isSubtype && serializer.ReturnsValue) 
            {
                WriteToState(ctx, loc, null, serializer.ExpectedType);
            }

            //if (serType == ExpectedType)
            //{
            //    if (canBeNull) EmitCreateIfNull(ctx, loc);
            //    serializer.EmitRead(ctx, loc);
            //}

            //if (serializer.ReturnsValue)
            //{   // update the variable
            //    if (Helpers.IsValueType(serType))
            //    {
            //        // but box it first in case of value type
            //        ctx.CastToObject(serType);
            //    }
            //    ctx.StoreValue(loc);
            //}
            ctx.Branch(@continue, false); // "continue"
        }

        bool IProtoTypeSerializer.ShouldEmitCreateInstance
            => factory is not null || !GetFlag(StateFlags.UseConstructor);

        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject)
        {
            // different ways of creating a new instance
            if (factory is not null)
            {
                EmitInvokeCallback(ctx, factory, constructType, ExpectedType, null);
            }
            else if (!GetFlag(StateFlags.UseConstructor))
            {   // DataContractSerializer style
                ctx.LoadValue(constructType);
                ctx.EmitCall(typeof(BclHelpers).GetMethod(nameof(BclHelpers.GetUninitializedObject)));
                ctx.Cast(ExpectedType);
            }
            else if (constructType.IsClass && GetFlag(StateFlags.HasConstructor))
            {   // XmlSerializer style
                ctx.EmitCtor(constructType);
            }
            else
            {
                ctx.LoadValue(ExpectedType);
                ctx.LoadNullRef();
                ctx.EmitCall(typeof(TypeModel).GetMethod(nameof(TypeModel.ThrowCannotCreateInstance),
                    BindingFlags.Static | BindingFlags.Public));
                ctx.LoadNullRef();
                callNoteObject = false;
            }

            // at this point we have an ExpectedType on the stack

            if (callNoteObject || baseCtorCallbacks is not null)
            {
                // we're going to need it multiple times; use a local
                using var loc = new Local(ctx, ExpectedType);
                ctx.StoreValue(loc);

#if FEAT_DYNAMIC_REF
                if (callNoteObject)
                {
                    // track root object creation
                    ctx.LoadState();
                    ctx.LoadValue(loc);
                    ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.NoteObject)));
                }
#endif

                //if (baseCtorCallbacks is object)
                //{
                //    for (int i = 0; i < baseCtorCallbacks.Length; i++)
                //    {
                //        EmitInvokeCallback(ctx, baseCtorCallbacks[i], null, ExpectedType, loc);
                //    }
                //}

                ctx.LoadValue(loc);
            }
        }
        private void EmitCreateIfNull(Compiler.CompilerContext ctx, Compiler.Local storage)
        {
            Debug.Assert(storage is not null);
            if (!ExpectedType.IsValueType)
            {
                Compiler.CodeLabel afterNullCheck = ctx.DefineLabel();
                ctx.LoadValue(storage);
                ctx.BranchIfTrue(afterNullCheck, false);

                ((IProtoTypeSerializer)this).EmitCreateInstance(ctx);
                ctx.StoreValue(storage);

                //if (callbacks is object) EmitInvokeCallback(ctx, callbacks.BeforeDeserialize, null, ExpectedType, storage);
                ctx.MarkLabel(afterNullCheck);
            }
        }
    }

    internal static class TypeSerializerMethodCache
    {
        internal static readonly Type Type_IExtensible = typeof(IExtensible), Type_ITypedIExtensible = typeof(ITypedExtensible);
        internal static readonly MethodInfo
            Method_Write_AppendExtensionData_IExtensible = typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.AppendExtensionData), new[] { typeof(IExtensible) }),
            Method_Write_AppendExtensionData_ITypedExtensible = typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.AppendExtensionData), new[] { typeof(ITypedExtensible), typeof(Type) }),
            Method_Read_AppendExtensionData_IExtensible = typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.AppendExtensionData), new[] { typeof(IExtensible) }),
            Method_Read_AppendExtensionData_ITypedExtensible = typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.AppendExtensionData), new[] { typeof(ITypedExtensible), typeof(Type) });

        internal static readonly Dictionary<int,MethodInfo> ThrowUnexpectedSubtype
        = (from method in typeof(TypeModel).GetMethods(BindingFlags.Static | BindingFlags.Public)
                where method.Name == nameof(TypeModel.ThrowUnexpectedSubtype) && method.IsGenericMethodDefinition
                where method.GetParameters().Length == 1
                let args = method.GetGenericArguments()
                select new { Count = args.Length, Method = method }).ToDictionary(x => x.Count, x => x.Method);
    }
}