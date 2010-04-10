#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;
using System.Reflection.Emit;
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
        private readonly bool applyCallbacks, useConstructor;
        private readonly CallbackSet callbacks;
        private readonly MethodInfo[] baseCtorCallbacks;
        public TypeSerializer(Type forType, int[] fieldNumbers, IProtoSerializer[] serializers, MethodInfo[] baseCtorCallbacks, bool applyCallbacks, bool useConstructor, CallbackSet callbacks)
        {
            Helpers.DebugAssert(forType != null);
            Helpers.DebugAssert(fieldNumbers != null);
            Helpers.DebugAssert(serializers != null);
            Helpers.DebugAssert(fieldNumbers.Length == serializers.Length);

            Helpers.Sort(fieldNumbers, serializers);
            this.forType = forType;
            this.serializers = serializers;
            this.fieldNumbers = fieldNumbers;
            this.callbacks = callbacks;
            this.applyCallbacks = applyCallbacks;
            this.useConstructor = useConstructor;
            
            if (baseCtorCallbacks != null && baseCtorCallbacks.Length == 0) baseCtorCallbacks = null;
            this.baseCtorCallbacks = baseCtorCallbacks;
#if !NO_GENERICS
            if (Nullable.GetUnderlyingType(forType) != null)
            {
                throw new ArgumentException("Cannot create a TypeSerializer for nullable types", "forType");
            }
#endif
        }

        public void Callback(object value, TypeModel.CallbackType callbackType)
        {
            if (callbacks != null) InvokeCallback(callbacks[callbackType], value);
            IProtoTypeSerializer ser = (IProtoTypeSerializer)GetMoreSpecificSerializer(value);
            if (ser != null) ser.Callback(value, callbackType);

        }
        private IProtoSerializer GetMoreSpecificSerializer(object value)
        {
            Type actualType = value.GetType();
            if (actualType != forType)
            {
                for (int i = 0; i < serializers.Length; i++)
                {
                    IProtoSerializer ser = serializers[i];
                    if (ser.ExpectedType != forType && ser.ExpectedType.IsAssignableFrom(actualType))
                    {
                        return ser;
                    }
                }
            }
            return null;
        }
        public void Write(object value, ProtoWriter dest)
        {
            if (applyCallbacks) Callback(value, TypeModel.CallbackType.BeforeSerialize);
            // write inheritance first
            IProtoSerializer next = GetMoreSpecificSerializer(value);
            if (next != null) next.Write(value, dest);

            // write all actual fields
            for (int i = 0; i < serializers.Length; i++)
            {
                IProtoSerializer ser = serializers[i];
                if(ser.ExpectedType == forType) ser.Write(value, dest);
            }
            if (applyCallbacks) Callback(value, TypeModel.CallbackType.AfterSerialize);
        }

        public object Read(object value, ProtoReader source)
        {
            if (applyCallbacks && value != null) { Callback(value, TypeModel.CallbackType.BeforeDeserialize); } 
            int fieldNumber, lastFieldNumber = 0, lastFieldIndex = 0;
            bool fieldHandled;
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
                    if (value == null) value = CreateInstance(source);
                    source.SkipField();
                }
            }
            if(value == null) value = CreateInstance(source);
            if (applyCallbacks) { Callback(value, TypeModel.CallbackType.AfterDeserialize); } 
            return value;
        }
        private void InvokeCallback(MethodInfo method, object obj)
        {
            if (method != null)
            {   // pass in a streaming context if one is needed, else null
                method.Invoke(obj, method.GetParameters().Length == 0 ? null :
                    new object[] { new StreamingContext(StreamingContextState) });
            }
        }
        object CreateInstance(ProtoReader source)
        {
            object obj = useConstructor ? Activator.CreateInstance(forType) : FormatterServices.GetUninitializedObject(forType);
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
                // pre-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.BeforeSerialize);

                // inheritance
                Compiler.CodeLabel startFields = ctx.DefineLabel();
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
                // fields
                ctx.MarkLabel(startFields);                
                for (int i = 0; i < serializers.Length; i++)
                {
                    IProtoSerializer ser = serializers[i];
                    if(ser.ExpectedType == forType) ser.EmitWrite(ctx, loc);
                }

                // post-callbacks
                EmitCallbackIfNeeded(ctx, loc, TypeModel.CallbackType.AfterSerialize);
            }
        }
        class Group
        {
            public readonly int First;
            public readonly BasicList Items;
            public Group(int first)
            {
                this.First = first;
                this.Items = new BasicList();
            }
        }
        BasicList GetContiguousGroups()
        {
            BasicList outer = new BasicList();
            Group group = null;
            int lastIndex = 0;
            for (int i = 0; i < fieldNumbers.Length; i++)
            {
                if (fieldNumbers[i] != lastIndex + 1) { group = null; }
                if (group == null)
                {
                    group = new Group(fieldNumbers[i]);
                    outer.Add(group);
                }
                lastIndex = fieldNumbers[i];
                group.Items.Add(serializers[i]);
            }
            return outer;
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
            if (applyCallbacks && ((IProtoTypeSerializer)this).HasCallbacks(callbackType))
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
                foreach (Group group in GetContiguousGroups())
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
                ctx.EmitCall(typeof(ProtoReader).GetMethod("SkipField"));
                
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
                    ctx.EmitCall(typeof(FormatterServices).GetMethod("GetUninitializedObject"));
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
                    //TODO: raise an appropriate message; unable to create new instance, and 'tis null
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