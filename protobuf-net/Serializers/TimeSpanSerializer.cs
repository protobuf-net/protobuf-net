#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class TimeSpanSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(TimeSpan); } }
        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteTimeSpan((TimeSpan)value, dest);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadTimeSpan(source);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite(typeof(BclHelpers), "WriteTimeSpan", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead(typeof(BclHelpers), "ReadTimeSpan", ExpectedType);
        }
#endif

    }
}
#endif