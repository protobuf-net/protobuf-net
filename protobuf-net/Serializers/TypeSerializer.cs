#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;
#if FEAT_COMPILER
using System.Reflection.Emit;
#endif
using System.Reflection;
using System.Runtime.Serialization;


namespace ProtoBuf.Serializers
{
    sealed class TypeSerializer : IProtoTypeSerializer
    {
        public bool HasCallbacks(TypeModel.CallbackType callbackType) {
            if(callbacks != null && callbacks[callbackType] != null) return true;
            for (int i = 0; i < serializers.Length; i++)
            {
                if (serializers[i].ExpectedType != forType && ((IProtoTypeSerializer)serializers[i]).HasCallbacks(callbackType)) return true;
            }
            return false;
        }
        private readonly Type forType;
        public Type ExpectedType { get { return forType; } }
        private readonly IProtoSerializer[] serializers;
        private readonly int[] fieldNumbers;
        private readonly bool isRootType, useConstructor, isExtensible;
        private readonly CallbackSet callbacks;
        private readonly MethodInfo[] baseCtorCallbacks;
        public TypeSerializer(Type forType, int[] fieldNumbers, IProtoSerializer[] serializers, MethodInfo[] baseCtorCallbacks, bool isRootType, bool useConstructor, CallbackSet callbacks)
        {
            Helpers.DebugAssert(forType != null);
            Helpers.DebugAssert(fieldNumbers != null);
            Helpers.DebugAssert(serializers != null);
            Helpers.DebugAssert(fieldNumbers.Length == serializers.Length);

            Helpers.Sort(fieldNumbers, serializers);
            bool hasSubTypes = false;
            for (int i = 1; i < fieldNumbers.Length; i++)
            {
                if (fieldNumbers[i] == fieldNumbers[i - 1]) throw new InvalidOperationException("Duplicate field-number detected; " +
                           fieldNumbers[i].ToString() + " on: " + forType.FullName);
                if(!hasSubTypes && serializers[i].ExpectedType != forType)
                {
                    hasSubTypes = true;
                }
            }
            this.forType = forType;
            this.serializers = serializers;
            this.fieldNumbers = fieldNumbers;
            this.callbacks = callbacks;
            this.isRootType = isRootType;
            this.useConstructor = useConstructor;
            
            if (baseCtorCallbacks != null && baseCtorCallbacks.Length == 0) baseCtorCallbacks = null;
            this.baseCtorCallbacks = baseCtorCallbacks;
#if !NO_GENERICS
            if (Nullable.GetUnderlyingType(forType) != null)
            {
                throw new ArgumentException("Cannot create a TypeSerializer for nullable types", "forType");
            }
#endif
            
            if (typeof(IExtensible).IsAssignableFrom(forType))
            {
                if (forType.IsValueType || !isRootType || hasSubTypes)
                {
                    throw new NotSupportedException("IExtensible is not supported in structs or classes with inheritance");
                }
                isExtensible = true;
            }
        }

        public void Callback(object value, TypeModel.CallbackType callbackType)
        {
            if (callbacks != null) InvokeCallback(callbacks[callbackType], value);
            IProtoTypeSerializer ser = (IProtoTypeSerializer)GetMoreSpecificSerializer(value);
            if (ser != null) ser.Callback(value, callbackType);

        }
        private bool CanHaveInheritance
        {
            get { return forType.IsClass && !forType.IsSealed; }
        }
        private IProtoSerializer GetMoreSpecificSerializer(object value)
        {
            if (!CanHaveInheritance) return null;
            Type actualType = value.GetType();
            if (actualType == forType) return null;
           
            for (int i = 0; i < serializers.Length; i++)
            {
                IProtoSerializer ser = serializers[i];
                if (ser.ExpectedType != forType && ser.ExpectedType.IsAssignableFrom(actualType))
                {
                    return ser;
                }
            }
            TypeModel.ThrowUnexpectedSubtype(forType, actualType); // might throw (if not a proxy)
            return null;
        }

        public void Write(object value, ProtoWriter dest)
        {
            if (isRootType) Callback(value, TypeModel.CallbackType.BeforeSerialize);
            // write inheritance first
            IProtoSerializer next = GetMoreSpecificSerializer(value);
            if (next != null) next.Write(value, dest);

            // write all actual fields
            //Helpers.DebugWriteLine(">> Writing fields for " + forType.FullName);
            for (int i = 0; i < serializers.Length; i++)
            {
                IProtoSerializer ser = serializers[i];
                if (ser.ExpectedType == forType)
                {
                    //Helpers.DebugWriteLine(": " + ser.ToString());
                    ser.Write(value, dest);
                }
            }
            //Helpers.DebugWriteLine("<< Writing fields for " + forType.FullName);
            if (isExtensible) ProtoWriter.AppendExtensionData((IExtensible)value, dest);
            if (isRootType) Callback(value, TypeModel.CallbackType.AfterSerialize);
        }
        public object Read(object value, ProtoReader source)
        {
            if (isRootType && value != null) { Callback(value, TypeModel.CallbackType.BeforeDeserialize); } 
            int fieldNumber, lastFieldNumber = 0, lastFieldIndex = 0;
            bool fieldHandled;

            //Helpers.DebugWriteLine(">> Reading fields for " + forType.FullName);
            while ((fieldNumber = source.ReadFieldHeader()) > 0)
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
                        IProtoSerializer ser = serializers[i];
                        //Helpers.DebugWriteLine(": " + ser.ToString());
                        if (value == null && ser.ExpectedType == forType) value = CreateInstance(source);
                        if (ser.ReturnsValue) {
                            value = ser.Read(value, source);
                        } else { // pop
                            ser.Read(value, source);
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
                    if (value == null) value = CreateInstance(source);
                    if (isExtensible) {
                        source.AppendExtensionData((IExtensible)value); 
                    } else {
                        source.SkipField();
                    }
                }
            }
            //Helpers.DebugWriteLine("<< Reading fields for " + forType.FullName);
            if(value == null) value = CreateInstance(source);
            if (isRootType) { Callback(value, TypeModel.CallbackType.AfterDeserialize); } 
            return value;
        }
        private void InvokeCallback(MethodInfo method, object obj)
        {
            if (method != null)
            {   // pass in a streaming context if one is needed, else null
                switch (method.GetParameters().Length)
                {
                    case 0: method.Invoke(obj, null); break;
#if PLAT_BINARYFORMATTER
                    case 1: method.Invoke(obj, new object[] { new StreamingContext(StreamingContextState) }); break;
#endif
                    default: throw new NotSupportedException("Invalid callback signature for this platform");
                }
            }
        }
        object CreateInstance(ProtoReader source)
        {
            //Helpers.DebugWriteLine("* creating : " + forType.FullName);
            object obj;
            if (useConstructor)
            {
                if (forType.IsAbstract) TypeModel.ThrowCannotCreateInstance(forType);
                obj = Activator.CreateInstance(forType
#if !CF
                    , true
#endif
                    );
            }
            else
            {
                obj = BclHelpers.GetUninitializedObject(forType);
            }
            if (baseCtorCallbacks != null) {
                for (int i = 0; i < baseCtorCallbacks.Length; i++) {
                    InvokeCallback(baseCtorCallbacks[i], obj);
                }
            }
            if (callbacks != null) InvokeCallback(callbacks.BeforeDeserialize, obj);
            return obj;
        }

        bool IProtoSerializer.RequiresOldValue { get { return true; } }
        bool IProtoSerializer.ReturnsValue { get { return false; } } // updates field directly
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            {
                if (forType.IsEnum) return;
                // pre-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeSerialize);

                Compiler.CodeLabel startFields = ctx.DefineLabel();
                // inheritance
                if (CanHaveInheritance)
                {
                    for (int i = 0; i < serializers.Length; i++)
                    {
                        IProtoSerializer ser = serializers[i];
                        if (ser.ExpectedType != forType)
                        {
                            Compiler.CodeLabel ifMatch = ctx.DefineLabel(), nextTest = ctx.DefineLabel();
                            ctx.LoadValue(loc);
                            ctx.TryCast(ser.ExpectedType);
                            ctx.CopyValue();
                            ctx.BranchIfTrue(ifMatch, true);
                            ctx.DiscardValue();
                            ctx.Branch(nextTest, true);
                            ctx.MarkLabel(ifMatch);
                            ser.EmitWrite(ctx, null);
                            ctx.Branch(startFields, false);
                            ctx.MarkLabel(nextTest);
                        }
                    }
                    // would have jumped to "fields" if an expected sub-type, so two options:
                    // a: *exactly* that type, b: an *unexpected* type
                    ctx.LoadValue(loc);
                    ctx.EmitCall(typeof(object).GetMethod("GetType"));
                    ctx.LoadValue(forType);
                    ctx.BranchIfEqual(startFields, true);
                    // unexpected, then... note that this *might* be a proxy, which
                    // is handled by ThrowUnexpectedSubtype
                    ctx.LoadValue(forType);
                    ctx.LoadValue(loc);
                    ctx.EmitCall(typeof(object).GetMethod("GetType"));
                    ctx.EmitCall(typeof(TypeModel).GetMethod("ThrowUnexpectedSubtype",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));

                }
                // fields
                
                ctx.MarkLabel(startFields);
                for (int i = 0; i < serializers.Length; i++)
                {
                    IProtoSerializer ser = serializers[i];
                    if (ser.ExpectedType == forType) ser.EmitWrite(ctx, loc);
                }

                // extension data
                if (isExtensible)
                {
                    ctx.LoadValue(loc);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(typeof(ProtoWriter).GetMethod("AppendExtensionData"));
                }
                // post-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.AfterSerialize);
            }
        }
        static void EmitInvokeCallback(Compiler.CompilerContext ctx, MethodInfo method)
        {
            if (method != null)
            {
                ctx.CopyValue(); // assumes the target is on the stack, and that we want to *retain* it on the stack
                if (method.GetParameters().Length == 1)
                {
                    ctx.LoadValue((int)StreamingContextState);
                    ctx.EmitCtor(typeof(StreamingContext), new Type[] { typeof(StreamingContextStates) });
                }
                ctx.EmitCall(method);
            }
        }
        private void EmitCallbackIfNeeded(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType) {
            Helpers.DebugAssert(valueFrom != null);
            if (isRootType && ((IProtoTypeSerializer)this).HasCallbacks(callbackType))
            {
                ((IProtoTypeSerializer)this).EmitCallback(ctx, valueFrom, callbackType);
            }
        }   
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            Helpers.DebugAssert(((IProtoTypeSerializer)this).HasCallbacks(callbackType), "Shouldn't be calling this if there is nothing to do");
            MethodInfo method = callbacks == null ? null : callbacks[callbackType];
            ctx.LoadValue(valueFrom);
            EmitInvokeCallback(ctx, method);
            Compiler.CodeLabel @break = ctx.DefineLabel();
            if (CanHaveInheritance)
            {
                for (int i = 0; i < serializers.Length; i++)
                {
                    IProtoSerializer ser = serializers[i];
                    IProtoTypeSerializer typeser;
                    if (ser.ExpectedType != forType && (typeser = (IProtoTypeSerializer)ser).HasCallbacks(callbackType))
                    {
                        Compiler.CodeLabel ifMatch = ctx.DefineLabel(), nextTest = ctx.DefineLabel();
                        ctx.CopyValue();
                        ctx.TryCast(ser.ExpectedType);
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
            }
            ctx.MarkLabel(@break);                
            ctx.DiscardValue();
        }
        const StreamingContextStates StreamingContextState = StreamingContextStates.Persistence;
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            Helpers.DebugAssert(valueFrom != null);

            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int)))
            {
                if (forType.IsEnum) return;
                // pre-callbacks
                if (HasCallbacks(TypeModel.CallbackType.BeforeDeserialize))
                {
                    Compiler.CodeLabel callbacksDone = ctx.DefineLabel();
                    ctx.LoadValue(loc);
                    ctx.BranchIfFalse(callbacksDone, false);
                    EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeDeserialize);
                    ctx.MarkLabel(callbacksDone);
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
                        WriteFieldHandler(ctx, expected, loc, processThisField, @continue, (IProtoSerializer)group.Items[0]);
                    }
                    else
                    {   // implement as a jump-table-based switch
                        ctx.LoadValue(fieldNumber);
                        ctx.LoadValue(group.First);
                        ctx.Subtract(); // jump-tables are zero-based
                        Compiler.CodeLabel[] jmp = new Compiler.CodeLabel[groupItemCount];
                        for (int i = 0; i < groupItemCount; i++) {
                            jmp[i] = ctx.DefineLabel();
                        }
                        ctx.Switch(jmp);
                        // write the default...
                        ctx.Branch(tryNextField, false);
                        for (int i = 0; i < groupItemCount; i++)
                        {
                            WriteFieldHandler(ctx, expected, loc, jmp[i], @continue, (IProtoSerializer)group.Items[i]);
                        }
                    }
                    ctx.MarkLabel(tryNextField);
                }

                EmitCreateIfNull(ctx, expected, loc);
                ctx.LoadReaderWriter();
                if (isExtensible)
                {
                    ctx.LoadValue(loc);
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("AppendExtensionData"));
                }
                else
                {
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("SkipField"));
                }
                
                ctx.MarkLabel(@continue);
                ctx.EmitBasicRead("ReadFieldHeader", typeof(int));
                ctx.CopyValue();
                ctx.StoreValue(fieldNumber);
                ctx.LoadValue(0);
                ctx.BranchIfGreater(processField, false);

                EmitCreateIfNull(ctx, expected, loc);
                // post-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.AfterDeserialize);
            }
        }

        private void WriteFieldHandler(
            Compiler.CompilerContext ctx, Type expected, Compiler.Local loc,
            Compiler.CodeLabel handler, Compiler.CodeLabel @continue, IProtoSerializer serializer)
        {
            ctx.MarkLabel(handler);
            if (serializer.ExpectedType == forType) {
                EmitCreateIfNull(ctx, expected, loc);
                serializer.EmitRead(ctx, loc);
            }
            else {
                ctx.LoadValue(loc);
                ctx.Cast(serializer.ExpectedType);
                serializer.EmitRead(ctx, null);                
            }
            
            if (serializer.ReturnsValue)
            {   // update the variable
                ctx.StoreValue(loc);
            }
            ctx.Branch(@continue, false); // "continue"
        }
        

        private void EmitCreateIfNull(Compiler.CompilerContext ctx, Type type, Compiler.Local storage)
        {
            Helpers.DebugAssert(storage != null);
            if (!type.IsValueType)
            {
                Compiler.CodeLabel afterNullCheck = ctx.DefineLabel();
                ctx.LoadValue(storage);
                ctx.BranchIfTrue(afterNullCheck, true);

                ConstructorInfo defaultCtor;
                // different ways of creating a new instance
                if (!useConstructor)
                {   // DataContractSerializer style
                    ctx.LoadValue(forType);
                    ctx.EmitCall(typeof(BclHelpers).GetMethod("GetUninitializedObject"));
                    ctx.Cast(forType);
                } else if (type.IsClass && !type.IsAbstract && (
                    (defaultCtor = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null, Helpers.EmptyTypes, null)) != null))
                {   // XmlSerializer style
                    ctx.EmitCtor(type);
                }
                else
                {
                    ctx.LoadValue(type);
                    ctx.EmitCall(typeof(TypeModel).GetMethod("ThrowCannotCreateInstance",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
                    ctx.LoadNullRef();
                }
                if (baseCtorCallbacks != null) {
                    for (int i = 0; i < baseCtorCallbacks.Length; i++) {
                        EmitInvokeCallback(ctx, baseCtorCallbacks[i]);
                    }
                }
                if (callbacks != null) EmitInvokeCallback(ctx, callbacks.BeforeDeserialize);
                ctx.StoreValue(storage);
                ctx.MarkLabel(afterNullCheck);
            }
        }
#endif
    }

}
#endif