using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class TimeSpanSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => false;
        private static TimeSpanSerializer s_Legacy, s_Duration;
        private static readonly Type expectedType = typeof(TimeSpan);
        private readonly bool _useDuration;

        public static TimeSpanSerializer Create(CompatibilityLevel compatibilityLevel)
            => compatibilityLevel >= CompatibilityLevel.Level240
            ? s_Duration ??= new TimeSpanSerializer(true)
            : s_Legacy ??= new TimeSpanSerializer(false);

        private TimeSpanSerializer(bool useDuration)
            => _useDuration = useDuration;

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            if (_useDuration)
            {
                return BclHelpers.ReadDuration(ref state);
            }
            else
            {
                Debug.Assert(value is null); // since replaces
                return BclHelpers.ReadTimeSpan(ref state);
            }
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            if (_useDuration)
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
                _useDuration ? nameof(BclHelpers.WriteDuration) : nameof(BclHelpers.WriteTimeSpan), valueFrom, typeof(BclHelpers));
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            if (_useDuration) ctx.LoadValue(entity);
            ctx.EmitStateBasedRead(typeof(BclHelpers),
                _useDuration ? nameof(BclHelpers.ReadDuration) : nameof(BclHelpers.ReadTimeSpan),
                ExpectedType);
        }
    }
}