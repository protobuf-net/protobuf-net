#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class DecimalSerializer : IProtoSerializer
    {
        static readonly Type expectedType = typeof(decimal);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return BclHelpers.ReadDecimal(ref state, source);
        }

        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteDecimal((decimal)value, dest);
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite(ctx.MapType(typeof(BclHelpers)), nameof(BclHelpers.WriteDecimal), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead<BclHelpers>(nameof(BclHelpers.ReadDecimal), ExpectedType);
        }
#endif

    }
}
#endif