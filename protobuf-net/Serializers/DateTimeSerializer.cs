#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class DateTimeSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(DateTime); } }
        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteDateTime((DateTime)value, dest);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadDateTime(source);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite(typeof(BclHelpers), "WriteDateTime", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead(typeof(BclHelpers), "ReadDateTime", ExpectedType);
        }
#endif

    }
}
#endif