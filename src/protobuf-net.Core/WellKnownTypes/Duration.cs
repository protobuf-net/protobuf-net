using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using ProtoBuf.WellKnownTypes;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Duration>, ISerializer<Duration?>,
        IMeasuringSerializer<Duration>, IMeasuringSerializer<Duration?>
    {
        // Arithmetic sizing for the well-known pair, which MeasureSecondsNanos already had; what
        // is new is exposing it through the interface, so a CALLER can ask instead of writing to a
        // counting writer. The generated AOT path is that caller: a surrogate whose serializer is
        // this one can now be measured, which is what keeps a NodaTime-style contract - and every
        // contract referencing it - on measure-first. See notes/gaps.md B42.
        //
        // Deliberately NOT accompanied by OptionTrySkipWritingWhenMeasuring: that flag is what
        // makes the CLASSIC engine use this (ProtoWriter.Measure tests both), and turning it on
        // here would speed up the control that classic-vs-raw comparisons are measured against.
        // Adding it is a separate decision on its own merits, not a side-effect of this.
        int IMeasuringSerializer<Duration>.Measure(ISerializationContext context, WireType wireType, Duration value)
            => MeasureSecondsNanos(value.Seconds, value.Nanoseconds, false);

        int IMeasuringSerializer<Duration?>.Measure(ISerializationContext context, WireType wireType, Duration? value)
        {
            var duration = value.GetValueOrDefault();
            return MeasureSecondsNanos(duration.Seconds, duration.Nanoseconds, false);
        }

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
            if (state.WireType == WireType.String)
            {
                // the resident fast path lives on State now: peek the canonical
                // two-varint shape, commit only on a complete match
                long seconds = value.Seconds;
                int nanos = value.Nanoseconds;
                if (state.TryReadWellKnownPairFast(ref seconds, ref nanos))
                {
                    return new Duration(seconds, nanos);
                }
            }
            return ReadDurationFallback(ref state, value);
        }

        private static Duration ReadDurationFallback(ref ProtoReader.State state, Duration value)
            => ReadRawSecondsNanosBody(ref state, value);

        /// <summary>
        /// The seconds/nanos loop over the raw surface, reading within the CURRENT scope (the
        /// caller frames it - ReadMessage on the stateful path, a self-framing raw wrapper on
        /// the generated path). Wire tolerance per field mirrors the stateful ReadInt64/ReadInt32
        /// this replaced: varint, fixed64 and fixed32 all accepted, anything else on a known
        /// field throws exactly as those reads did.
        /// </summary>
        internal static Duration ReadRawSecondsNanosBody(ref ProtoReader.State state, Duration value)
        {
            var seconds = value.Seconds;
            var nanos = value.Nanoseconds;
            uint tag = state.ReadRawTag();
            while (tag != 0)
            {
                switch (tag)
                {
                    case (1 << 3) | 0: seconds = unchecked((long)state.ReadRawVarint64()); break;
                    case (1 << 3) | 1: seconds = unchecked((long)state.ReadRawFixed64()); break;
                    case (1 << 3) | 5: seconds = unchecked((int)state.ReadRawFixed32()); break;
                    case (2 << 3) | 0: nanos = unchecked((int)state.ReadRawVarint32()); break;
                    case (2 << 3) | 5: nanos = unchecked((int)state.ReadRawFixed32()); break;
                    case (2 << 3) | 1: nanos = checked((int)unchecked((long)state.ReadRawFixed64())); break;
                    default:
                        if (state.IsScopeEnd(tag)) return new Duration(seconds, nanos);
                        if ((tag >> 3) is 1 or 2) state.ThrowUnexpectedWireType(tag);
                        state.SkipTag(tag);
                        break;
                }
                tag = state.ReadRawTag();
            }
            return new Duration(seconds, nanos);
        }

        void ISerializer<Duration>.Write(ref ProtoWriter.State state, Duration value)
            => WriteSecondsNanos(ref state, value.Seconds, value.Nanoseconds, false);

        internal static void WriteDuration(ref ProtoWriter.State state, Duration value)
            => WriteSecondsNanos(ref state, value.Seconds, value.Nanoseconds, false);

        internal static long ToDurationSeconds(long ticks, out int nanos, bool isTimestamp)
        {
            nanos = (int)(((ticks % TimeSpan.TicksPerSecond) * 1000000)
                / TimeSpan.TicksPerMillisecond);
            var seconds = ticks / TimeSpan.TicksPerSecond;
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
        /// <summary>
        /// The number of bytes <see cref="WriteSecondsNanos"/> emits as the message BODY, excluding
        /// the field header and length prefix. Beside the writer deliberately: the two must agree
        /// field-for-field, and adjacency is the cheapest way to keep that true.
        /// </summary>
        /// <remarks>
        /// Value-dependent in both fields, because each is omitted when zero — so a <c>default</c>
        /// <c>Duration</c>/<c>Timestamp</c> has an empty body. The normalisation has to run first
        /// and with the same <paramref name="isTimestamp"/> flag, since it is what decides the
        /// final pair; measuring the un-normalised values would disagree at every boundary.
        /// </remarks>
        internal static int MeasureSecondsNanos(long seconds, int nanos, bool isTimestamp)
        {
            NormalizeSecondsNanoseconds(ref seconds, ref nanos, isTimestamp);
            int len = 0;
            // one-byte tags: fields 1 and 2, both varint. A negative value sign-extends to the
            // ten-byte form, which MeasureInt64/MeasureInt32 already account for
            if (seconds != 0) len += 1 + ProtoWriter.MeasureInt64(seconds);
            if (nanos != 0) len += 1 + ProtoWriter.MeasureInt32(nanos);
            return len;
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
        public Duration(TimeSpan value) : this(value.Ticks) { }

        internal Duration(long ticks)
        {
            Seconds = PrimaryTypeProvider.ToDurationSeconds(ticks, out var nanoseconds, false);
            Nanoseconds = nanoseconds;
        }

        /// <summary>Converts a Duration to a TimeSpan</summary>
        public TimeSpan AsTimeSpan() => TimeSpan.FromTicks(ToTicks());

        internal long ToTicks() => PrimaryTypeProvider.ToTicks(Seconds, Nanoseconds);

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
