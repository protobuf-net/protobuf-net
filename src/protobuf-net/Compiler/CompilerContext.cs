//#define DEBUG_COMPILE
using System;
using System.Threading;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Diagnostics;
using ProtoBuf.Internal;
using System.Collections;
using System.Linq;
using System.Globalization;
using ProtoBuf.Internal.Serializers;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ProtoBuf.Compiler
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct CodeLabel
    {
        public readonly Label Value;
        public readonly int Index;
        public CodeLabel(Label value, int index)
        {
            this.Value = value;
            this.Index = index;
        }
    }
    internal sealed class CompilerContext : IDisposable
    {
        public TypeModel Model { get; }

        private readonly DynamicMethod method;
        private static int next;

        internal CodeLabel DefineLabel()
        {
            CodeLabel result = new CodeLabel(il.DefineLabel(), nextLabel++);
            return result;
        }
#if DEBUG_COMPILE
        static readonly string traceCompilePath;
        static CompilerContext()
        {
            traceCompilePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),
                "TraceCompile.txt");
            Console.WriteLine("DEBUG_COMPILE enabled; writing to " + traceCompilePath);
        }
#endif
        [System.Diagnostics.Conditional("DEBUG_COMPILE")]
#pragma warning disable CA1822 // Mark members as static - it uses state when it has content
        private void TraceCompile(string value)
#pragma warning restore CA1822 // Mark members as static
        {
#if DEBUG_COMPILE
            if (!string.IsNullOrWhiteSpace(value))
            {
                using (System.IO.StreamWriter sw = System.IO.File.AppendText(traceCompilePath))
                {
                    sw.WriteLine(value);
                }
            }
#endif
        }
        internal void MarkLabel(CodeLabel label)
        {
            il.MarkLabel(label.Value);
            TraceCompile("#: " + label.Index);
        }

        public static ProtoSerializer<TActual> BuildSerializer<TActual>(CompilerContextScope scope, IRuntimeProtoSerializerNode head, TypeModel model)
        {
            Type type = head.ExpectedType;
            try
            {
                using CompilerContext ctx = new CompilerContext(scope, type, SignatureType.WriterScope_Input, true, model, typeof(TActual), null);
                ctx.WriteNullCheckedTail(type, head, ctx.InputValue);
                ctx.Emit(OpCodes.Ret);
                return (ProtoSerializer<TActual>)ctx.method.CreateDelegate(
                    typeof(ProtoSerializer<TActual>));
            }
            catch (Exception ex)
            {
                string name = type.FullName;
                if (string.IsNullOrEmpty(name)) name = type.Name;
                throw new InvalidOperationException("It was not possible to prepare a serializer for: " + name, ex);
            }
        }
        /*public static ProtoCallback BuildCallback(IProtoTypeSerializer head)
        {
            Type type = head.ExpectedType;
            using CompilerContext ctx = new CompilerContext(type, true, true);
            using (Local typedVal = new Local(ctx, type))
            {
                ctx.LoadValue(Local.InputValue);
                ctx.CastFromObject(type);
                ctx.StoreValue(typedVal);
                CodeLabel[] jumpTable = new CodeLabel[4];
                for(int i = 0 ; i < jumpTable.Length ; i++) {
                    jumpTable[i] = ctx.DefineLabel();
                }
                ctx.LoadReaderWriter();
                ctx.Switch(jumpTable);
                ctx.Return();
                for(int i = 0 ; i < jumpTable.Length ; i++) {
                    ctx.MarkLabel(jumpTable[i]);
                    if (head.HasCallbacks((TypeModel.CallbackType)i))
                    {
                        head.EmitCallback(ctx, typedVal, (TypeModel.CallbackType)i);
                    }
                    ctx.Return();
                }                
            }
            
            ctx.Emit(OpCodes.Ret);
            return (ProtoCallback)ctx.method.CreateDelegate(
                typeof(ProtoCallback));
        }*/

        public static ProtoSubTypeDeserializer<T> BuildSubTypeDeserializer<T>(CompilerContextScope scope, IRuntimeProtoSerializerNode head, TypeModel model)
            where T : class
        {
            using CompilerContext ctx = new CompilerContext(scope, head.ExpectedType, SignatureType.ReaderScope_Input, true, model, typeof(SubTypeState<T>), typeof(T));
            head.EmitRead(ctx, ctx.InputValue);
            // note that EmitRead will unwrap the T for us on the stack
            ctx.Return();

            return (ProtoSubTypeDeserializer<T>)ctx.method.CreateDelegate(typeof(ProtoSubTypeDeserializer<T>));
        }

        public static ProtoDeserializer<T> BuildDeserializer<T>(CompilerContextScope scope, IRuntimeProtoSerializerNode head, TypeModel model, bool isScalar = false)
        {
            using CompilerContext ctx = new CompilerContext(scope, head.ExpectedType, SignatureType.ReaderScope_Input, true, model, typeof(T), typeof(T));

            head.EmitRead(ctx, ctx.InputValue);
            if (!isScalar) ctx.LoadValue(ctx.InputValue);
            ctx.Return();

            return (ProtoDeserializer<T>)ctx.method.CreateDelegate(typeof(ProtoDeserializer<T>));
        }

        public static Func<ISerializationContext, T> BuildFactory<T>(CompilerContextScope scope, IRuntimeProtoSerializerNode head, TypeModel model)
        {
            if (head is IProtoTypeSerializer pts && pts.ShouldEmitCreateInstance)
            {
                using var ctx = new CompilerContext(scope, head.ExpectedType, SignatureType.Context, true , model, typeof(ISerializationContext), typeof(T));
                pts.EmitCreateInstance(ctx, false);
                ctx.Return();

                return (Func<ISerializationContext, T>)ctx.method.CreateDelegate(typeof(Func<ISerializationContext, T>));
            }
            return null;
        }

        static readonly MethodInfo s_CreateInstance = typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.CreateInstance),
            BindingFlags.Public | BindingFlags.Instance);
        internal void CreateInstance<T>()
        {
            // call state.CreateInstance<T>(null)
            LoadState();
            LoadNullRef();
            EmitCall(s_CreateInstance.MakeGenericMethod(typeof(T)));
        }

        internal void Return()
        {
            Emit(OpCodes.Ret);
        }

        private static bool IsObject(Type type)
        {
            return type == typeof(object);
        }

        internal void CastToObject(Type type)
        {
            if (IsObject(type))
            { }
            else if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
                TraceCompile(OpCodes.Box + ": " + type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, typeof(object));
                TraceCompile(OpCodes.Castclass + ": " + type);
            }
        }

        internal void CastFromObject(Type type)
        {
            if (IsObject(type))
            { }
            else if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
                TraceCompile(OpCodes.Unbox_Any + ": " + type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
                TraceCompile(OpCodes.Castclass + ": " + type);
            }
        }

        internal bool NonPublic { get; }

        public Local InputValue { get; }

        internal CompilerContext(CompilerContext parent, ILGenerator il, bool isStatic, SignatureType signature, Type inputType, string traceName)
            : this(parent.Scope, il, isStatic, signature, parent.Model, inputType, traceName)
        { }

        internal void ThrowException(Type exceptionType) => il.ThrowException(exceptionType);

        
        internal CompilerContext(CompilerContextScope scope, ILGenerator il, bool isStatic, SignatureType signature,
            TypeModel model, Type inputType, string traceName)
        {
            Scope = scope;

            this.il = il ?? throw new ArgumentNullException(nameof(il));
            // NonPublic = false; <== implicit
            this.Model = model ?? throw new ArgumentNullException(nameof(model));
            if (inputType is not null) InputValue = new Local(null, inputType);
            TraceCompile(">> " + traceName);
            _traceName = traceName;
            IsStatic = isStatic;
            _signature = signature;
            GetOpCodes(signature, isStatic, out _state, out _inputArg);
        }

        public bool IsStatic { get; }

        public override string ToString() => _traceName;
        private readonly string _traceName;

        private readonly OpCode _state;
        private readonly byte _inputArg;

        private static void GetOpCodes(SignatureType signature, bool isStatic, out OpCode state, out byte inputArg)
        {
            switch (signature)
            {
                case SignatureType.ReaderScope_Input:
                case SignatureType.WriterScope_Input:
                    state = isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1;
                    inputArg = (byte)(isStatic ? 1 : 2);
                    break;
                default:
                    state = default;
                    inputArg = (byte)(isStatic ? 0 : 1);
                    break;
            }
            
        }

        internal enum SignatureType
        {
            WriterScope_Input,
            ReaderScope_Input,
            Context,
        }
        private readonly SignatureType _signature;

        internal CompilerContextScope Scope { get; }
        private CompilerContext(CompilerContextScope scope, Type associatedType, SignatureType signature, bool isStatic, TypeModel model, Type inputType, Type returnType)
        {
            Scope = scope;
            Model = model ?? throw new ArgumentNullException(nameof(model));
            NonPublic = true;
            _signature = signature;
            GetOpCodes(signature, isStatic, out _state, out _inputArg);
            var paramTypes = signature switch
            {
                SignatureType.ReaderScope_Input => new Type[] { StateBasedReadMethods.ByRefStateType, inputType },
                SignatureType.WriterScope_Input => new Type[] { WriterUtil.ByRefStateType, inputType },
                _ => new Type[] { inputType },
            };
            int uniqueIdentifier = Interlocked.Increment(ref next);
            method = new DynamicMethod("proto_" + uniqueIdentifier.ToString(CultureInfo.InvariantCulture), returnType ?? typeof(void), paramTypes,
                associatedType.IsInterface ? typeof(object) : associatedType, true);
            this.il = method.GetILGenerator();
            if (inputType is not null) InputValue = new Local(null, inputType);
            TraceCompile(">> " + method.Name);
            _traceName = method.Name;
            IsStatic = isStatic;
        }

        public bool IsService => Scope.IsFullEmit && !IsStatic;

        public void LoadSelfAsService<TService, T>(CompatibilityLevel compatibilityLevel, DataFormat dataFormat) where TService : class
        {
            var inbuilt = TypeModel.GetInbuiltSerializer<T>(compatibilityLevel, dataFormat);
            if (IsStatic || inbuilt is object) // don't claim inbuilts
            {
                if (inbuilt is object && typeof(TService) == typeof(ISerializer<T>) && !(inbuilt is PrimaryTypeProvider))
                {
                    // we'll get the call-site to emit TypeModel.GetInbuiltSerializer<T>(compatibilityLevel, dataFormat)
                    LoadValue((int)compatibilityLevel);
                    LoadValue((int)dataFormat);
                    EmitCall(s_GetInbuiltSerializer.MakeGenericMethod(typeof(T)));
                }
                else
                {
                    // no serializer (uses Level200 etc)
                    LoadNullRef();
                }
            }
            else
            {
                Emit(OpCodes.Ldarg_0); // push ourselves
                if (Scope.IsFullEmit && Scope.ImplementsServiceFor<T>(compatibilityLevel))
                { } // yay, we should be fine here
                else
                {
                    // otherwise, we'll use isinst to find
                    // out at runtime, else loading null
                    TryCast(typeof(TService));
                }
            }
        }

        private static readonly MethodInfo s_GetInbuiltSerializer = typeof(TypeModel).GetMethod(nameof(TypeModel.GetInbuiltSerializer), BindingFlags.Static | BindingFlags.Public);

        private readonly ILGenerator il;

        internal void Emit(OpCode opcode)
        {
            il.Emit(opcode);
            TraceCompile(opcode.ToString());
        }

        public void LoadValue(string value)
        {
            if (value is null)
            {
                LoadNullRef();
            }
            else
            {
                il.Emit(OpCodes.Ldstr, value);
                TraceCompile(OpCodes.Ldstr + ": " + value);
            }
        }

        public void LoadValue(float value)
        {
            il.Emit(OpCodes.Ldc_R4, value);
            TraceCompile(OpCodes.Ldc_R4 + ": " + value);
        }

        public void LoadValue(double value)
        {
            il.Emit(OpCodes.Ldc_R8, value);
            TraceCompile(OpCodes.Ldc_R8 + ": " + value);
        }

        public void LoadValue(ulong value)
            => LoadValue((long)value); // there is no ldc.u8

        public void LoadValue(long value)
        {
            il.Emit(OpCodes.Ldc_I8, value);
            TraceCompile(OpCodes.Ldc_I8 + ": " + value);
        }

        public void LoadValue(bool value)
        {
            Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
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
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                        TraceCompile(OpCodes.Ldc_I4_S + ": " + value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                        TraceCompile(OpCodes.Ldc_I4 + ": " + value);
                    }
                    break;
            }
        }

        private readonly List<LocalBuilder> locals = new List<LocalBuilder>();

        void IDisposable.Dispose() { }

        internal LocalBuilder GetFromPool(Type type)
        {
            int count = locals.Count;
            for (int i = 0; i < count; i++)
            {
                LocalBuilder item = locals[i];
                if (item is not null && item.LocalType == type)
                {
                    locals[i] = null; // remove from pool
                    return item;
                }
            }
            LocalBuilder result = il.DeclareLocal(type);
            TraceCompile("$ " + result + ": " + type);
            return result;
        }

        //
        internal void ReleaseToPool(LocalBuilder value)
        {
            int count = locals.Count;
            for (int i = 0; i < count; i++)
            {
                if (locals[i] is null)
                {
                    locals[i] = value; // released into existing slot
                    return;
                }
            }
            locals.Add(value); // create a new slot
        }

        public void LoadState() => Emit(_state);

        public void StoreValue(Local local)
        {
            if (local == this.InputValue)
            {
                il.Emit(OpCodes.Starg_S, _inputArg);
                TraceCompile(OpCodes.Starg_S + ": $" + _inputArg);
            }
            else if (local is null)
            {
                // just leave it on the top of the stack
            }
            else
            {
                switch (local.Value.LocalIndex)
                {
                    case 0: Emit(OpCodes.Stloc_0); break;
                    case 1: Emit(OpCodes.Stloc_1); break;
                    case 2: Emit(OpCodes.Stloc_2); break;
                    case 3: Emit(OpCodes.Stloc_3); break;
                    default:
                        OpCode code = UseShortForm(local) ? OpCodes.Stloc_S : OpCodes.Stloc;
                        il.Emit(code, local.Value);
                        TraceCompile(code + ": $" + local.Value);
                        break;
                }
            }
        }

        public void LoadValue(Local local)
        {
            if (local is null) { /* nothing to do; top of stack */}
            else if (local == this.InputValue)
            {
                switch(_inputArg)
                {
                    case 0: Emit(OpCodes.Ldarg_0); break;
                    case 1: Emit(OpCodes.Ldarg_1); break;
                    case 2: Emit(OpCodes.Ldarg_2); break;
                    case 3: Emit(OpCodes.Ldarg_3); break;
                    default:
                        il.Emit(OpCodes.Ldarg_S, _inputArg);
                        TraceCompile(OpCodes.Ldarg_S + ": $" + _inputArg);
                        break;
                }
            }
            else
            {
                switch (local.Value.LocalIndex)
                {
                    case 0: Emit(OpCodes.Ldloc_0); break;
                    case 1: Emit(OpCodes.Ldloc_1); break;
                    case 2: Emit(OpCodes.Ldloc_2); break;
                    case 3: Emit(OpCodes.Ldloc_3); break;
                    default:

                        OpCode code = UseShortForm(local) ? OpCodes.Ldloc_S : OpCodes.Ldloc;
                        il.Emit(code, local.Value);
                        TraceCompile(code + ": $" + local.Value);

                        break;
                }
            }
        }

        public Local GetLocalWithValue(Type type, Compiler.Local fromValue)
        {
            if (fromValue is not null)
            {
                if (fromValue.Type == type) return fromValue.AsCopy();
                // otherwise, load onto the stack and let the default handling (below) deal with it
                LoadValue(fromValue);
                if (!type.IsValueType && (fromValue.Type is null || !type.IsAssignableFrom(fromValue.Type)))
                { // need to cast
                    Cast(type);
                }
            }
            // need to store the value from the stack
            Local result = new Local(this, type);
            StoreValue(result);
            return result;
        }

        internal static class StateBasedReadMethods
        {
            internal static readonly Type ByRefStateType = typeof(ProtoReader.State).MakeByRefType();
            private static readonly Hashtable s_perTypeCache = new Hashtable();
            private static Dictionary<string, MethodInfo> CreateAndAdd(Type parentType)
            {
                var lookup = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
                foreach (var method in parentType.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.IsDefined(typeof(ObsoleteAttribute), true)) continue;

                    var args = method.GetParameters();
                    if (method.IsStatic)
                    {
                        if (args.Length != 1) continue;
                        if (args[0].ParameterType != ByRefStateType) continue;
                    }
                    else
                    {
                        if (args.Length != 0) continue;
                    }
                    lookup.Add(method.Name, method);
                }
                lock(s_perTypeCache)
                {
                    s_perTypeCache[parentType] = lookup;
                }
                return lookup;
            }

            internal static bool Find(Type parentType, string methodName, out MethodInfo method)
            {
                var lookup = ((Dictionary<string, MethodInfo>)s_perTypeCache[parentType]) ?? CreateAndAdd(parentType);

                return lookup.TryGetValue(methodName, out method);
            }
        }

        internal void EmitStateBasedRead(string methodName, Type expectedType)
            => EmitStateBasedRead(typeof(ProtoReader.State), methodName, expectedType);
        internal void EmitStateBasedRead(Type ownerType, string methodName, Type expectedType)
        {
            if (!StateBasedReadMethods.Find(ownerType, methodName, out var method))
            {
                throw new ArgumentException($"No suitable '{methodName}' method found on {ownerType.Name}");
            }
            if (method.ReturnType != expectedType)
            {
                throw new ArgumentException($"Method '{methodName}' has wrong return type; got {method.ReturnType.Name}, expected {expectedType.Name}");
            }
            LoadState();
            EmitCall(method);
        }

        internal void EmitStateBasedWrite(string methodName, Local fromValue, Type type = null, Type argType = null)
        {
            if (string.IsNullOrEmpty(methodName)) throw new ArgumentNullException(nameof(methodName));
            type ??= typeof(ProtoWriter.State);

            Type foundType;
            MethodInfo foundMethod;
            try
            {
                var found = (from method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                         where method.Name == methodName && !method.IsGenericMethodDefinition
                         && method.ReturnType == typeof(void)
                         let args = method.GetParameters()
                         where args.Length == (method.IsStatic ? 2 : 1)
                         && (!method.IsStatic || args[0].ParameterType == WriterUtil.ByRefStateType)
                         let paramType = args[method.IsStatic ? 1 : 0].ParameterType
                         where argType is null || argType == paramType // if argType specified: must match
                         select new { Method = method, Type = paramType }).Single();
                foundType = found.Type;
                foundMethod = found.Method;
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Unable to uniquely resolve {type.Name}.{methodName}", ex);
            }
            using var tmp = GetLocalWithValue(foundType, fromValue);
            LoadState();
            LoadValue(tmp);
            EmitCall(foundMethod);
        }

        public void EmitCall(MethodInfo method) { EmitCall(method, null); }

        public void EmitCall(MethodInfo method, Type targetType)
        {
            MemberInfo member = method ?? throw new ArgumentNullException(nameof(method));
            CheckAccessibility(ref member);
            OpCode opcode;
            Debug.Assert(method is MethodBuilder || !method.IsDefined(typeof(ObsoleteAttribute), true), "calling an obsolete method: " + method.Name);
            if (method.IsStatic || method.DeclaringType.IsValueType)
            {
                opcode = OpCodes.Call;
            }
            else
            {
                opcode = OpCodes.Callvirt;
                if (targetType is not null && targetType.IsValueType && !method.DeclaringType.IsValueType)
                {
                    Constrain(targetType);
                }
            }
            il.EmitCall(opcode, method, null);
            TraceCompile(opcode + ": " + method + " on " + method.DeclaringType + (targetType is null ? "" : (" via " + targetType)));
        }

        /// <summary>
        /// Pushes a null reference onto the stack. Note that this should only
        /// be used to return a null (or set a variable to null); for null-tests
        /// use BranchIfTrue / BranchIfFalse.
        /// </summary>
        public void LoadNullRef()
        {
            Emit(OpCodes.Ldnull);
        }

        private int nextLabel;

        internal void WriteNullCheckedTail(Type type, IRuntimeProtoSerializerNode tail, Compiler.Local valueFrom)
        {
            if (tail is TagDecorator td && td.ExpectedType == type && td.CanEmitDirectWrite())
            {
                td.EmitDirectWrite(this, valueFrom);
            }
            else if (type.IsValueType)
            {
                Type underlyingType = Nullable.GetUnderlyingType(type);

                if (underlyingType is null)
                { // not a nullable T; can invoke directly
                    tail.EmitWrite(this, valueFrom);
                }
                else
                { // nullable T; check HasValue
                    using Compiler.Local valOrNull = GetLocalWithValue(type, valueFrom);
                    LoadAddress(valOrNull, type);
                    LoadValue(type.GetProperty("HasValue"));
                    CodeLabel @end = DefineLabel();
                    BranchIfFalse(@end, false);
                    LoadAddress(valOrNull, type);
                    EmitCall(type.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                    tail.EmitWrite(this, null);
                    MarkLabel(@end);
                }
            }
            else
            { // ref-type; do a null-check
                LoadValue(valueFrom);
                CopyValue();
                CodeLabel hasVal = DefineLabel(), @end = DefineLabel();
                BranchIfTrue(hasVal, true);
                DiscardValue();
                Branch(@end, false);
                MarkLabel(hasVal);
                tail.EmitWrite(this, null);
                MarkLabel(@end);
            }
        }

        internal void ReadNullCheckedTail(Type type, IRuntimeProtoSerializerNode tail, Compiler.Local valueFrom)
        {
            Type underlyingType;
            if (type.IsValueType && type != tail.ExpectedType && (underlyingType = Nullable.GetUnderlyingType(type)) is not null
                && underlyingType == tail.ExpectedType)
            {
                if (tail.RequiresOldValue)
                {
                    // we expect the input value to be in valueFrom; need to unpack it from T?
                    using Local loc = GetLocalWithValue(type, valueFrom);
                    LoadAddress(loc, type);
                    EmitCall(type.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                }
                else
                {
                    Debug.Assert(valueFrom is null); // not expecting a valueFrom in this case
                }
                tail.EmitRead(this, null); // either unwrapped on the stack or not provided
                if (tail.ReturnsValue)
                {
                    // now re-wrap the value
                    EmitCtor(type, underlyingType);
                }
                return;
            }

            // either a ref-type of a non-nullable struct; treat "as is", even if null
            // (the type-serializer will handle the null case; it needs to allow null
            // inputs to perform the correct type of subclass creation)
            tail.EmitRead(this, valueFrom);
        }

        public void EmitCtor(Type type)
        {
            EmitCtor(type, Type.EmptyTypes);
        }

        public void EmitCtor(ConstructorInfo ctor)
        {
            if (ctor is null) throw new ArgumentNullException(nameof(ctor));
            MemberInfo ctorMember = ctor;
            CheckAccessibility(ref ctorMember);
            il.Emit(OpCodes.Newobj, ctor);
            TraceCompile(OpCodes.Newobj + ": " + ctor.DeclaringType);
        }

        public void InitLocal(Type type, Compiler.Local target)
        {
            LoadAddress(target, type, evenIfClass: true); // for class, initobj is a load-null, store-indirect
            il.Emit(OpCodes.Initobj, type);
            TraceCompile(OpCodes.Initobj + ": " + type);
        }

        internal ILGenerator IL => il;

        public void EmitCtor(Type type, params Type[] parameterTypes)
        {
            Debug.Assert(type is not null);
            Debug.Assert(parameterTypes is not null);
            if (type.IsValueType && parameterTypes.Length == 0)
            {
                il.Emit(OpCodes.Initobj, type);
                TraceCompile(OpCodes.Initobj + ": " + type);
            }
            else
            {
                ConstructorInfo ctor = Helpers.GetConstructor(type, parameterTypes, true) ?? throw new InvalidOperationException("No suitable constructor found for " + type.FullName);
                EmitCtor(ctor);
            }
        }

        private List<Assembly> knownTrustedAssemblies, knownUntrustedAssemblies;

        private bool InternalsVisible(Assembly assembly)
        {
            if (string.IsNullOrEmpty(Scope.AssemblyName)) return false;
            if (knownTrustedAssemblies is not null)
            {
                if (knownTrustedAssemblies.IndexOf(assembly) >= 0)
                {
                    return true;
                }
            }
            if (knownUntrustedAssemblies is not null)
            {
                if (knownUntrustedAssemblies.IndexOf(assembly) >= 0)
                {
                    return false;
                }
            }
            bool isTrusted = false;
            Type attributeType = typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute);
            if (attributeType is null) return false;
            foreach (System.Runtime.CompilerServices.InternalsVisibleToAttribute attrib in assembly.GetCustomAttributes(attributeType, false))
            {
                if (attrib.AssemblyName == Scope.AssemblyName || attrib.AssemblyName.StartsWith(Scope.AssemblyName + ",", StringComparison.Ordinal))
                {
                    isTrusted = true;
                    break;
                }
            }

            if (isTrusted)
            {
                (knownTrustedAssemblies ??= new List<Assembly>()).Add(assembly);
            }
            else
            {
                (knownUntrustedAssemblies ??= new List<Assembly>()).Add(assembly);
            }
            return isTrusted;
        }

        internal void CheckAccessibility(ref MemberInfo member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }
            Type type;
            if (!NonPublic)
            {
                if (member is FieldInfo && (member.Name.StartsWith("<", StringComparison.Ordinal) & member.Name.EndsWith(">k__BackingField", StringComparison.Ordinal)))
                {
                    var propName = member.Name.Substring(1, member.Name.Length - 17);
                    var prop = member.DeclaringType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    if (prop is not null) member = prop;
                }
                bool isPublic;
                MemberTypes memberType = member.MemberType;
                switch (memberType)
                {
                    case MemberTypes.TypeInfo:
                        // top-level type
                        type = (Type)member;
                        isPublic = type.IsPublic || InternalsVisible(type.Assembly);
                        break;
                    case MemberTypes.NestedType:
                        type = (Type)member;
                        do
                        {
                            isPublic = type.IsNestedPublic || type.IsPublic || ((type.DeclaringType is null || type.IsNestedAssembly || type.IsNestedFamORAssem) && InternalsVisible(type.Assembly));
                        } while (isPublic && (type = type.DeclaringType) is not null); // ^^^ !type.IsNested, but not all runtimes have that
                        break;
                    case MemberTypes.Field:
                        FieldInfo field = ((FieldInfo)member);
                        isPublic = field.IsPublic || ((field.IsAssembly || field.IsFamilyOrAssembly) && InternalsVisible(field.DeclaringType.Assembly));
                        break;
                    case MemberTypes.Constructor:
                        ConstructorInfo ctor = ((ConstructorInfo)member);
                        isPublic = ctor.IsPublic || ((ctor.IsAssembly || ctor.IsFamilyOrAssembly) && InternalsVisible(ctor.DeclaringType.Assembly));
                        break;
                    case MemberTypes.Method:
                        MethodInfo method = ((MethodInfo)member);
                        isPublic = method.IsPublic || ((method.IsAssembly || method.IsFamilyOrAssembly) && InternalsVisible(method.DeclaringType.Assembly));
                        if (!isPublic)
                        {
                            // allow calls to TypeModel protected methods, and methods we are in the process of creating
                            if (
                                member is MethodBuilder
                                || member.DeclaringType == typeof(TypeModel))
                            {
                                isPublic = true;
                            }
                        }
                        break;
                    case MemberTypes.Property:
                        isPublic = true; // defer to get/set
                        break;
                    default:
                        throw new NotSupportedException(memberType.ToString());
                }
                if (!isPublic)
                {
                    switch (member)
                    {
                        case FieldBuilder:
                        case TypeBuilder:
                        case PropertyBuilder:
                            // we're building them; 'tis fine
                            break;
                        default:
                            throw memberType switch
                            {
                                MemberTypes.TypeInfo or MemberTypes.NestedType =>
                                    new InvalidOperationException("Non-public type cannot be used with full dll compilation: " + ((Type)member).NormalizeName()),
                                _ =>
                                    new InvalidOperationException("Non-public member cannot be used with full dll compilation: " + member.DeclaringType.NormalizeName() + "." + member.Name),
                            };
                    }
                }
            }
        }

        public void LoadValue(FieldInfo field, bool checkAccessibility = true)
        {
            MemberInfo member = field;
            if (checkAccessibility) CheckAccessibility(ref member);
            if (member is PropertyInfo prop)
            {
                LoadValue(prop);
            }
            else
            {
                OpCode code = field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
                il.Emit(code, field);
                TraceCompile(code + ": " + field + " on " + field.DeclaringType);
            }
        }

        public void StoreValue(FieldInfo field)
        {
            MemberInfo member = field;
            CheckAccessibility(ref member);
            if (member is PropertyInfo prop)
            {
                StoreValue(prop);
            }
            else
            {
                OpCode code = field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld;
                il.Emit(code, field);
                TraceCompile(code + ": " + field + " on " + field.DeclaringType);
            }
        }

        public void LoadValue(PropertyInfo property)
        {
            MemberInfo member = property;
            CheckAccessibility(ref member);
            EmitCall(Helpers.GetGetMethod(property, true, true));
        }

        public void StoreValue(PropertyInfo property)
        {
            MemberInfo member = property;
            CheckAccessibility(ref member);
            EmitCall(Helpers.GetSetMethod(property, true, true));
        }

        //internal void EmitInstance()
        //{
        //    if (isStatic) throw new InvalidOperationException();
        //    Emit(OpCodes.Ldarg_0);
        //}

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
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        private static bool UseShortForm(Local local)
            => local.Value.LocalIndex < 256;

        internal void LoadAddress(Local local, Type type, bool evenIfClass = false)
        {
            if (evenIfClass || type.IsValueType)
            {
                if (local is null)
                {
                    throw new InvalidOperationException("Cannot load the address of the head of the stack");
                }

                if (local == this.InputValue)
                {
                    il.Emit(OpCodes.Ldarga_S, _inputArg);
                    TraceCompile(OpCodes.Ldarga_S + ": $" + _inputArg);
                }
                else
                {
                    OpCode code = UseShortForm(local) ? OpCodes.Ldloca_S : OpCodes.Ldloca;
                    il.Emit(code, local.Value);
                    TraceCompile(code + ": $" + local.Value);
                }
            }
            else
            {   // reference-type; already *is* the address; just load it
                LoadValue(local);
            }
        }

        internal void Branch(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Br_S : OpCodes.Br;
            il.Emit(code, label.Value);
            TraceCompile(code + ": " + label.Index);
        }

        internal void BranchIfFalse(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Brfalse_S : OpCodes.Brfalse;
            il.Emit(code, label.Value);
            TraceCompile(code + ": " + label.Index);
        }

        internal void BranchIfTrue(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Brtrue_S : OpCodes.Brtrue;
            il.Emit(code, label.Value);
            TraceCompile(code + ": " + label.Index);
        }

        internal void BranchIfEqual(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Beq_S : OpCodes.Beq;
            il.Emit(code, label.Value);
            TraceCompile(code + ": " + label.Index);
        }

        //internal void TestEqual()
        //{
        //    Emit(OpCodes.Ceq);
        //}

        internal void CopyValue()
        {
            Emit(OpCodes.Dup);
        }

        internal void BranchIfGreater(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Bgt_S : OpCodes.Bgt;
            il.Emit(code, label.Value);
            TraceCompile(code + ": " + label.Index);
        }

        internal void BranchIfLess(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Blt_S : OpCodes.Blt;
            il.Emit(code, label.Value);
            TraceCompile(code + ": " + label.Index);
        }

        internal void DiscardValue()
        {
            Emit(OpCodes.Pop);
        }

        public void Subtract()
        {
            Emit(OpCodes.Sub);
        }

        public void Switch(CodeLabel[] jumpTable)
        {
            const int MAX_JUMPS = 128;

            if (jumpTable.Length <= MAX_JUMPS)
            {
                // simple case
                Label[] labels = new Label[jumpTable.Length];
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i] = jumpTable[i].Value;
                }
                TraceCompile(OpCodes.Switch.ToString());
                il.Emit(OpCodes.Switch, labels);
            }
            else
            {
                // too many to jump easily (especially on Android) - need to split up (note: uses a local pulled from the stack)
                using Local val = GetLocalWithValue(typeof(int), null);
                int count = jumpTable.Length, offset = 0;
                int blockCount = count / MAX_JUMPS;
                if ((count % MAX_JUMPS) != 0) blockCount++;

                Label[] blockLabels = new Label[blockCount];
                for (int i = 0; i < blockCount; i++)
                {
                    blockLabels[i] = il.DefineLabel();
                }
                CodeLabel endOfSwitch = DefineLabel();

                LoadValue(val);
                LoadValue(MAX_JUMPS);
                Emit(OpCodes.Div);
                TraceCompile(OpCodes.Switch.ToString());
                il.Emit(OpCodes.Switch, blockLabels);
                Branch(endOfSwitch, false);

                Label[] innerLabels = new Label[MAX_JUMPS];
                for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
                {
                    il.MarkLabel(blockLabels[blockIndex]);

                    int itemsThisBlock = Math.Min(MAX_JUMPS, count);
                    count -= itemsThisBlock;
                    if (innerLabels.Length != itemsThisBlock) innerLabels = new Label[itemsThisBlock];

                    int subtract = offset;
                    for (int j = 0; j < itemsThisBlock; j++)
                    {
                        innerLabels[j] = jumpTable[offset++].Value;
                    }
                    LoadValue(val);
                    if (subtract != 0) // switches are always zero-based
                    {
                        LoadValue(subtract);
                        Emit(OpCodes.Sub);
                    }
                    TraceCompile(OpCodes.Switch.ToString());
                    il.Emit(OpCodes.Switch, innerLabels);
                    if (count != 0)
                    { // force default to the very bottom
                        Branch(endOfSwitch, false);
                    }
                }
                Debug.Assert(count == 0, "Should use exactly all switch items");
                MarkLabel(endOfSwitch);
            }
        }

        internal void EndFinally()
        {
            il.EndExceptionBlock();
            TraceCompile("EndExceptionBlock");
        }

        internal void BeginFinally()
        {
            il.BeginFinallyBlock();
            TraceCompile("BeginFinallyBlock");
        }

        internal void EndTry(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Leave_S : OpCodes.Leave;
            il.Emit(code, label.Value);
            TraceCompile(code + ": " + label.Index);
        }

        internal CodeLabel BeginTry()
        {
            CodeLabel label = new CodeLabel(il.BeginExceptionBlock(), nextLabel++);
            TraceCompile("BeginExceptionBlock: " + label.Index);
            return label;
        }

        internal void Constrain(Type type)
        {
            il.Emit(OpCodes.Constrained, type);
            TraceCompile(OpCodes.Constrained + ": " + type);
        }

        internal void TryCast(Type type)
        {
            il.Emit(OpCodes.Isinst, type);
            TraceCompile(OpCodes.Isinst + ": " + type);
        }

        internal void Cast(Type type)
        {
            il.Emit(OpCodes.Castclass, type);
            TraceCompile(OpCodes.Castclass + ": " + type);
        }

        public IDisposable Using(Local local)
        {
            return new UsingBlock(this, local);
        }

        private sealed class UsingBlock : IDisposable
        {
            private Local local;
            private CompilerContext ctx;
            private CodeLabel label;

            /// <summary>
            /// <para>
            /// Creates a new "using" block (equivalent) around a variable;
            /// the variable must exist, and note that (unlike in C#) it is
            /// the variables *final* value that gets disposed. If you need
            /// *original* disposal, copy your variable first.
            /// </para>
            /// <para>
            /// It is the callers responsibility to ensure that the variable's
            /// scope fully-encapsulates the "using"; if not, the variable
            /// may be re-used (and thus re-assigned) unexpectedly.
            /// </para>
            /// </summary>
            public UsingBlock(CompilerContext ctx, Local local)
            {
                if (local is null) throw new ArgumentNullException(nameof(local));

                Type type = local.Type;
                // check if **never** disposable
                if ((type.IsValueType || type.IsSealed)
                    && !typeof(IDisposable).IsAssignableFrom(type))
                {
                    return; // nothing to do! easiest "using" block ever
                    // (note that C# wouldn't allow this as a "using" block,
                    // but we'll be generous and simply not do anything)
                }
                this.local = local;
                this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
                label = ctx.BeginTry();
            }
            public void Dispose()
            {
                if (local is null || ctx is null) return;

                ctx.EndTry(label, false);
                ctx.BeginFinally();
                Type disposableType = typeof(IDisposable);
                MethodInfo dispose = disposableType.GetMethod("Dispose");
                Type type = local.Type;
                // remember that we've already (in the .ctor) excluded the case
                // where it *cannot* be disposable
                if (type.IsValueType)
                {
                    ctx.LoadAddress(local, type);
                    ctx.Constrain(type);
                    ctx.EmitCall(dispose);
                }
                else
                {
                    Compiler.CodeLabel @null = ctx.DefineLabel();
                    if (disposableType.IsAssignableFrom(type))
                    {   // *known* to be IDisposable; just needs a null-check                            
                        ctx.LoadValue(local);
                        ctx.BranchIfFalse(@null, true);
                        ctx.LoadAddress(local, type);
                    }
                    else
                    {   // *could* be IDisposable; test via "as"
                        using Compiler.Local disp = new Compiler.Local(ctx, disposableType);
                        ctx.LoadValue(local);
                        ctx.TryCast(disposableType);
                        ctx.CopyValue();
                        ctx.StoreValue(disp);
                        ctx.BranchIfFalse(@null, true);
                        ctx.LoadAddress(disp, disposableType);
                    }
                    ctx.EmitCall(dispose);
                    ctx.MarkLabel(@null);
                }
                ctx.EndFinally();
                this.local = null;
                this.ctx = null;
                label = new CodeLabel(); // default
            }
        }

        internal void Add()
        {
            Emit(OpCodes.Add);
        }

        internal void LoadLength(Local arr, bool zeroIfNull)
        {
            Debug.Assert(arr.Type.IsArray && arr.Type.GetArrayRank() == 1);

            if (zeroIfNull)
            {
                Compiler.CodeLabel notNull = DefineLabel(), done = DefineLabel();
                LoadValue(arr);
                CopyValue(); // optimised for non-null case
                BranchIfTrue(notNull, true);
                DiscardValue();
                LoadValue(0);
                Branch(done, true);
                MarkLabel(notNull);
                Emit(OpCodes.Ldlen);
                Emit(OpCodes.Conv_I4);
                MarkLabel(done);
            }
            else
            {
                LoadValue(arr);
                Emit(OpCodes.Ldlen);
                Emit(OpCodes.Conv_I4);
            }
        }

        internal void CreateArray(Type elementType, Local length)
        {
            LoadValue(length);
            il.Emit(OpCodes.Newarr, elementType);
            TraceCompile(OpCodes.Newarr + ": " + elementType);
        }

        internal void LoadArrayValue(Local arr, Local i)
        {
            Type type = arr.Type;
            Debug.Assert(type.IsArray && arr.Type.GetArrayRank() == 1);
            type = type.GetElementType();
            Debug.Assert(type is not null, "Not an array: " + arr.Type.FullName);
            LoadValue(arr);
            LoadValue(i);
            switch (Helpers.GetTypeCode(type))
            {
                case ProtoTypeCode.SByte: Emit(OpCodes.Ldelem_I1); break;
                case ProtoTypeCode.Int16: Emit(OpCodes.Ldelem_I2); break;
                case ProtoTypeCode.Int32: Emit(OpCodes.Ldelem_I4); break;
                case ProtoTypeCode.Int64: Emit(OpCodes.Ldelem_I8); break;

                case ProtoTypeCode.Byte: Emit(OpCodes.Ldelem_U1); break;
                case ProtoTypeCode.UInt16: Emit(OpCodes.Ldelem_U2); break;
                case ProtoTypeCode.UInt32: Emit(OpCodes.Ldelem_U4); break;
                case ProtoTypeCode.UInt64: Emit(OpCodes.Ldelem_I8); break; // odd, but this is what C# does...

                case ProtoTypeCode.Single: Emit(OpCodes.Ldelem_R4); break;
                case ProtoTypeCode.Double: Emit(OpCodes.Ldelem_R8); break;
                default:
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Ldelema, type);
                        il.Emit(OpCodes.Ldobj, type);
                        TraceCompile(OpCodes.Ldelema + ": " + type);
                        TraceCompile(OpCodes.Ldobj + ": " + type);
                    }
                    else
                    {
                        Emit(OpCodes.Ldelem_Ref);
                    }

                    break;
            }
        }


        internal static void LoadValue(ILGenerator il, Type type)
        {
            il.Emit(OpCodes.Ldtoken, type);
            il.EmitCall(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)), null);
        }
        internal void LoadValue(Type type)
        {
            il.Emit(OpCodes.Ldtoken, type);
            TraceCompile(OpCodes.Ldtoken + ": " + type);
            EmitCall(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
        }

        internal void ConvertToInt32(ProtoTypeCode typeCode, bool uint32Overflow)
        {
            switch (typeCode)
            {
                case ProtoTypeCode.Byte:
                case ProtoTypeCode.SByte:
                case ProtoTypeCode.Int16:
                case ProtoTypeCode.UInt16:
                    Emit(OpCodes.Conv_I4);
                    break;
                case ProtoTypeCode.Int32:
                    break;
                case ProtoTypeCode.Int64:
                    Emit(OpCodes.Conv_Ovf_I4);
                    break;
                case ProtoTypeCode.UInt32:
                    Emit(uint32Overflow ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_Ovf_I4);
                    break;
                case ProtoTypeCode.UInt64:
                    Emit(OpCodes.Conv_Ovf_I4_Un);
                    break;
                default:
                    throw new InvalidOperationException("ConvertToInt32 not implemented for: " + typeCode.ToString());
            }
        }

        internal void ConvertFromInt32(ProtoTypeCode typeCode, bool uint32Overflow)
        {
            switch (typeCode)
            {
                case ProtoTypeCode.SByte: Emit(OpCodes.Conv_Ovf_I1); break;
                case ProtoTypeCode.Byte: Emit(OpCodes.Conv_Ovf_U1); break;
                case ProtoTypeCode.Int16: Emit(OpCodes.Conv_Ovf_I2); break;
                case ProtoTypeCode.UInt16: Emit(OpCodes.Conv_Ovf_U2); break;
                case ProtoTypeCode.Int32: break;
                case ProtoTypeCode.UInt32: Emit(uint32Overflow ? OpCodes.Conv_Ovf_U4 : OpCodes.Conv_U4); break;
                case ProtoTypeCode.Int64: Emit(OpCodes.Conv_I8); break;
                case ProtoTypeCode.UInt64: Emit(OpCodes.Conv_U8); break;
                default: throw new InvalidOperationException();
            }
        }

        internal void LoadValue(decimal value)
        {
            if (value == 0M)
            {
                LoadValue(typeof(decimal).GetField("Zero"));
            }
            else
            {
                int[] bits = decimal.GetBits(value);
                LoadValue(bits[0]); // lo
                LoadValue(bits[1]); // mid
                LoadValue(bits[2]); // hi
                LoadValue((int)(((uint)bits[3]) >> 31)); // isNegative (bool, but int for CLI purposes)
                LoadValue((bits[3] >> 16) & 0xFF); // scale (byte, but int for CLI purposes)

                EmitCtor(typeof(decimal), new Type[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) });
            }
        }

        internal void LoadValue(Guid value)
        {
            if (value == Guid.Empty)
            {
                LoadValue(typeof(Guid).GetField("Empty"));
            }
            else
            { // note we're adding lots of shorts/bytes here - but at the IL level they are I4, not I1/I2 (which barely exist)
                byte[] bytes = value.ToByteArray();
                int i = (bytes[0]) | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
                LoadValue(i);
                short s = (short)((bytes[4]) | (bytes[5] << 8));
                LoadValue(s);
                s = (short)((bytes[6]) | (bytes[7] << 8));
                LoadValue(s);
                for (i = 8; i <= 15; i++)
                {
                    LoadValue(bytes[i]);
                }
                EmitCtor(typeof(Guid), new Type[] { typeof(int), typeof(short), typeof(short),
                            typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte), typeof(byte) });
            }
        }

        //internal void LoadValue(bool value)
        //{
        //    Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        //}

        internal void LoadSerializationContext(Type asType) // old api = SerializationContext; new api = ISerializationContext
        {
            LoadState();
            switch (_signature)
            {
                case SignatureType.WriterScope_Input:
                    LoadValue(typeof(ProtoWriter.State).GetProperty(nameof(ProtoWriter.State.Context)));
                    break;
                case SignatureType.ReaderScope_Input:
                    LoadValue(typeof(ProtoReader.State).GetProperty(nameof(ProtoReader.State.Context)));
                    break;
                case SignatureType.Context:
                    LoadValue(InputValue);
                    break;
                default:
                    ThrowHelper.ThrowInvalidOperationException($"Cannot load context for {_signature}");
                    break;
            }
            if (asType == typeof(ISerializationContext))
            {
                // fine, done
            }
            else if (asType == typeof(SerializationContext))
            {
                EmitCall(typeof(SerializationContext).GetMethod(nameof(SerializationContext.AsSerializationContext)));
            }
            else if (asType == typeof(StreamingContext))
            {
                EmitCall(typeof(SerializationContext).GetMethod(nameof(SerializationContext.AsStreamingContext)));
            }
            else
            {
                ThrowHelper.ThrowArgumentException($"Unexpected context type: {asType.NormalizeName()}");
            }
        }

        internal bool AllowInternal(PropertyInfo property)
        {
            return NonPublic || InternalsVisible(property.DeclaringType.Assembly);
        }
    }
}