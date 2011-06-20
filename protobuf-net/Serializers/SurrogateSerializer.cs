#if !NO_RUNTIME
using System;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    sealed class SurrogateSerializer : IProtoTypeSerializer
    {
        bool IProtoTypeSerializer.HasCallbacks(ProtoBuf.Meta.TypeModel.CallbackType callbackType) { return false; }
#if FEAT_COMPILER
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, ProtoBuf.Meta.TypeModel.CallbackType callbackType) { }
#endif
        void IProtoTypeSerializer.Callback(object value, ProtoBuf.Meta.TypeModel.CallbackType callbackType, SerializationContext context) { }
        public bool ReturnsValue { get { return false; } }
        public bool RequiresOldValue { get { return true; } }
        public Type ExpectedType { get { return forType; } }
        private readonly Type forType, declaredType;
        private readonly MethodInfo toTail, fromTail;
        IProtoTypeSerializer rootTail;

        public SurrogateSerializer(Type forType, Type declaredType, IProtoTypeSerializer rootTail)
        {
            Helpers.DebugAssert(forType != null, "forType");
            Helpers.DebugAssert(declaredType != null, "declaredType");
            Helpers.DebugAssert(rootTail != null, "rootTail");
            Helpers.DebugAssert(rootTail.RequiresOldValue, "RequiresOldValue");
            Helpers.DebugAssert(!rootTail.ReturnsValue, "ReturnsValue");
            Helpers.DebugAssert(declaredType == rootTail.ExpectedType || declaredType.IsSubclassOf(rootTail.ExpectedType));
            this.forType = forType;
            this.declaredType = declaredType;
            this.rootTail = rootTail;
            toTail = GetConversion(true);
            fromTail = GetConversion(false);
        }
        public MethodInfo GetConversion(bool toTail)
        {
            Type surrogateType = declaredType, to = toTail ? surrogateType : forType;
            Type[] from = new Type[] {toTail ? forType : surrogateType};
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo op;
            if ((op = surrogateType.GetMethod("op_Implicit", flags, null, from, null)) != null && op.ReturnType == to) return op;
            if ((op = surrogateType.GetMethod("op_Explicit", flags, null, from, null)) != null && op.ReturnType == to) return op;
            if ((op = forType.GetMethod("op_Implicit", flags, null, from, null)) != null && op.ReturnType == to) return op;
            if ((op = forType.GetMethod("op_Explicit", flags, null, from, null)) != null && op.ReturnType == to) return op;
            throw new InvalidOperationException("No suitable conversion operator found fopr surrogate: " +
                forType.FullName + " / " + surrogateType.FullName);
        }

        public void Write(object value, ProtoWriter writer)
        {
            rootTail.Write(toTail.Invoke(null, new object[] { value }), writer);
        }
        public object Read(object value, ProtoReader source)
        {
            // convert the incoming value
            object[] args = { value };
            value = toTail.Invoke(null, args);
            
            // invoke the tail and convert the outgoing value
            args[0] = rootTail.Read(value, source);
            return fromTail.Invoke(null, args);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Helpers.DebugAssert(valueFrom != null); // don't support stack-head for this
            using (Compiler.Local converted = new Compiler.Local(ctx, declaredType)) // declare/re-use local
            {
                ctx.LoadValue(valueFrom); // load primary onto stack
                ctx.EmitCall(toTail); // static convert op, primary-to-surrogate
                ctx.StoreValue(converted); // store into surrogate local

                rootTail.EmitRead(ctx, converted); // downstream processing against surrogate local

                ctx.LoadValue(converted); // load from surrogate local
                ctx.EmitCall(fromTail);  // static convert op, surrogate-to-primary
                ctx.StoreValue(valueFrom); // store back into primary
            }
        }

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(toTail);
            rootTail.EmitWrite(ctx, null);
        }
#endif
    }
}
#endif