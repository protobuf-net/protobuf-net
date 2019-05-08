#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class DecimalSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(decimal);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadDecimal(source, ref state);
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            BclHelpers.WriteDecimal((decimal)value, dest, ref state);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite<BclHelpers>(nameof(BclHelpers.WriteDecimal), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitBasicRead<BclHelpers>(nameof(BclHelpers.ReadDecimal), ExpectedType);
        }
#endif

    }
}
#endif