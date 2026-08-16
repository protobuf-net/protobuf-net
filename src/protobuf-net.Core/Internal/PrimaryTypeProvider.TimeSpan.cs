using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider :
        ISerializer<PrimaryTypeProvider.ScaledTicks>,
        ISerializer<TimeSpan>, ISerializer<TimeSpan?>,
        ISerializer<DateTime>, ISerializer<DateTime?>
    {
        SerializerFeatures ISerializer<DateTime>.Features=> SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;
        SerializerFeatures ISerializer<DateTime?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;

        SerializerFeatures ISerializer<TimeSpan>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;
        SerializerFeatures ISerializer<TimeSpan?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessageWrappedAtRoot;

        TimeSpan? ISerializer<TimeSpan?>.Read(ref ProtoReader.State state, TimeSpan? value)
            => ((ISerializer<TimeSpan>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<TimeSpan?>.Write(ref ProtoWriter.State state, TimeSpan? value)
            => ((ISerializer<TimeSpan>)this).Write(ref state, value.Value);

        DateTime? ISerializer<DateTime?>.Read(ref ProtoReader.State state, DateTime? value)
            => ((ISerializer<DateTime>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<DateTime?>.Write(ref ProtoWriter.State state, DateTime? value)
            => ((ISerializer<DateTime>)this).Write(ref state, value.Value);

        TimeSpan ISerializer<TimeSpan>.Read(ref ProtoReader.State state, TimeSpan value)
            => ((ISerializer<ScaledTicks>)this).Read(ref state, default).ToTimeSpan();

        void ISerializer<TimeSpan>.Write(ref ProtoWriter.State state, TimeSpan value)
            => ((ISerializer<ScaledTicks>)this).Write(ref state, new ScaledTicks(value, DateTimeKind.Unspecified));

        DateTime ISerializer<DateTime>.Read(ref ProtoReader.State state, DateTime value)
            => ((ISerializer<ScaledTicks>)this).Read(ref state, default).ToDateTime();

        void ISerializer<DateTime>.Write(ref ProtoWriter.State state, DateTime value)
        {
            var includeKind = state.Model.HasOption(TypeModel.TypeModelOptions.IncludeDateTimeKind);
            ((ISerializer<ScaledTicks>)this).Write(ref state, ScaledTicks.Create(value, includeKind));
        }

        void ISerializer<ScaledTicks>.Write(ref ProtoWriter.State state, ScaledTicks value)
        {
            if (value.Value != 0)
            {
                state.WriteFieldHeader(ScaledTicks.FieldTimeSpanValue, WireType.SignedVarint);
                state.WriteInt64(value.Value);
            }
            if (value.Scale != TimeSpanScale.Days)
            {
                state.WriteFieldHeader(ScaledTicks.FieldTimeSpanScale, WireType.Varint);
                state.WriteInt32((int)value.Scale);
            }
            if (value.Kind != DateTimeKind.Unspecified)
            {
                state.WriteFieldHeader(ScaledTicks.FieldTimeSpanKind, WireType.Varint);
                state.WriteInt32((int)value.Kind);
            }
        }

        SerializerFeatures ISerializer<ScaledTicks>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        ScaledTicks ISerializer<ScaledTicks>.Read(ref ProtoReader.State state, ScaledTicks _)
            => ReadRawScaledTicksBody(ref state);

        /// <summary>
        /// The bcl.TimeSpan loop over the raw surface, reading within the CURRENT scope (the
        /// caller frames it - ReadMessage on the stateful path, a self-framing raw wrapper on
        /// the generated path). Wire fidelity per field is the stateful read's: value was
        /// Assert(SignedVarint), so it is zigzag wire-0 ONLY (no fixed tolerance - the assert
        /// threw); scale and kind were ReadInt32, so varint/fixed32/fixed64 all serve, and the
        /// kind validation is unchanged.
        /// </summary>
        internal static ScaledTicks ReadRawScaledTicksBody(ref ProtoReader.State state)
        {
            TimeSpanScale scale = TimeSpanScale.Days;
            long value = 0;
            var kind = DateTimeKind.Unspecified;
            uint tag = state.ReadRawTag();
            while (tag != 0)
            {
                switch (tag)
                {
                    case (ScaledTicks.FieldTimeSpanValue << 3) | 0:
                        value = state.ReadRawZigZag64();
                        break;
                    case (ScaledTicks.FieldTimeSpanScale << 3) | 0:
                        scale = (TimeSpanScale)unchecked((int)state.ReadRawVarint32());
                        break;
                    case (ScaledTicks.FieldTimeSpanScale << 3) | 5:
                        scale = (TimeSpanScale)unchecked((int)state.ReadRawFixed32());
                        break;
                    case (ScaledTicks.FieldTimeSpanScale << 3) | 1:
                        scale = (TimeSpanScale)checked((int)unchecked((long)state.ReadRawFixed64()));
                        break;
                    case (ScaledTicks.FieldTimeSpanKind << 3) | 0:
                        kind = CheckKind((DateTimeKind)unchecked((int)state.ReadRawVarint32()));
                        break;
                    case (ScaledTicks.FieldTimeSpanKind << 3) | 5:
                        kind = CheckKind((DateTimeKind)unchecked((int)state.ReadRawFixed32()));
                        break;
                    case (ScaledTicks.FieldTimeSpanKind << 3) | 1:
                        kind = CheckKind((DateTimeKind)checked((int)unchecked((long)state.ReadRawFixed64())));
                        break;
                    default:
                        if (state.IsScopeEnd(tag)) return new ScaledTicks(value, scale, kind);
                        if ((tag >> 3) is ScaledTicks.FieldTimeSpanValue or ScaledTicks.FieldTimeSpanScale
                            or ScaledTicks.FieldTimeSpanKind)
                        {
                            state.ThrowUnexpectedWireType(tag);
                        }
                        state.SkipTag(tag);
                        break;
                }
                tag = state.ReadRawTag();
            }
            return new ScaledTicks(value, scale, kind);

            static DateTimeKind CheckKind(DateTimeKind kind)
            {
                switch (kind)
                {
                    case DateTimeKind.Unspecified:
                    case DateTimeKind.Utc:
                    case DateTimeKind.Local:
                        break; // fine
                    default:
                        ThrowHelper.ThrowProtoException("Invalid date/time kind: " + kind.ToString());
                        break;
                }
                return kind;
            }
        }

        [StructLayout(LayoutKind.Auto)]
        [ProtoContract(Name = ".bcl.TimeSpan")]
        internal readonly struct ScaledTicks
        {
            [ProtoMember(1, DataFormat = DataFormat.ZigZag, Name = "value")]
            public long Value { get; }
            [ProtoMember(2, Name = "scale")]
            public TimeSpanScale Scale { get; }
            [ProtoMember(3, Name = "kind")]
            public DateTimeKind Kind { get; }
            public ScaledTicks(long value, TimeSpanScale scale, DateTimeKind kind)
            {
                Value = value;
                Scale = scale;
                Kind = kind;
            }

            public static ScaledTicks Create(DateTime value, bool includeKind)
            {
                if (value == DateTime.MinValue) return new ScaledTicks(-1, TimeSpanScale.MinMax, DateTimeKind.Unspecified);
                if (value == DateTime.MaxValue) return new ScaledTicks(1, TimeSpanScale.MinMax, DateTimeKind.Unspecified);
                var kind = includeKind ? value.Kind : DateTimeKind.Unspecified;
                return new ScaledTicks(value - BclHelpers.EpochOrigin[(int)kind], kind);
            }

            public DateTime ToDateTime()
            {
                long tickDelta;
                switch (Scale)
                {
                    case TimeSpanScale.Days:
                        tickDelta = Value * TimeSpan.TicksPerDay;
                        break;
                    case TimeSpanScale.Hours:
                        tickDelta = Value * TimeSpan.TicksPerHour;
                        break;
                    case TimeSpanScale.Minutes:
                        tickDelta = Value * TimeSpan.TicksPerMinute;
                        break;
                    case TimeSpanScale.Seconds:
                        tickDelta = Value * TimeSpan.TicksPerSecond;
                        break;
                    case TimeSpanScale.Milliseconds:
                        tickDelta = Value * TimeSpan.TicksPerMillisecond;
                        break;
                    case TimeSpanScale.Ticks:
                        tickDelta = Value;
                        break;
                    case TimeSpanScale.MinMax:
                        switch (Value)
                        {
                            case 1: return DateTime.MaxValue;
                            case -1: return DateTime.MinValue;
                            default:
                                ThrowHelper.ThrowProtoException("Unknown min/max value: " + Value.ToString());
                                return default;
                        }
                    default:
                        ThrowHelper.ThrowProtoException("Unknown timescale: " + Scale.ToString());
                        return default;
                }
                return BclHelpers.EpochOrigin[(int)Kind].AddTicks(tickDelta);
            }

            internal ScaledTicks(TimeSpan timeSpan, DateTimeKind kind)
            {
                TimeSpanScale scale;
                long value = timeSpan.Ticks;
                if (timeSpan == TimeSpan.MaxValue)
                {
                    value = 1;
                    scale = TimeSpanScale.MinMax;
                }
                else if (timeSpan == TimeSpan.MinValue)
                {
                    value = -1;
                    scale = TimeSpanScale.MinMax;
                }
                else if (value % TimeSpan.TicksPerDay == 0)
                {
                    scale = TimeSpanScale.Days;
                    value /= TimeSpan.TicksPerDay;
                }
                else if (value % TimeSpan.TicksPerHour == 0)
                {
                    scale = TimeSpanScale.Hours;
                    value /= TimeSpan.TicksPerHour;
                }
                else if (value % TimeSpan.TicksPerMinute == 0)
                {
                    scale = TimeSpanScale.Minutes;
                    value /= TimeSpan.TicksPerMinute;
                }
                else if (value % TimeSpan.TicksPerSecond == 0)
                {
                    scale = TimeSpanScale.Seconds;
                    value /= TimeSpan.TicksPerSecond;
                }
                else if (value % TimeSpan.TicksPerMillisecond == 0)
                {
                    scale = TimeSpanScale.Milliseconds;
                    value /= TimeSpan.TicksPerMillisecond;
                }
                else
                {
                    scale = TimeSpanScale.Ticks;
                }

                Kind = kind;
                Value = value;
                Scale = scale;
            }


            public TimeSpan ToTimeSpan()
            {
                switch (Scale)
                {
                    case TimeSpanScale.Days:
                        return TimeSpan.FromDays(Value);
                    case TimeSpanScale.Hours:
                        return TimeSpan.FromHours(Value);
                    case TimeSpanScale.Minutes:
                        return TimeSpan.FromMinutes(Value);
                    case TimeSpanScale.Seconds:
                        return TimeSpan.FromSeconds(Value);
                    case TimeSpanScale.Milliseconds:
                        return TimeSpan.FromMilliseconds(Value);
                    case TimeSpanScale.Ticks:
                        return TimeSpan.FromTicks(Value);
                    case TimeSpanScale.MinMax:
                        switch (Value)
                        {
                            case 1: return TimeSpan.MaxValue;
                            case -1: return TimeSpan.MinValue;
                            default:
                                ThrowHelper.ThrowProtoException("Unknown min/max value: " + Value.ToString());
                                return default;
                        }
                    default:
                        ThrowHelper.ThrowProtoException("Unknown timescale: " + Scale.ToString());
                        return default;
                }
            }

            internal const int FieldTimeSpanValue = 0x01, FieldTimeSpanScale = 0x02, FieldTimeSpanKind = 0x03;
        }
    }
}
