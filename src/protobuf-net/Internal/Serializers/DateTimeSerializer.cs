using ProtoBuf.Meta;
using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class DateTimeSerializer : IRuntimeProtoSerializerNode
    {

        bool IRuntimeProtoSerializerNode.IsScalar => false;
        private static readonly Type expectedType = typeof(DateTime);
        private static DateTimeSerializer s_Timestamp;

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;
        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        private readonly bool _includeKind, _useTimestamp;

        public static DateTimeSerializer Create(CompatibilityLevel compatibilityLevel, TypeModel model)
            =>  compatibilityLevel >= CompatibilityLevel.Level240
                ? s_Timestamp ??= new DateTimeSerializer(true, false)
                : new DateTimeSerializer(false, model.HasOption(TypeModel.TypeModelOptions.IncludeDateTimeKind));

        private DateTimeSerializer(bool useTimestamp, bool includeKind)
        {
            _useTimestamp = useTimestamp;
            _includeKind = includeKind;
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            if (_useTimestamp)
            {
                return BclHelpers.ReadTimestamp(ref state);
            }
            else
            {
                Debug.Assert(value is null); // since replaces
                return BclHelpers.ReadDateTime(ref state);
            }
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            if (_useTimestamp)
                BclHelpers.WriteTimestamp(ref state, (DateTime)value);
            else if (_includeKind)
                BclHelpers.WriteDateTimeWithKind(ref state, (DateTime)value);
            else
                BclHelpers.WriteDateTime(ref state, (DateTime)value);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(
                _useTimestamp ? nameof(BclHelpers.WriteTimestamp)
                : _includeKind ? nameof(BclHelpers.WriteDateTimeWithKind) : nameof(BclHelpers.WriteDateTime), valueFrom, typeof(BclHelpers));
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            if (_useTimestamp) ctx.LoadValue(entity);
            ctx.EmitStateBasedRead(typeof(BclHelpers),
                _useTimestamp ? nameof(BclHelpers.ReadTimestamp) : nameof(BclHelpers.ReadDateTime),
                ExpectedType);
        }
    }
}