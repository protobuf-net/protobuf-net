#if FEAT_COMPILER
#define DEBUG_COMPILE
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
#if DEBUG_COMPILE
using System.Diagnostics;
#endif

namespace ProtoBuf.Compiler
{
    internal struct CodeLabel
    {
        public readonly Label Value;
        public readonly int Index;
        public CodeLabel(Label value, int index)
        {
            this.Value = value;
            this.Index = index;
        }
    }
    internal class CompilerContext
    {
        
#if !FX11
        readonly DynamicMethod method;
        static int next;
#endif

        internal CodeLabel DefineLabel()
        {
            CodeLabel result = new CodeLabel(il.DefineLabel(), nextLabel++);
            return result;
        }
        internal void MarkLabel(CodeLabel label)
        {
            il.MarkLabel(label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine("#: " + label.Index);
#endif
        }

#if !FX11
        public static ProtoSerializer BuildSerializer(IProtoSerializer head)
        {
            Type type = head.ExpectedType;
            CompilerContext ctx = new CompilerContext(type, true, true);
            ctx.LoadValue(Local.InputValue);
            ctx.CastFromObject(type);
            ctx.WriteNullCheckedTail(type, head, null);
            ctx.Emit(OpCodes.Ret);
            return (ProtoSerializer)ctx.method.CreateDelegate(
                typeof(ProtoSerializer));
        }
        public static ProtoDeserializer BuildDeserializer(IProtoSerializer head)
        {
            Type type = head.ExpectedType;
            CompilerContext ctx = new CompilerContext(type, false, true);
            
            using (Local typedVal = new Local(ctx, type))
            using (Local position = type.IsValueType ? new Local(ctx, typeof(int)) : null)
            {
                if (position == null)
                {
                    ctx.LoadValue(Local.InputValue);
                    ctx.CastFromObject(type);
                    ctx.StoreValue(typedVal);
                }
                else
                {   // check if the input obj is null; if so capture the position (otherwise use -1)
                    ctx.LoadValue(Local.InputValue);
                    ctx.LoadNull();
                    CodeLabel ifNull = ctx.DefineLabel(), endNull = ctx.DefineLabel();
                    ctx.BranchIfEqual(ifNull);

                    ctx.LoadValue(-1);
                    ctx.StoreValue(position);
                    ctx.LoadValue(Local.InputValue);
                    ctx.CastFromObject(type);
                    ctx.StoreValue(typedVal);
                    ctx.Branch(endNull);

                    ctx.MarkLabel(ifNull);
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(typeof(ProtoReader).GetProperty("Position"));
                    ctx.StoreValue(position);
                    ctx.LoadAddress(typedVal, type);
                    ctx.EmitCtor(type);

                    ctx.MarkLabel(endNull);
                }
                head.EmitRead(ctx, typedVal);

                if (head.ReturnsValue)
                {
                    ctx.StoreValue(typedVal);
                }

                if (position == null)
                {
                    ctx.LoadValue(typedVal);
                    ctx.CastToObject(type);
                }
                else {
                    ctx.LoadValue(position);
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(typeof(ProtoReader).GetProperty("Position"));
                    CodeLabel noData = ctx.DefineLabel(), endData = ctx.DefineLabel();
                    ctx.BranchIfEqual(noData);

                    ctx.LoadValue(typedVal);
                    ctx.CastToObject(type);
                    ctx.Branch(endData);

                    ctx.MarkLabel(noData);
                    ctx.LoadNull();
                    ctx.MarkLabel(endData);
                }
            }
            ctx.Emit(OpCodes.Ret);
            return (ProtoDeserializer)ctx.method.CreateDelegate(
                typeof(ProtoDeserializer));
        }
#endif
        internal void Return()
        {
            Emit(OpCodes.Ret);
        }
        internal void CastToObject(Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Box + ": " + type);
#endif
            }
            else
            {
                il.Emit(OpCodes.Castclass, typeof(object));
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Castclass + ": " + type);
#endif
            }
        }

        internal void CastFromObject(Type type)
        {
            if (type.IsValueType)
            {
#if FX11
                il.Emit(OpCodes.Unbox, type);
                il.Emit(OpCodes.Ldobj, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Unbox + ": " + type);
                Debug.WriteLine(OpCodes.Ldobj + ": " + type);
#endif
#else
                il.Emit(OpCodes.Unbox_Any, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Unbox_Any + ": " + type);
#endif
#endif

            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Castclass + ": " + type);
#endif
            }
        }
        private readonly bool isStatic;
        private CompilerContext(bool isStatic)
        {
            this.isStatic = isStatic;
        }
        internal CompilerContext(ILGenerator il, bool isStatic)
            : this(isStatic)
        {
            this.il = il;
        }
#if !FX11
        private CompilerContext(Type associatedType, bool isWriter, bool isStatic)
            : this(isStatic)
        {
            Type[] paramTypes;
            Type returnType;
            if (isWriter)
            {
                returnType = typeof(void);
                paramTypes = new Type[] { typeof(object), typeof(ProtoWriter) };
            }
            else
            {
                returnType = typeof(object);
                paramTypes = new Type[] { typeof(object), typeof(ProtoReader) };
            }

            method = new DynamicMethod("proto_" + Interlocked.Increment(ref next), returnType, paramTypes, associatedType);
            this.il = method.GetILGenerator();
        }
#endif
        private readonly ILGenerator il;

        private void Emit(OpCode opcode)
        {
            il.Emit(opcode);
#if DEBUG_COMPILE
            Debug.WriteLine(opcode);
#endif
        }
        public void LoadValue(int value)
        {
            switch (value)
            {
                case 0: Emit(OpCodes.Ldc_I4_0); break;
                case 1: Emit(OpCodes.Ldc_I4_1); break;
                case 2: Emit(OpCodes.Ldc_I4_2); break;
                case 3: Emit(OpCodes.Ldc_I4_3); break;
                case 4: Emit(OpCodes.Ldc_I4_4); break;
                case 5: Emit(OpCodes.Ldc_I4_5); break;
                case 6: Emit(OpCodes.Ldc_I4_6); break;
                case 7: Emit(OpCodes.Ldc_I4_7); break;
                case 8: Emit(OpCodes.Ldc_I4_8); break;
                case -1: Emit(OpCodes.Ldc_I4_M1); break;
                default:
                    il.Emit(OpCodes.Ldc_I4, value);
#if DEBUG_COMPILE
                    Debug.WriteLine(OpCodes.Ldc_I4 + ": " + value);
#endif
                    break;


            }
        }

        MutableList locals = new MutableList();
        internal LocalBuilder GetFromPool(Type type)
        {
            int count = locals.Count;
            for (int i = 0; i < count; i++)
            {
                LocalBuilder item = (LocalBuilder)locals[i];
                if (item != null && item.LocalType == type)
                {
                    locals[i] = null; // remove from pool
                    return item;
                }
            }
            LocalBuilder result = il.DeclareLocal(type);
#if DEBUG_COMPILE
            Debug.WriteLine("$ " + result + ": " + type);
#endif
            return result;
        }
        //
        internal void ReleaseToPool(LocalBuilder value)
        {
            int count = locals.Count;
            for (int i = 0; i < count; i++)
            {
                if (locals[i] == null)
                {
                    locals[i] = value; // released into existing slot
                    return;
                }
            }
            locals.Add(value); // create a new slot
        }
        public void LoadReaderWriter()
        {
            Emit(isStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2);
        }
        public void StoreValue(Local local)
        {
            if (local == Local.InputValue)
            {
                byte b = isStatic ? (byte) 0 : (byte)1;
                il.Emit(OpCodes.Starg_S, b);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Starg_S + ": $" + b);
#endif                
            }
            else
            {
#if !FX11
                switch (local.Value.LocalIndex)
                {
                    case 0: Emit(OpCodes.Stloc_0); break;
                    case 1: Emit(OpCodes.Stloc_1); break;
                    case 2: Emit(OpCodes.Stloc_2); break;
                    case 3: Emit(OpCodes.Stloc_3); break;
                    default:
#endif
                        OpCode code = UseShortForm(local) ? OpCodes.Stloc_S : OpCodes.Stloc;
                        il.Emit(code, local.Value);
#if DEBUG_COMPILE
                        Debug.WriteLine(code + ": $" + local.Value);
#endif
#if !FX11
                        break;
                }
#endif
            }
        }
        public void LoadValue(Local local)
        {
            if (local == null) { /* nothing to do; top of stack */}
            else if (local == Local.InputValue)
            {
                Emit(isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
            }
            else
            {
#if !FX11
                switch (local.Value.LocalIndex)
                {
                    case 0: Emit(OpCodes.Ldloc_0); break;
                    case 1: Emit(OpCodes.Ldloc_1); break;
                    case 2: Emit(OpCodes.Ldloc_2); break;
                    case 3: Emit(OpCodes.Ldloc_3); break;
                    default:
#endif             
                        OpCode code = UseShortForm(local) ? OpCodes.Ldloc_S :  OpCodes.Ldloc;
                        il.Emit(code, local.Value);
#if DEBUG_COMPILE
                        Debug.WriteLine(code + ": $" + local.Value);
#endif
#if !FX11
                        break;
                }
#endif
            }
        }

        internal void InjectStore(Type type, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = GetLocalWithValue(type, valueFrom))
            {
                LoadReaderWriter();
                this.LoadValue(loc);
            }
        }
        public Local GetLocalWithValue(Type type, Compiler.Local fromValue) {
            if (fromValue != null) { return fromValue.AsCopy(); }
            // need to store the value from the stack
            Local result = new Local(this, type);
            StoreValue(result);
            return result;
        }
        internal void EmitBasicRead(string methodName, Type expectedType)
        {
            MethodInfo method = typeof(ProtoReader).GetMethod(
                methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null || method.ReturnType != expectedType
                || method.GetParameters().Length != 0) throw new ArgumentException("methodName");
            LoadReaderWriter();
            EmitCall(method);            
        }
        internal void EmitWrite(string methodName, Type injectForType, Compiler.Local fromValue)
        {
            this.InjectStore(injectForType, fromValue);
            EmitWrite(methodName);
        }

        internal void EmitWrite(string methodName)
        {
            if (Helpers.IsNullOrEmpty(methodName)) throw new ArgumentNullException("methodName");
            MethodInfo method = typeof(ProtoWriter).GetMethod(
                methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null || method.ReturnType != typeof(void)) throw new ArgumentException("methodName");
            EmitCall(method);
        }
        public void EmitCall(MethodInfo method)
        {
            OpCode opcode = (method.IsStatic || method.DeclaringType.IsValueType) ? OpCodes.Call : OpCodes.Callvirt;
            il.EmitCall(opcode, method, null);
#if DEBUG_COMPILE
            Debug.WriteLine(opcode + ": " + method + " on " + method.DeclaringType);
#endif
        }
        public void LoadNull()
        {
            Emit(OpCodes.Ldnull);
        }
#if DEBUG_COMPILE
        private int nextLabel;
#endif
        internal void WriteNullCheckedTail(Type type, IProtoSerializer tail, Compiler.Local valueFrom)
        {
            if (type.IsValueType)
            {
                Type underlyingType = null;
#if !FX11
                underlyingType = Nullable.GetUnderlyingType(type);
#endif
                if (underlyingType == null)
                { // not a nullable T; can invoke directly
                    tail.EmitWrite(this, valueFrom);
                }
                else
                { // nullable T; check HasValue
                    using (Compiler.Local valOrNull = GetLocalWithValue(type, valueFrom))
                    {
                        LoadAddress(valOrNull, type);
                        LoadValue(type.GetProperty("HasValue"));
                        CodeLabel @end = DefineLabel();
                        BranchIfFalse(@end);
                        LoadAddress(valOrNull, type);
                        EmitCall(type.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                        tail.EmitWrite(this, null);
                        MarkLabel(@end);
                    }
                }
            }
            else
            { // ref-type; do a null-check
                using (Compiler.Local loc = GetLocalWithValue(type, valueFrom))
                {
                    LoadValue(loc);
                    LoadNull();
                    CodeLabel @end = DefineLabel();
                    BranchIfEqual(@end);
                    tail.EmitWrite(this, loc);
                    MarkLabel(@end);
                }
            }
        }

        internal void ReadNullCheckedTail(Type type, IProtoSerializer tail, Compiler.Local valueFrom)
        {
#if !FX11
            Type underlyingType;
            if (type.IsValueType && (underlyingType = Nullable.GetUnderlyingType(type)) != null)
            {
                if(tail.RequiresOldValue)
                {
                    // we expect the input value to be in valueFrom; need to unpack it from T?
                    LoadAddress(valueFrom, type);
                    EmitCall(type.GetMethod("GetValueOrDefault", Type.EmptyTypes)); 
                }
                else
                {
                    Debug.Assert(valueFrom == null); // not expecting a valueFrom in this case
                }
                tail.EmitRead(this, null); // either unwrapped on the stack or not provided
                if (tail.ReturnsValue)
                {
                    // now re-wrap the value
                    EmitCtor(type, underlyingType);
                }
                return;
            }
#endif
            // either a ref-type of a non-nullable struct; treat "as is", even if null
            // (the type-serializer will handle the null case; it needs to allow null
            // inputs to perform the correct type of subclass creation)
            tail.EmitRead(this, valueFrom);
        }

        public void EmitCtor(Type type)
        {
            EmitCtor(type, Type.EmptyTypes);
        }
        public void EmitCtor(Type type, params Type[] parameterTypes)
        {
            Debug.Assert(type != null);
            Debug.Assert(parameterTypes != null);
            if (type.IsValueType && parameterTypes.Length == 0)
            {
                il.Emit(OpCodes.Initobj, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Initobj + ": " + type);
#endif
            }
            else
            {
                ConstructorInfo ctor = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, parameterTypes, null);
                if (ctor == null) throw new InvalidOperationException("No suitable constructor found");
                il.Emit(OpCodes.Newobj, ctor);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Newobj + ": " + type);
#endif
            }
        }

        public void LoadValue(FieldInfo field)
        {
            OpCode code = field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
            il.Emit(code, field);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + field + " on " + field.DeclaringType);
#endif
        }
        public void StoreValue(FieldInfo field)
        {
            OpCode code = field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld;
            il.Emit(code, field);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + field + " on " + field.DeclaringType);
#endif
        }
        public void LoadValue(PropertyInfo property)
        {
            EmitCall(property.GetGetMethod());
        }
        public void StoreValue(PropertyInfo property)
        {
            EmitCall(property.GetSetMethod());
        }

        internal void EmitInstance()
        {
            if (isStatic) throw new InvalidOperationException();
            Emit(OpCodes.Ldarg_0);
        }

        internal static void LoadValue(ILGenerator il, int value)
        {
            switch (value)
            {
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                default: il.Emit(OpCodes.Ldc_I4, value); break;
            }
        }

        private bool UseShortForm(Local local)
        {
#if FX11
            return locals.Count < 256;
#else
            return local.Value.LocalIndex < 256;
#endif
        }
        internal void LoadAddress(Local local, Type type)
        {
            if (type.IsValueType)
            {
                // if the value is on the stack we'll need a local variable to it;
                // may as well use GetLocalWithValue which handles all scenarios
                using (Compiler.Local tmp = GetLocalWithValue(type, local))
                {
                    if (tmp == Local.InputValue)
                    {
                        il.Emit(OpCodes.Ldarga_S, (isStatic ? (byte)0 : (byte)1));
#if DEBUG_COMPILE
                        Debug.WriteLine(OpCodes.Ldarga_S + ": $" + (isStatic ? 0 : 1));
#endif
                    }
                    else
                    {
                        OpCode code = UseShortForm(tmp) ? OpCodes.Ldloca_S : OpCodes.Ldloca;
                        il.Emit(code, tmp.Value);
#if DEBUG_COMPILE
                        Debug.WriteLine(code + ": $" + local.Value);
#endif
                    }
                }

            }
            else
            {   // reference-type; already *is* the address; just load it
                LoadValue(local);
            }
        }
        internal void Branch(CodeLabel label)
        {
            il.Emit(OpCodes.Br, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Br + ": " + label.Index);
#endif
        }
        internal void BranchIfFalse(CodeLabel label)
        {
            il.Emit(OpCodes.Brfalse, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Brfalse + ": " + label.Index);
#endif
        }


        internal void BranchIfTrue(CodeLabel label)
        {
            il.Emit(OpCodes.Brtrue, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Brtrue + ": " + label.Index);
#endif
        }
        internal void BranchIfEqual(CodeLabel label)
        {
            il.Emit(OpCodes.Beq, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Beq + ": " + label.Index);
#endif
        }
        internal void TestEqual()
        {
            Emit(OpCodes.Ceq);
        }


        internal void CopyValue()
        {
            Emit(OpCodes.Dup);
        }

        internal void BranchIfGreater(CodeLabel label)
        {
            il.Emit(OpCodes.Bgt, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Bgt + ": " + label.Index);
#endif
        }

        internal void DiscardValue()
        {
            Emit(OpCodes.Pop);
        }
    }
}
#endif