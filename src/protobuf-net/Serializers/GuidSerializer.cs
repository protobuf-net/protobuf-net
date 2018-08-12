#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class GuidSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(Guid);

        public Type ExpectedType { get { return expectedType; } }

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            BclHelpers.WriteGuid((Guid)value, dest, ref state);
        }

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadGuid(source, ref state);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite<BclHelpers>(nameof(BclHelpers.WriteGuid), valueFrom);
        }

        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead<BclHelpers>(nameof(BclHelpers.ReadGuid), ExpectedType);
        }
#endif

    }
}
#endif