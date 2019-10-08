using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
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

        public object Read(ref ProtoReader.State state, object value)
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

        public void Write(ref ProtoWriter.State state, object value)
        {
            if (wellKnown)
            {
                BclHelpers.WriteDuration(ref state, (TimeSpan)value);
            }
            else
            {
                BclHelpers.WriteTimeSpan(ref state, (TimeSpan)value);
            }
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(
                wellKnown ? nameof(BclHelpers.WriteDuration) : nameof(BclHelpers.WriteTimeSpan), valueFrom, typeof(BclHelpers));
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