#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class DecimalSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(decimal); } }
        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteDecimal((decimal)value, dest);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadDecimal(source);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite(typeof(BclHelpers), "WriteDecimal", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead(typeof(BclHelpers), "ReadDecimal", ExpectedType);
        }
#endif

    }
}
#endif