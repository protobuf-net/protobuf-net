#if !NO_RUNTIME
using System;



namespace ProtoBuf.Serializers
{
    sealed class GuidSerializer : IProtoSerializer
    {
        public Type ExpectedType { get { return typeof(Guid); } }
        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteGuid((Guid)value, dest);
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadGuid(source);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite(typeof(BclHelpers), "WriteGuid", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead(typeof(BclHelpers), "ReadGuid", ExpectedType);
        }
#endif

    }
}
#endif