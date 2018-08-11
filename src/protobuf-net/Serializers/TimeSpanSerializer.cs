#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class TimeSpanSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(TimeSpan);
        private readonly bool wellKnown;
        public TimeSpanSerializer(DataFormat dataFormat)
        {
            wellKnown = dataFormat == DataFormat.WellKnown;
        }
        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            if (wellKnown)
            {
                return BclHelpers.ReadDuration(source, ref state);
            }
            else
            {
                Helpers.DebugAssert(value == null); // since replaces
                return BclHelpers.ReadTimeSpan(source, ref state);
            }
        }

        public void Write(object value, ProtoWriter dest)
        {
            if (wellKnown)
            {
                BclHelpers.WriteDuration((TimeSpan)value, dest);
            }
            else
            {
                BclHelpers.WriteTimeSpan((TimeSpan)value, dest);
            }
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite(typeof(BclHelpers),
                wellKnown ? nameof(BclHelpers.WriteDuration) : nameof(BclHelpers.WriteTimeSpan), valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            if (wellKnown) ctx.LoadValue(entity);
            ctx.EmitBasicRead<BclHelpers>(
                wellKnown ? nameof(BclHelpers.ReadDuration) : nameof(BclHelpers.ReadTimeSpan),
                ExpectedType);
        }
#endif

    }
}
#endif