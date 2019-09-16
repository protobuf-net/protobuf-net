using System;
using ProtoBuf.Meta;
using System.Reflection;
using ProtoBuf.Compiler;

namespace ProtoBuf.Serializers
{
    internal abstract class TypeSerializer
    {
        public static IProtoTypeSerializer Create(Type forType, int[] fieldNumbers, IRuntimeProtoSerializerNode[] serializers, MethodInfo[] baseCtorCallbacks, bool isRootType, bool useConstructor, CallbackSet callbacks, Type constructType, MethodInfo factory, Type rootType)
        {
            var obj = (TypeSerializer)(rootType != null
                ? Activator.CreateInstance(typeof(InheritanceTypeSerializer<,>).MakeGenericType(rootType, forType))
                : Activator.CreateInstance(typeof(TypeSerializer<>).MakeGenericType(forType)));
            
            obj.Init(fieldNumbers, serializers, baseCtorCallbacks, isRootType, useConstructor, callbacks, constructType, factory);
            return (IProtoTypeSerializer)obj;
        }
        abstract internal void Init(int[] fieldNumbers, IRuntimeProtoSerializerNode[] serializers, MethodInfo[] baseCtorCallbacks, bool isRootType, bool useConstructor, CallbackSet callbacks, Type constructType, MethodInfo factory);
    }

    internal sealed class InheritanceTypeSerializer<TBase, T> : TypeSerializer<T>, IProtoSubTypeSerializer<T>
        where TBase : class
        where T : class, TBase
    {
        public override bool HasInheritance => true;

        internal override Type BaseType => typeof(TBase);

        public override void Write(ProtoWriter writer, ref ProtoWriter.State state, T value)
            => ProtoWriter.WriteBaseType<TBase>(value, writer, ref state);

        public override T Read(ProtoReader reader, ref ProtoReader.State state, T value)
            => reader.ReadBaseType<TBase, T>(ref state, value);

        T IProtoSubTypeSerializer<T>.ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<T> value)
            => DeserializeImpl(reader, ref state, value);

        void IProtoSubTypeSerializer<T>.WriteSubType(ProtoWriter writer, ref ProtoWriter.State state, T value)
            => SerializeImpl(writer, ref state, value);

        public override void EmitReadRoot(CompilerContext context, Local valueFrom)
        {   // => reader.ReadBaseType<TBase, T>(ref state, value, this);
            context.LoadReader(true);
            context.LoadValue(valueFrom);
            context.LoadSelfAsService<IProtoSubTypeSerializer<TBase>>();
            context.EmitCall(typeof(ProtoReader).GetMethod(nameof(ProtoReader.ReadBaseType), BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(typeof(TBase), typeof(T)));
        }
        public override void EmitWriteRoot(CompilerContext context, Local valueFrom)
        {   // => ProtoWriter.WriteBaseType<TBase>(value, writer, ref state, this);
            context.LoadValue(valueFrom);
            context.LoadWriter(true);
            context.LoadSelfAsService<IProtoSubTypeSerializer<TBase>>();
            context.EmitCall(typeof(ProtoWriter).GetMethod(nameof(ProtoWriter.WriteBaseType), BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(typeof(TBase)));
        }
    }
    internal class TypeSerializer<T> : TypeSerializer, IProtoSerializer<T>, IProtoDeserializer<T>, IProtoFactory<T>, IProtoTypeSerializer
    {
        public virtual bool HasInheritance => false;
        public virtual void EmitReadRoot(CompilerContext context, Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitRead(context, valueFrom);
        public virtual void EmitWriteRoot(CompilerContext context, Local valueFrom)
            => ((IRuntimeProtoSerializerNode)this).EmitWrite(context, valueFrom);

        T IProtoFactory<T>.Create(ISerializationContext context) => (T)CreateInstance(context);

        public virtual void Write(ProtoWriter writer, ref ProtoWriter.State state, T value)
            => SerializeImpl(writer, ref state, value);

        public virtual T Read(ProtoReader reader, ref ProtoReader.State state, T value)
            => DeserializeImpl(reader, ref state, new SubTypeState<T>(reader, null, value));

        void IRuntimeProtoSerializerNode.Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
            => Write(dest, ref state, (T)value);

        object IRuntimeProtoSerializerNode.Read(ProtoReader source, ref ProtoReader.State state, object value)
            => Read(source, ref state, (T)value);

        public bool HasCallbacks(TypeModel.CallbackType callbackType)
        {
            if (callbacks != null && callbacks[callbackType] != null) return true;
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
        private bool isRootType, useConstructor, isExtensible, hasConstructor;
        private CallbackSet callbacks;
        private MethodInfo[] baseCtorCallbacks;
        private MethodInfo factory;

        internal override void Init(int[] fieldNumbers, IRuntimeProtoSerializerNode[] serializers, MethodInfo[] baseCtorCallbacks, bool isRootType, bool useConstructor, CallbackSet callbacks, Type constructType, MethodInfo factory)
        {
            Helpers.DebugAssert(fieldNumbers != null);
            Helpers.DebugAssert(serializers != null);
            Helpers.DebugAssert(fieldNumbers.Length == serializers.Length);

            Helpers.Sort(fieldNumbers, serializers);
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

            if (constructType == null)
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
            this.isRootType = isRootType;
            this.useConstructor = useConstructor;

            if (baseCtorCallbacks != null)
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

            if (Helpers.GetUnderlyingType(forType) != null)
            {
                throw new ArgumentException("Cannot create a TypeSerializer for nullable types", nameof(forType));
            }
            if (iextensible.IsAssignableFrom(forType))
            {
                if (forType.IsValueType || !isRootType || hasSubTypes)
                {
                    throw new NotSupportedException("IExtensible is not supported in structs or classes with inheritance");
                }
                isExtensible = true;
            }
            hasConstructor = !constructType.IsAbstract && Helpers.GetConstructor(constructType, Helpers.EmptyTypes, true) != null;
            if (constructType != forType && useConstructor && !hasConstructor)
            {
                throw new ArgumentException("The supplied default implementation cannot be created: " + constructType.FullName, nameof(constructType));
            }

            if (callbacks != null)
            {
                _onBeforeDeserialize = (val, ctx) => Callback(val, TypeModel.CallbackType.BeforeDeserialize, ctx.Context);
                _onAfterDeserialize = (val, ctx) => Callback(val, TypeModel.CallbackType.AfterDeserialize, ctx.Context);
            }
        }
        private static readonly System.Type iextensible = typeof(IExtensible);

        private bool CanHaveInheritance
        {
            get
            {
                return (ExpectedType.IsClass || ExpectedType.IsInterface) && !ExpectedType.IsSealed;
            }
        }

        bool IProtoTypeSerializer.CanCreateInstance() { return true; }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source) => CreateInstance(source);

        public void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            if (callbacks != null) InvokeCallback(callbacks[callbackType], value, context);
            IProtoTypeSerializer ser = (IProtoTypeSerializer)GetMoreSpecificSerializer(value);
            if (ser != null) ser.Callback(value, callbackType, context);
        }
        private IRuntimeProtoSerializerNode GetMoreSpecificSerializer(object value)
        {
            if (!CanHaveInheritance) return null;
            Type actualType = value.GetType();
            if (actualType == ExpectedType) return null;

            for (int i = 0; i < serializers.Length; i++)
            {
                IRuntimeProtoSerializerNode ser = serializers[i];
                if (ser.ExpectedType != ExpectedType && Helpers.IsAssignableFrom(ser.ExpectedType, actualType))
                {
                    return ser;
                }
            }
            if (actualType == constructType) return null; // needs to be last in case the default concrete type is also a known sub-type
            TypeModel.ThrowUnexpectedSubtype(ExpectedType, actualType); // might throw (if not a proxy)
            return null;
        }

        protected void SerializeImpl(ProtoWriter writer, ref ProtoWriter.State state, T value)
        {
            if (isRootType) Callback(value, TypeModel.CallbackType.BeforeSerialize, writer.Context);
            // write inheritance first
            if (CanHaveInheritance)
            {
                IRuntimeProtoSerializerNode next = GetMoreSpecificSerializer(value);
                if (next != null) next.Write(writer, ref state, value);
            }

            // write all actual fields
            //Helpers.DebugWriteLine(">> Writing fields for " + forType.FullName);
            for (int i = 0; i < serializers.Length; i++)
            {
                IRuntimeProtoSerializerNode ser = serializers[i];
                if (ser.ExpectedType == ExpectedType)
                {
                    //Helpers.DebugWriteLine(": " + ser.ToString());
                    ser.Write(writer, ref state, value);
                }
            }
            //Helpers.DebugWriteLine("<< Writing fields for " + forType.FullName);
            if (isExtensible) ProtoWriter.AppendExtensionData((IExtensible)value, writer, ref state);
            if (isRootType) Callback(value, TypeModel.CallbackType.AfterSerialize, writer.Context);
        }

        private Action<T, ISerializationContext> _onBeforeDeserialize, _onAfterDeserialize;
        protected T DeserializeImpl(ProtoReader source, ref ProtoReader.State state, SubTypeState<T> valueSource)
        {
            valueSource.OnBeforeDeserialize(_onBeforeDeserialize);

            int fieldNumber, lastFieldNumber = 0, lastFieldIndex = 0;
            bool fieldHandled;

            //Helpers.DebugWriteLine(">> Reading fields for " + forType.FullName);
            while ((fieldNumber = source.ReadFieldHeader(ref state)) > 0)
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
                        //Helpers.DebugWriteLine(": " + ser.ToString());
                        Type serType = ser.ExpectedType;
                        if (!valueSource.HasValue)
                        {
                            if (serType == ExpectedType) valueSource.CreateIfNeeded();
                        }
                        else
                        {
                            if (serType != ExpectedType && ((IProtoTypeSerializer)ser).CanCreateInstance()
                                && serType
                                .IsSubclassOf(valueSource.RawValue.GetType()))
                            {
                                valueSource.Value = (T)ProtoReader.Merge(source, valueSource.RawValue, ((IProtoTypeSerializer)ser).CreateInstance(source));
                            }
                        }

                        if (ser.ReturnsValue)
                        {
                            valueSource.Value = (T)ser.Read(source, ref state, valueSource.Value);
                        }
                        else
                        { // pop
                            ser.Read(source, ref state, valueSource.Value);
                        }

                        lastFieldIndex = i;
                        lastFieldNumber = fieldNumber;
                        fieldHandled = true;
                        break;
                    }
                }
                if (!fieldHandled)
                {
                    //Helpers.DebugWriteLine(": [" + fieldNumber + "] (unknown)");
                    if (isExtensible)
                    {
                        source.AppendExtensionData(ref state, (IExtensible)valueSource.Value);
                    }
                    else
                    {
                        source.SkipField(ref state);
                    }
                }
            }
            //Helpers.DebugWriteLine("<< Reading fields for " + forType.FullName);
            return valueSource.OnAfterDeserialize(_onAfterDeserialize);
        }

        private object InvokeCallback(MethodInfo method, object obj, SerializationContext context)
        {
            object result = null;
            object[] args;
            if (method != null)
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
                            if (paramType == typeof(SerializationContext)) { val = context; }
                            else if (paramType == typeof(System.Type)) { val = constructType; }
                            else if (paramType == typeof(System.Runtime.Serialization.StreamingContext)) { val = (System.Runtime.Serialization.StreamingContext)context; }
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
            //Helpers.DebugWriteLine("* creating : " + forType.FullName);
            object obj;
            if (factory != null)
            {
                obj = InvokeCallback(factory, null, context.Context);
            }
            else if (useConstructor)
            {
                if (!hasConstructor) TypeModel.ThrowCannotCreateInstance(constructType);
                obj = Activator.CreateInstance(constructType, nonPublic: true);
            }
            else
            {
                obj = BclHelpers.GetUninitializedObject(constructType);
            }
            if (context is ProtoReader reader) ProtoReader.NoteObject(obj, reader);
            if (baseCtorCallbacks != null)
            {
                for (int i = 0; i < baseCtorCallbacks.Length; i++)
                {
                    InvokeCallback(baseCtorCallbacks[i], obj, context.Context);
                }
            }
            return obj;
        }

        bool IRuntimeProtoSerializerNode.RequiresOldValue { get { return true; } }
        bool IRuntimeProtoSerializerNode.ReturnsValue { get { return false; } } // updates field directly

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            {
                // pre-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeSerialize);

                Compiler.CodeLabel startFields = ctx.DefineLabel();
                // inheritance
                if (CanHaveInheritance)
                {
                    for (int i = 0; i < serializers.Length; i++)
                    {
                        IRuntimeProtoSerializerNode ser = serializers[i];
                        Type serType = ser.ExpectedType;
                        if (serType != ExpectedType)
                        {
                            Compiler.CodeLabel ifMatch = ctx.DefineLabel(), nextTest = ctx.DefineLabel();
                            ctx.LoadValue(loc);
                            ctx.TryCast(serType);
                            ctx.CopyValue();
                            ctx.BranchIfTrue(ifMatch, true);
                            ctx.DiscardValue();
                            ctx.Branch(nextTest, true);
                            ctx.MarkLabel(ifMatch);
                            if (Helpers.IsValueType(serType))
                            {
                                ctx.DiscardValue();
                                ctx.LoadValue(loc);
                                ctx.CastFromObject(serType);
                            }
                            ser.EmitWrite(ctx, null);
                            ctx.Branch(startFields, false);
                            ctx.MarkLabel(nextTest);
                        }
                    }

                    if (constructType != null && constructType != ExpectedType)
                    {
                        using (Compiler.Local actualType = new Compiler.Local(ctx, typeof(Type)))
                        {
                            // would have jumped to "fields" if an expected sub-type, so two options:
                            // a: *exactly* that type, b: an *unexpected* type
                            ctx.LoadValue(loc);
                            ctx.EmitCall(typeof(object).GetMethod("GetType"));
                            ctx.CopyValue();
                            ctx.StoreValue(actualType);
                            ctx.LoadValue(ExpectedType);
                            ctx.BranchIfEqual(startFields, true);

                            ctx.LoadValue(actualType);
                            ctx.LoadValue(constructType);
                            ctx.BranchIfEqual(startFields, true);
                        }
                    }
                    else
                    {
                        // would have jumped to "fields" if an expected sub-type, so two options:
                        // a: *exactly* that type, b: an *unexpected* type
                        ctx.LoadValue(loc);
                        ctx.EmitCall(typeof(object).GetMethod("GetType"));
                        ctx.LoadValue(ExpectedType);
                        ctx.BranchIfEqual(startFields, true);
                    }
                    // unexpected, then... note that this *might* be a proxy, which
                    // is handled by ThrowUnexpectedSubtype
                    ctx.LoadValue(ExpectedType);
                    ctx.LoadValue(loc);
                    ctx.EmitCall(typeof(object).GetMethod("GetType"));
                    ctx.EmitCall(typeof(TypeModel).GetMethod("ThrowUnexpectedSubtype",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                        null, new Type[] { typeof(Type), typeof(Type) },null));
                }
                // fields

                ctx.MarkLabel(startFields);
                for (int i = 0; i < serializers.Length; i++)
                {
                    IRuntimeProtoSerializerNode ser = serializers[i];
                    if (ser.ExpectedType == ExpectedType) ser.EmitWrite(ctx, loc);
                }

                // extension data
                if (isExtensible)
                {
                    ctx.LoadValue(loc);
                    ctx.LoadWriter(true);
                    ctx.EmitCall(Compiler.WriterUtil.GetStaticMethod("AppendExtensionData", this));
                }
                // post-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.AfterSerialize);
            }
        }
        private static void EmitInvokeCallback(Compiler.CompilerContext ctx, MethodInfo method, bool copyValue, Type constructType, Type type)
        {
            if (method != null)
            {
                if (copyValue) ctx.CopyValue(); // assumes the target is on the stack, and that we want to *retain* it on the stack
                ParameterInfo[] parameters = method.GetParameters();
                bool handled = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;
                    if (parameterType == typeof(SerializationContext))
                    {
                        ctx.LoadSerializationContext();
                    }
                    else if (parameterType == typeof(Type))
                    {
                        Type tmp = constructType ?? type;
                        ctx.LoadValue(tmp);
                    }
                    else if (parameterType == typeof(System.Runtime.Serialization.StreamingContext))
                    {
                        ctx.LoadSerializationContext();
                        MethodInfo op = typeof(SerializationContext).GetMethod("op_Implicit", new Type[] { typeof(SerializationContext) });
                        if (op != null)
                        { // it isn't always! (framework versions, etc)
                            ctx.EmitCall(op);
                            handled = true;
                        }
                    }
                    else
                    {
                        handled = false;
                    }
                }
                if (handled)
                {
                    ctx.EmitCall(method);
                    if (constructType != null)
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
            Helpers.DebugAssert(valueFrom != null);
            if (isRootType && ((IProtoTypeSerializer)this).HasCallbacks(callbackType))
            {
                ((IProtoTypeSerializer)this).EmitCallback(ctx, valueFrom, callbackType);
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
                    }
                }
            }

            Helpers.DebugAssert(((IProtoTypeSerializer)this).HasCallbacks(callbackType), "Shouldn't be calling this if there is nothing to do");
            MethodInfo method = callbacks?[callbackType];
            if (method == null && !actuallyHasInheritance)
            {
                return;
            }
            ctx.LoadAddress(valueFrom, ExpectedType);
            EmitInvokeCallback(ctx, method, actuallyHasInheritance, null, ExpectedType);

            if (actuallyHasInheritance)
            {
                Compiler.CodeLabel @break = ctx.DefineLabel();
                for (int i = 0; i < serializers.Length; i++)
                {
                    IRuntimeProtoSerializerNode ser = serializers[i];
                    IProtoTypeSerializer typeser;
                    Type serType = ser.ExpectedType;
                    if (serType != ExpectedType
                        && (typeser = (IProtoTypeSerializer)ser).HasCallbacks(callbackType))
                    {
                        Compiler.CodeLabel ifMatch = ctx.DefineLabel(), nextTest = ctx.DefineLabel();
                        ctx.CopyValue();
                        ctx.TryCast(serType);
                        ctx.CopyValue();
                        ctx.BranchIfTrue(ifMatch, true);
                        ctx.DiscardValue();
                        ctx.Branch(nextTest, false);
                        ctx.MarkLabel(ifMatch);
                        typeser.EmitCallback(ctx, null, callbackType);
                        ctx.Branch(@break, false);
                        ctx.MarkLabel(nextTest);
                    }
                }
                ctx.MarkLabel(@break);
                ctx.DiscardValue();
            }
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            Helpers.DebugAssert(valueFrom != null);

            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int)))
            {
                // pre-callbacks
                if (HasCallbacks(TypeModel.CallbackType.BeforeDeserialize))
                {
                    if (Helpers.IsValueType(ExpectedType))
                    {
                        EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeDeserialize);
                    }
                    else
                    { // could be null
                        Compiler.CodeLabel callbacksDone = ctx.DefineLabel();
                        ctx.LoadValue(loc);
                        ctx.BranchIfFalse(callbacksDone, false);
                        EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeDeserialize);
                        ctx.MarkLabel(callbacksDone);
                    }
                }

                Compiler.CodeLabel @continue = ctx.DefineLabel(), processField = ctx.DefineLabel();
                ctx.Branch(@continue, false);

                ctx.MarkLabel(processField);
                foreach (BasicList.Group group in BasicList.GetContiguousGroups(fieldNumbers, serializers))
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
                        WriteFieldHandler(ctx, expected, loc, processThisField, @continue, (IRuntimeProtoSerializerNode)group.Items[0]);
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
                            WriteFieldHandler(ctx, expected, loc, jmp[i], @continue, (IRuntimeProtoSerializerNode)group.Items[i]);
                        }
                    }
                    ctx.MarkLabel(tryNextField);
                }

                EmitCreateIfNull(ctx, loc);
                ctx.LoadReader(true);
                if (isExtensible)
                {
                    ctx.LoadValue(loc);
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("AppendExtensionData",
                        new[] { Compiler.ReaderUtil.ByRefStateType, typeof(IExtensible) }));
                }
                else
                {
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("SkipField", Compiler.ReaderUtil.StateTypeArray));
                }
                ctx.MarkLabel(@continue);
                ctx.EmitBasicRead("ReadFieldHeader", typeof(int));
                ctx.CopyValue();
                ctx.StoreValue(fieldNumber);
                ctx.LoadValue(0);
                ctx.BranchIfGreater(processField, false);

                EmitCreateIfNull(ctx, loc);
                // post-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.AfterDeserialize);

                if (valueFrom != null && !loc.IsSame(valueFrom))
                {
                    ctx.LoadValue(loc);
                    ctx.Cast(valueFrom.Type);
                    ctx.StoreValue(valueFrom);
                }
            }
        }

        private void WriteFieldHandler(
            Compiler.CompilerContext ctx, Type expected, Compiler.Local loc,
            Compiler.CodeLabel handler, Compiler.CodeLabel @continue, IRuntimeProtoSerializerNode serializer)
        {
            ctx.MarkLabel(handler);
            Type serType = serializer.ExpectedType;
            if (serType == ExpectedType)
            {
                EmitCreateIfNull(ctx, loc);
                serializer.EmitRead(ctx, loc);
            }
            else
            {
                //RuntimeTypeModel rtm = (RuntimeTypeModel)ctx.Model;
                if (((IProtoTypeSerializer)serializer).CanCreateInstance())
                {
                    Compiler.CodeLabel allDone = ctx.DefineLabel();

                    ctx.LoadValue(loc);
                    ctx.BranchIfFalse(allDone, false); // null is always ok

                    ctx.LoadValue(loc);
                    ctx.TryCast(serType);
                    ctx.BranchIfTrue(allDone, false); // not null, but of the correct type

                    // otherwise, need to convert it
                    ctx.LoadReader(false);
                    ctx.LoadValue(loc);
                    ((IProtoTypeSerializer)serializer).EmitCreateInstance(ctx);

                    ctx.EmitCall(typeof(ProtoReader).GetMethod("Merge",
                        new[] { typeof(ProtoReader), typeof(object), typeof(object)}));
                    ctx.Cast(expected);
                    ctx.StoreValue(loc); // Merge always returns a value

                    // nothing needs doing
                    ctx.MarkLabel(allDone);
                }

                if (Helpers.IsValueType(serType))
                {
                    Compiler.CodeLabel initValue = ctx.DefineLabel();
                    Compiler.CodeLabel hasValue = ctx.DefineLabel();
                    using (Compiler.Local emptyValue = new Compiler.Local(ctx, serType))
                    {
                        ctx.LoadValue(loc);
                        ctx.BranchIfFalse(initValue, false);

                        ctx.LoadValue(loc);
                        ctx.CastFromObject(serType);
                        ctx.Branch(hasValue, false);

                        ctx.MarkLabel(initValue);
                        ctx.InitLocal(serType, emptyValue);
                        ctx.LoadValue(emptyValue);

                        ctx.MarkLabel(hasValue);
                    }
                }
                else
                {
                    ctx.LoadValue(loc);
                    ctx.Cast(serType);
                }

                serializer.EmitRead(ctx, null);
            }

            if (serializer.ReturnsValue)
            {   // update the variable
                if (Helpers.IsValueType(serType))
                {
                    // but box it first in case of value type
                    ctx.CastToObject(serType);
                }
                ctx.StoreValue(loc);
            }
            ctx.Branch(@continue, false); // "continue"
        }

        bool IProtoTypeSerializer.ShouldEmitCreateInstance
            => factory != null || !useConstructor;

        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx, bool callNoteObject)
        {
            // different ways of creating a new instance
            if (factory != null)
            {
                EmitInvokeCallback(ctx, factory, false, constructType, ExpectedType);
            }
            else if (!useConstructor)
            {   // DataContractSerializer style
                ctx.LoadValue(constructType);
                ctx.EmitCall(typeof(BclHelpers).GetMethod("GetUninitializedObject"));
                ctx.Cast(ExpectedType);
            }
            else if (constructType.IsClass && hasConstructor)
            {   // XmlSerializer style
                ctx.EmitCtor(constructType);
            }
            else
            {
                ctx.LoadValue(ExpectedType);
                ctx.EmitCall(typeof(TypeModel).GetMethod("ThrowCannotCreateInstance",
                    BindingFlags.Static | BindingFlags.Public));
                ctx.LoadNullRef();
                callNoteObject = false;
            }
            if (callNoteObject)
            {
                // track root object creation
                ctx.CopyValue();
                ctx.LoadReader(false);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("NoteObject",
                        BindingFlags.Static | BindingFlags.Public));
            }
            if (baseCtorCallbacks != null)
            {
                for (int i = 0; i < baseCtorCallbacks.Length; i++)
                {
                    EmitInvokeCallback(ctx, baseCtorCallbacks[i], true, null, ExpectedType);
                }
            }
        }
        private void EmitCreateIfNull(Compiler.CompilerContext ctx, Compiler.Local storage)
        {
            Helpers.DebugAssert(storage != null);
            if (!Helpers.IsValueType(ExpectedType))
            {
                Compiler.CodeLabel afterNullCheck = ctx.DefineLabel();
                ctx.LoadValue(storage);
                ctx.BranchIfTrue(afterNullCheck, false);

                ((IProtoTypeSerializer)this).EmitCreateInstance(ctx);

                if (callbacks != null) EmitInvokeCallback(ctx, callbacks.BeforeDeserialize, true, null, ExpectedType);
                ctx.StoreValue(storage);
                ctx.MarkLabel(afterNullCheck);
            }
        }
    }
}