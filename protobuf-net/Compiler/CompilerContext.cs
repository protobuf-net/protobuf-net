//#define DEBUG_COMPILE
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
    internal class CompilerContext
    {
        static int GetLen(string s)
        {
            if (s == null) return 0;
            return s.Length;
        }
#if !FX11
        readonly DynamicMethod method;
        static int next;
#endif
        private readonly bool isWriter;

#if DEBUG_COMPILE
        int label;
#endif
        public void Branch(IBranchAction branch)
        {
            Label ifLabel = il.DefineLabel(),
                continuePoint = il.DefineLabel();
#if DEBUG_COMPILE
            int ifLabelIndex = label++, continueLabelIndex = label++;
#endif

            il.Emit(OpCodes.Brtrue, ifLabel);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Brtrue + " >>> " + ifLabelIndex);
#endif
            branch.Else(this);
            il.Emit(OpCodes.Br, continuePoint);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Br + " >>> " + continueLabelIndex);
#endif
            il.MarkLabel(ifLabel);
#if DEBUG_COMPILE
            Debug.WriteLine("<<< " + ifLabelIndex);
#endif
            branch.If(this);

            il.MarkLabel(continuePoint);
#if DEBUG_COMPILE
            Debug.WriteLine("<<< " + continueLabelIndex);
#endif            
        }
#if !FX11
        public static ProtoSerializer BuildSerializer(IProtoSerializer head)
        {
            Type type = head.ExpectedType;
            CompilerContext ctx = new CompilerContext(type, true, true);
            ctx.Emit(OpCodes.Ldarg_0);
            ctx.CastFromObject(type);
            ctx.NullCheckedTail(type, head);
            ctx.Emit(OpCodes.Ret);
            return (ProtoSerializer)ctx.method.CreateDelegate(
                typeof(ProtoSerializer));
        }
#endif
        internal void Return()
        {
            Emit(OpCodes.Ret);
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
        private CompilerContext(bool isWriter, bool isStatic)
        {
            this.isStatic = isStatic;
            this.isWriter = isWriter;
        }
        internal CompilerContext(ILGenerator il, bool isWriter, bool isStatic)
            : this(isWriter, isStatic)
        {
            this.il = il;
        }
#if !FX11
        private CompilerContext(Type associatedType, bool isWriter, bool isStatic)
            : this(isWriter, isStatic)
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
                throw new NotImplementedException();
            }

            method = new DynamicMethod("proto_" + Interlocked.Increment(ref next), returnType, paramTypes, associatedType);
            this.il = method.GetILGenerator();
        }
#endif
        private readonly ILGenerator il;

        public void DiscardValue()
        {
            Emit(OpCodes.Pop);

        }
        private void Emit(OpCode opcode)
        {
            il.Emit(opcode);
#if DEBUG_COMPILE
            Debug.WriteLine(opcode);
#endif
        }
        internal void CopyValue()
        {
            Emit(OpCodes.Dup);
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
            Debug.WriteLine("$" + result.LocalIndex + ": " + type);
#endif
            return result;
        }
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
        public void LoadDest()
        {
            Emit(isStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2);
        }
        public void StoreValue(Local local)
        {
            il.Emit(OpCodes.Stloc, local.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Stloc + ": $" + local.Value.LocalIndex);
#endif
        }
        public void LoadValue(Local local)
        {
            il.Emit(OpCodes.Ldloc, local.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Ldloc + ": $" + local.Value.LocalIndex);
#endif
        }

        internal void InjectStore(Type type)
        {
            using (Local loc = GetLocal(type))
            {
                this.StoreValue(loc);
                this.LoadDest();
                this.LoadValue(loc);
            }
        }
        public Local GetLocal(Type type) { return new Local(this, type); }

        internal void EmitWrite(string methodName, Type injectForType)
        {
            this.InjectStore(injectForType);
            EmitWrite(methodName);
        }

        internal void EmitWrite(string methodName)
        {
            if (Helpers.IsNullOrEmpty(methodName)) throw new ArgumentNullException("methodName");
            MethodInfo method = typeof(ProtoWriter).GetMethod(
                methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) throw new ArgumentException("methodName");
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
        private void LoadNull()
        {
            Emit(OpCodes.Ldnull);
        }
        private class TailBranchAction : IBranchAction
        {
            public TailBranchAction(
                Local loc, IProtoSerializer tail)
            {
                this.loc = loc;
                this.tail = tail;
            }
            private readonly Local loc;
            private readonly IProtoSerializer tail;
            public void If(CompilerContext ctx) { }
            public void Else(CompilerContext ctx)
            {
                ctx.LoadValue(loc);
                loc.Dispose();
                tail.Write(ctx);
            }
        }
        internal void NullCheckedTail(Type type, IProtoSerializer tail)
        {
            if (type.IsValueType)
            {
                Type nullType = null;
#if !FX11
                nullType = Nullable.GetUnderlyingType(type);
#endif
                if (nullType == null)
                { // not a nullable T; can invoke directly
                    tail.Write(this);
                }
                else
                { // nullable T; check HasValue
                    throw new NotImplementedException();
                }
            }
            else
            { // ref-type; do a null-check
                using (Local loc = GetLocal(type))
                {
                    CopyValue();
                    StoreValue(loc);
                    LoadNull();
                    OpEqual();
                    Branch(new TailBranchAction(loc, tail));
                }


                    /*
                CopyValue();  // duplicate the ref so we can test it
                LoadValue(0); // push a null to compare
                OpEqual();    // are they equal?
                Branch(delegate
                {
                    DiscardValue(); // throw away hanging reference
                }, delegate
                {
                    tail.Write(this);
                });*/
            }
        }

        internal void OpAdd()
        {
            Emit(OpCodes.Add);
        }
        internal void OpEqual()
        {
            Emit(OpCodes.Ceq);
        }

        public void LoadValue(FieldInfo field)
        {
            OpCode code = field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
            il.Emit(code, field);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + field + " on " + field.DeclaringType);
#endif
        }
        public void LoadValue(PropertyInfo property)
        {
            EmitCall(property.GetGetMethod());
        }

        internal void LoadInputValue()
        {
            Emit(isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
        }

        internal void EmitInstance()
        {
            if (isStatic) throw new InvalidOperationException();
            Emit(OpCodes.Ldarg_0);
        }

        /* it turns out that this isn't really any faster 
        internal void BuildWriterWrapper(MethodBuilder method)
        {
            using (Local result = GetLocal(typeof(int)))
            using (Local writer = GetLocal(typeof(ProtoWriter)))
            {
            
            
                il.Emit(OpCodes.Ldarg_1); // stream
                il.Emit(OpCodes.Newobj, typeof(ProtoWriter).GetConstructor(new Type[] { typeof(Stream) })); // protowriter
                StoreValue(writer);

                Label tryFinally = il.BeginExceptionBlock();
                il.Emit(OpCodes.Ldarg_0); // serializer instance
                il.Emit(OpCodes.Ldarg_2); // value to serialize
                LoadValue(writer);        // protowriter
                EmitCall(method);         // call method
                StoreValue(result);
                Label done = il.DefineLabel();
                il.Emit(OpCodes.Leave_S, tryFinally);
            
                il.BeginFinallyBlock();
                LoadValue(writer);
                Label endFinally = il.DefineLabel();
                il.Emit(OpCodes.Brfalse_S, endFinally);
                LoadValue(writer);
                EmitCall(typeof(IDisposable).GetMethod("Dispose"));            
            
                il.MarkLabel(endFinally);
                il.EndExceptionBlock();
                LoadValue(result);
                il.Emit(OpCodes.Ret);
            }

        }*/

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

        internal void LoadAddress(Local local)
        {
            il.Emit(OpCodes.Ldloca, local.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Ldloca + ": $" + local.Value.LocalIndex);
#endif
        }
    }
}
