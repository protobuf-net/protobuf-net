using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using ProtoBuf.WellKnownTypes;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Duration>, ISerializer<Duration?>
    {
        SerializerFeatures ISerializer<Duration>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        SerializerFeatures ISerializer<Duration?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;

        Duration? ISerializer<Duration?>.Read(ref ProtoReader.State state, Duration? value)
            => ((ISerializer<Duration>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<Duration?>.Write(ref ProtoWriter.State state, Duration? value)
            => ((ISerializer<Duration>)this).Write(ref state, value.Value);

        Duration ISerializer<Duration>.Read(ref ProtoReader.State state, Duration value)
            => ReadDuration(ref state, value);

        internal static Duration ReadDuration(ref ProtoReader.State state, Duration value)
        {
            if (state.WireType == WireType.String && state.RemainingInCurrent >= 20)
            {
                if (TryReadDurationFast(ref state, ref value)) return value;
            }
            return ReadDurationFallback(ref state, value);
        }

        private static bool TryReadDurationFast(ref ProtoReader.State state, ref Duration value)
        {
            int offset = state.OffsetInCurrent;
            var span = state.Span;
            int prefixLength = state.ParseVarintUInt32(span, offset, out var len);
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
            state.Advance(prefixLength + len);

            value = new Duration((long)seconds, nanos);
            return true;
        }

        private static Duration ReadDurationFallback(ref ProtoReader.State state, Duration value)
        {
            var seconds = value.Seconds;
            var nanos = value.Nanoseconds;
            int fieldNumber;

            while ((fieldNumber = state.ReadFieldHeader()) > 0)
            {
                switch (fieldNumber)
                {
                    case 1:
                        seconds = state.ReadInt64();
                        break;
                    case 2:
                        nanos = state.ReadInt32();
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return new Duration(seconds, nanos);
        }

        void ISerializer<Duration>.Write(ref ProtoWriter.State state, Duration value)
            => WriteSecondsNanos(ref state, value.Seconds, value.Nanoseconds, false);

        internal static void WriteDuration(ref ProtoWriter.State state, Duration value)
            => WriteSecondsNanos(ref state, value.Seconds, value.Nanoseconds, false);

        internal static long ToDurationSeconds(TimeSpan value, out int nanos, bool isTimestamp)
        {
            nanos = (int)(((value.Ticks % TimeSpan.TicksPerSecond) * 1000000)
                / TimeSpan.TicksPerMillisecond);
            var seconds = value.Ticks / TimeSpan.TicksPerSecond;
            NormalizeSecondsNanoseconds(ref seconds, ref nanos, isTimestamp);
            return seconds;
        }

        internal static long ToTicks(long seconds, int nanos)
        {
            long ticks = checked((seconds * TimeSpan.TicksPerSecond)
                + (nanos * TimeSpan.TicksPerMillisecond / 1000000));
            return ticks;
        }

        internal static void NormalizeSecondsNanoseconds(ref long seconds, ref int nanos, bool isTimestamp)
        {
            const int SECOND_NANOS = 1000000000;
            // normalize to -999,999,999 to +999,999,999 inclusive
            seconds += nanos / SECOND_NANOS;
            nanos %= SECOND_NANOS;

            if (isTimestamp)
            {
                if (nanos < 0)
                {   // from Timestamp.proto:
                    // "Negative second values with fractions must still have
                    // non -negative nanos values that count forward in time."
                    seconds--;
                    nanos += SECOND_NANOS;
                }
            }
            else
            {
                // from Duration.Proto
                // Durations less than one second are represented with a 0
                // `seconds` field and a positive or negative `nanos` field. For durations
                // of one second or more, a non-zero value for the `nanos` field must be
                // of the same sign as the `seconds` field.

                if (nanos < 0) // and we already know < 1s, because of first lines
                {
                    // can we save space by encoding it as a positive?
                    if (seconds >= 0)
                    {
                        // for 0 and 1, this has the effect of making the nanos +ve, which
                        // is probably cheaper; for > 1, it enforces the "same sign" requirement
                        seconds--;
                        nanos += SECOND_NANOS;
                    }
                }
                if (nanos > 0 && seconds < 0)
                {
                    nanos -= SECOND_NANOS;
                    seconds++;
                }
            }
        }
        private static void WriteSecondsNanos(ref ProtoWriter.State state, long seconds, int nanos, bool isTimestamp)
        {
            NormalizeSecondsNanoseconds(ref seconds, ref nanos, isTimestamp);
            if (seconds != 0)
            {
                state.WriteFieldHeader(1, WireType.Varint);
                state.WriteInt64(seconds);
            }
            if (nanos != 0)
            {
                state.WriteFieldHeader(2, WireType.Varint);
                state.WriteInt32(nanos);
            }
        }
    }
}
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
    [ProtoContract(Name = ".google.protobuf.Duration", Serializer = typeof(PrimaryTypeProvider), Origin = "google/protobuf/duration.proto")]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Duration
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

        /// <summary>Creates a new Duration with the supplied values</summary>
        public Duration(long seconds, int nanoseconds)
        {
            Seconds = seconds;
            Nanoseconds = nanoseconds;
        }

        /// <summary>Converts a TimeSpan to a Duration</summary>
        public Duration(TimeSpan value)
        {
            Seconds = PrimaryTypeProvider.ToDurationSeconds(value, out var nanoseconds, false);
            Nanoseconds = nanoseconds;
        }

        /// <summary>Converts a Duration to a TimeSpan</summary>
        public TimeSpan AsTimeSpan() => TimeSpan.FromTicks(PrimaryTypeProvider.ToTicks(Seconds, Nanoseconds));

        /// <summary>Converts a Duration to a TimeSpan</summary>
        public static implicit operator TimeSpan(Duration value) => value.AsTimeSpan();
        /// <summary>Converts a TimeSpan to a Duration</summary>
        public static implicit operator Duration(TimeSpan value) => new Duration(value);

        /// <summary>
        /// Applies .proto rules to ensure that this value is in the expected ranges
        /// </summary>
        public Duration Normalize()
        {
            var seconds = Seconds;
            var nanos = Nanoseconds;
            PrimaryTypeProvider.NormalizeSecondsNanoseconds(ref seconds, ref nanos, false);
            return new Duration(seconds, nanos);
        }
    }
}
