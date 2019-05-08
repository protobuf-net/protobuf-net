#if !NO_RUNTIME
using System;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal sealed class DateTimeSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(DateTime);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;
        bool IProtoSerializer.ReturnsValue => true;

        private readonly bool includeKind, wellKnown;

        public DateTimeSerializer(DataFormat dataFormat, ProtoBuf.Meta.TypeModel model)
        {
            wellKnown = dataFormat == DataFormat.WellKnown;
            includeKind = model?.SerializeDateTimeKind() == true;
        }

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            if (wellKnown)
            {
                return BclHelpers.ReadTimestamp(source, ref state);
            }
            else
            {
                Helpers.DebugAssert(value == null); // since replaces
                return BclHelpers.ReadDateTime(source, ref state);
            }
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            if (wellKnown)
                BclHelpers.WriteTimestamp((DateTime)value, dest, ref state);
            else if (includeKind)
                BclHelpers.WriteDateTimeWithKind((DateTime)value, dest, ref state);
            else
                BclHelpers.WriteDateTime((DateTime)value, dest, ref state);
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitWrite<BclHelpers>(
                wellKnown ? nameof(BclHelpers.WriteTimestamp)
                : includeKind ? nameof(BclHelpers.WriteDateTimeWithKind) : nameof(BclHelpers.WriteDateTime), valueFrom);
        }

        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            if (wellKnown) ctx.LoadValue(entity);
            ctx.EmitBasicRead<BclHelpers>(
                wellKnown ? nameof(BclHelpers.ReadTimestamp) : nameof(BclHelpers.ReadDateTime),
                ExpectedType);
        }
#endif

    }
}
#endif