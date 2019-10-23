using ProtoBuf.Meta;
using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class DateTimeSerializer : IRuntimeProtoSerializerNode
    {
        private static readonly Type expectedType = typeof(DateTime);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;
        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        private readonly bool includeKind, wellKnown;

        public DateTimeSerializer(DataFormat dataFormat, TypeModel model)
        {
            wellKnown = dataFormat == DataFormat.WellKnown;
            includeKind = model.HasOption(TypeModel.TypeModelOptions.IncludeDateTimeKind);
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            if (wellKnown)
            {
                return BclHelpers.ReadTimestamp(ref state);
            }
            else
            {
                Debug.Assert(value == null); // since replaces
                return BclHelpers.ReadDateTime(ref state);
            }
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            if (wellKnown)
                BclHelpers.WriteTimestamp(ref state, (DateTime)value);
            else if (includeKind)
                BclHelpers.WriteDateTimeWithKind(ref state, (DateTime)value);
            else
                BclHelpers.WriteDateTime(ref state, (DateTime)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(
                wellKnown ? nameof(BclHelpers.WriteTimestamp)
                : includeKind ? nameof(BclHelpers.WriteDateTimeWithKind) : nameof(BclHelpers.WriteDateTime), valueFrom, typeof(BclHelpers));
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            if (wellKnown) ctx.LoadValue(entity);
            ctx.EmitStateBasedRead(typeof(BclHelpers),
                wellKnown ? nameof(BclHelpers.ReadTimestamp) : nameof(BclHelpers.ReadDateTime),
                ExpectedType);
        }
    }
}