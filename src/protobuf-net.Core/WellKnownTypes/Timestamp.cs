using System;

namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    ///  A Timestamp represents a point in time independent of any time zone or local
    /// calendar, encoded as a count of seconds and fractions of seconds at
    /// nanosecond resolution. The count is relative to an epoch at UTC midnight on
    /// January 1, 1970, in the proleptic Gregorian calendar which extends the
    /// Gregorian calendar backwards to year one.
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Timestamp ")]
    public readonly struct Timestamp
    {
        /// <summary>
        /// Represents seconds of UTC time since Unix epoch
        /// </summary>
        [ProtoMember(1, Name = "seconds", DataFormat = DataFormat.Default)]
        public long Seconds { get; }

        /// <summary>
        /// Non-negative fractions of a second at nanosecond resolution.
        /// </summary>
        [ProtoMember(2, Name = "nanos", DataFormat = DataFormat.Default)]
        public int Nanoseconds { get; }

        /// <summary>Creates a new Duration with the supplied values</summary>
        public Timestamp(long seconds, int nanoseconds)
        {
            Seconds = seconds;
            Nanoseconds = nanoseconds;
        }

        /// <summary>Converts a DateTime to a Timestamp</summary>
        public Timestamp(DateTime value)
        {
            Seconds = WellKnownSerializer.ToDurationSeconds(value - TimestampEpoch, out var nanoseconds);
            Nanoseconds = nanoseconds;
        }

        /// <summary>Converts a Timestamp to a DateTime</summary>
        public DateTime AsDateTime() => TimestampEpoch.AddTicks(WellKnownSerializer.ToTicks(Seconds, Nanoseconds));

        /// <summary>Converts a Timestamp to a DateTime</summary>
        public static implicit operator DateTime(Timestamp value) => value.AsDateTime();

        /// <summary>Converts a DateTime to a Timestamp</summary>
        public static implicit operator Timestamp(DateTime value) => new Timestamp(value);

        /// <summary>
        /// The default value for dates that are following google.protobuf.Timestamp semantics
        /// </summary>
        private static readonly DateTime TimestampEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    }

    partial class WellKnownSerializer : IMessageSerializer<Timestamp>, IMessageSerializer<DateTime>
    {
        Timestamp IMessageSerializer<Timestamp>.Read(ref ProtoReader.State state, Timestamp value)
        {
            var duration = new Duration(value.Seconds, value.Nanoseconds);
            duration = ReadDuration(ref state, duration);
            return new Timestamp(duration.Seconds, duration.Nanoseconds);
        }

        void IMessageSerializer<Timestamp>.Write(ref ProtoWriter.State state, Timestamp value)
            => WriteSecondsNanos(ref state, value.Seconds, value.Nanoseconds);

        DateTime IMessageSerializer<DateTime>.Read(ref ProtoReader.State state, DateTime value)
            => BclHelpers.ReadDateTime(ref state);

        void IMessageSerializer<DateTime>.Write(ref ProtoWriter.State state, DateTime value)
        {
            var model = state.Model;
            if (model != null && state.Model.SerializeDateTimeKind())
            {
                BclHelpers.WriteDateTimeWithKind(ref state, value);
            }
            else
            {
                BclHelpers.WriteDateTime(ref state, value);
            }
        }
    }
}
