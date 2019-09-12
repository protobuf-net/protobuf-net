using System;

namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// A Duration represents a signed, fixed-length span of time represented
    /// as a count of seconds and fractions of seconds at nanosecond
    /// resolution. It is independent of any calendar and concepts like "day"
    /// or "month". It is related to Timestamp in that the difference between
    /// two Timestamp values is a Duration and it can be added or subtracted
    /// from a Timestamp. 
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Duration")]
    internal readonly struct Duration
    {
        /// <summary>
        /// Signed seconds of the span of time.
        /// </summary>
        [ProtoMember(1, Name = "seconds", DataFormat = DataFormat.Default)]
        public long Seconds { get; }

        /// <summary>
        /// Signed fractions of a second at nanosecond resolution of the span of time.
        /// </summary>
        [ProtoMember(2, Name = "nanos", DataFormat = DataFormat.Default)]
        public int Nanoseconds { get; }

        public Duration(long seconds, int nanoseconds)
        {
            Seconds = seconds;
            Nanoseconds = nanoseconds;
        }

        public Duration(TimeSpan value)
        {
            Seconds = WellKnownSerializer.ToDurationSeconds(value, out var nanoseconds);
            Nanoseconds = nanoseconds;
        }

        public TimeSpan AsTimeSpan() => TimeSpan.FromTicks(WellKnownSerializer.ToTicks(Seconds, Nanoseconds));

        public static implicit operator TimeSpan(Duration value) => value.AsTimeSpan();
        public static implicit operator Duration(TimeSpan value) => new Duration(value);
    }

    partial class WellKnownSerializer : IProtoSerializer<Duration>
    {
        Duration IProtoSerializer<Duration, Duration>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Duration value)
            => ReadDuration(reader, ref state, value);

        private static Duration ReadDuration(ProtoReader reader, ref ProtoReader.State state, Duration value)
        {
            if (reader.WireType == WireType.String && state.RemainingInCurrent >= 20)
            {
                if (TryReadDurationFast(reader, ref state, ref value)) return value;
            }
            return ReadDurationFallback(reader, ref state, value);
        }

        private static bool TryReadDurationFast(ProtoReader source, ref ProtoReader.State state, ref Duration value)
        {
            int offset = state.OffsetInCurrent;
            var span = state.Span;
            int prefixLength = ProtoReader.State.ParseVarintUInt32(span, offset, out var len);
            offset += prefixLength;
            if (len == 0) return true;

            if ((prefixLength + len) > state.RemainingInCurrent) return false; // don't have entire submessage

            if (span[offset] != (1 << 3)) return false; // expected field 1
            var msgOffset = 1 + ProtoReader.State.TryParseUInt64Varint(span, 1 + offset, out var seconds);
            var nanos = value.Nanoseconds;
            if (msgOffset < len)
            {
                if (span[msgOffset++ + offset] != (2 << 3)) return false; // expected field 2
                msgOffset += ProtoReader.State.TryParseUInt64Varint(span, msgOffset + offset, out var tmp);
                nanos = (int)(long)tmp;
            }
            if (msgOffset != len) return false; // expected no more fields
            state.Skip(prefixLength + (int)len);
            source.Advance(prefixLength + len);

            value = new Duration((long)seconds, nanos);
            return true;
        }

        private static Duration ReadDurationFallback(ProtoReader reader, ref ProtoReader.State state, Duration value)
        {
            var seconds = value.Seconds;
            var nanos = value.Nanoseconds;
            int fieldNumber;

            while ((fieldNumber = reader.ReadFieldHeader(ref state)) > 0)
            {
                switch (fieldNumber)
                {
                    case 1:
                        seconds = reader.ReadInt64(ref state);
                        break;
                    case 2:
                        nanos = reader.ReadInt32(ref state);
                        break;
                    default:
                        reader.SkipField(ref state);
                        break;
                }
            }
            return new Duration(seconds, nanos);
        }

        void IProtoSerializer<Duration, Duration>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Duration value)
            => WriteSecondsNanos(writer, ref state, value.Seconds, value.Nanoseconds);

        internal static long ToDurationSeconds(TimeSpan value, out int nanos)
        {
            nanos = (int)(((value.Ticks % TimeSpan.TicksPerSecond) * 1000000)
                / TimeSpan.TicksPerMillisecond);
            return value.Ticks / TimeSpan.TicksPerSecond;
        }

        internal static long ToTicks(long seconds, int nanos)
        {
            long ticks = checked((seconds * TimeSpan.TicksPerSecond)
                + (nanos * TimeSpan.TicksPerMillisecond / 1000000));
            return ticks;
        }

        private static void WriteSecondsNanos(ProtoWriter writer, ref ProtoWriter.State state, long seconds, int nanos)
        {
            if (nanos < 0)
            {   // from Timestamp.proto:
                // "Negative second values with fractions must still have
                // non -negative nanos values that count forward in time."
                seconds--;
                nanos += 1000000000;
            }
            if (seconds != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt64(seconds, writer, ref state);
            }
            if (nanos != 0)
            {
                ProtoWriter.WriteFieldHeader(2, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32(nanos, writer, ref state);
            }
        }
    }
}
