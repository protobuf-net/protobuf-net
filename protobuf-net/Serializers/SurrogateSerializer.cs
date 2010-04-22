using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    sealed class SurrogateSerializer : IProtoTypeSerializer
    {
        bool IProtoTypeSerializer.HasCallbacks(ProtoBuf.Meta.TypeModel.CallbackType callbackType) { return false; }
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, ProtoBuf.Meta.TypeModel.CallbackType callbackType) { }
        void IProtoTypeSerializer.Callback(object value, ProtoBuf.Meta.TypeModel.CallbackType callbackType) { }
        public bool ReturnsValue { get { return false; } }
        public bool RequiresOldValue { get { return true; } }
        public Type ExpectedType { get { return forType; } }
        private readonly Type forType;
        private readonly MethodInfo toTail, fromTail;
        IProtoTypeSerializer tail;

        public SurrogateSerializer(Type forType, IProtoTypeSerializer tail)
        {
            Helpers.DebugAssert(forType != null, "forType");
            Helpers.DebugAssert(tail != null, "tail");
            Helpers.DebugAssert(tail.RequiresOldValue, "RequiresOldValue");
            Helpers.DebugAssert(!tail.ReturnsValue, "ReturnsValue");
            this.forType = forType;
            this.tail = tail;
            toTail = GetConversion(true);
            fromTail = GetConversion(false);
        }
        public MethodInfo GetConversion(bool toTail)
        {
            Type surrogateType = tail.ExpectedType, to = toTail ? surrogateType : forType;
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
            tail.Write(toTail.Invoke(null, new object[] { value }), writer);
        }
        public object Read(object value, ProtoReader source)
        {
            // convert the incoming value
            object[] args = { value };
            value = toTail.Invoke(null, args);
            
            // invoke the tail and convert the outgoing value
            args[0] = tail.Read(value, source);
            return fromTail.Invoke(null, args);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Helpers.DebugAssert(valueFrom != null);
            using (Compiler.Local converted = new Compiler.Local(ctx, tail.ExpectedType))
            {
                ctx.LoadValue(valueFrom);
                ctx.EmitCall(toTail);
                ctx.StoreValue(converted);

                tail.EmitRead(ctx, converted);

                ctx.LoadValue(converted);
                ctx.EmitCall(fromTail);
                ctx.StoreValue(valueFrom);
            }
        }
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(toTail);
            tail.EmitWrite(ctx, null);
        }
    }
}
