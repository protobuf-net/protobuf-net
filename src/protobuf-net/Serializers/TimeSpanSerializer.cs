using System;
using System.Diagnostics;

namespace ProtoBuf.Serializers
{
    internal sealed class TimeSpanSerializer : IRuntimeProtoSerializerNode
    {
        private static readonly Type expectedType = typeof(TimeSpan);
        private readonly bool wellKnown;
        public TimeSpanSerializer(DataFormat dataFormat)
        {
            wellKnown = dataFormat == DataFormat.WellKnown;
        }
        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            if (wellKnown)
            {
                return BclHelpers.ReadDuration(ref state);
            }
            else
            {
                Debug.Assert(value == null); // since replaces
                return BclHelpers.ReadTimeSpan(ref state);
            }
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            if (wellKnown)
            {
                BclHelpers.WriteDuration((TimeSpan)value, dest, ref state);
            }
            else
            {
                BclHelpers.WriteTimeSpan((TimeSpan)value, dest, ref state);
            }
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite<BclHelpers>(
                wellKnown ? nameof(BclHelpers.WriteDuration) : nameof(BclHelpers.WriteTimeSpan), valueFrom, this);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            if (wellKnown) ctx.LoadValue(entity);
            ctx.EmitStateBasedRead(typeof(BclHelpers),
                wellKnown ? nameof(BclHelpers.ReadDuration) : nameof(BclHelpers.ReadTimeSpan),
                ExpectedType);
        }
    }
}