using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using ProtoBuf.WellKnownTypes;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Timestamp>, ISerializer<Timestamp?>
    {
        SerializerFeatures ISerializer<Timestamp>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        SerializerFeatures ISerializer<Timestamp?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        Timestamp ISerializer<Timestamp>.Read(ref ProtoReader.State state, Timestamp value)
        {
            var duration = new Duration(value.Seconds, value.Nanoseconds);
            duration = ReadDuration(ref state, duration);
            return new Timestamp(duration.Seconds, duration.Nanoseconds);
        }

        internal static Timestamp ReadTimestamp(ref ProtoReader.State state, Timestamp value)
        {
            var duration = new Duration(value.Seconds, value.Nanoseconds);
            duration = ReadDuration(ref state, duration);
            return new Timestamp(duration.Seconds, duration.Nanoseconds);
        }

        void ISerializer<Timestamp>.Write(ref ProtoWriter.State state, Timestamp value)
            => WriteSecondsNanos(ref state, value.Seconds, value.Nanoseconds, true);

        internal static void WriteTimestamp(ref ProtoWriter.State state, Timestamp value)
            => WriteSecondsNanos(ref state, value.Seconds, value.Nanoseconds, true);

        Timestamp? ISerializer<Timestamp?>.Read(ref ProtoReader.State state, Timestamp? value)
            => ((ISerializer<Timestamp>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<Timestamp?>.Write(ref ProtoWriter.State state, Timestamp? value)
            => ((ISerializer<Timestamp>)this).Write(ref state, value.Value);
    }
}
namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    ///  A Timestamp represents a point in time independent of any time zone or local
    /// calendar, encoded as a count of seconds and fractions of seconds at
    /// nanosecond resolution. The count is relative to an epoch at UTC midnight on
    /// January 1, 1970, in the proleptic Gregorian calendar which extends the
    /// Gregorian calendar backwards to year one.
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Timestamp", Serializer = typeof(PrimaryTypeProvider), Origin = "google/protobuf/timestamp.proto")]
    [StructLayout(LayoutKind.Auto)]
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
            Seconds = PrimaryTypeProvider.ToDurationSeconds(value - TimestampEpoch, out var nanoseconds, true);
            Nanoseconds = nanoseconds;
        }

        /// <summary>
        /// Applies .proto rules to ensure that this value is in the expected ranges
        /// </summary>
        public Timestamp Normalize()
        {
            var seconds = Seconds;
            var nanos = Nanoseconds;
            PrimaryTypeProvider.NormalizeSecondsNanoseconds(ref seconds, ref nanos, true);
            return new Timestamp(seconds, nanos);
        }

        /// <summary>Converts a Timestamp to a DateTime</summary>
        public DateTime AsDateTime() => TimestampEpoch.AddTicks(PrimaryTypeProvider.ToTicks(Seconds, Nanoseconds));

        /// <summary>Converts a Timestamp to a DateTime</summary>
        public static implicit operator DateTime(Timestamp value) => value.AsDateTime();

        /// <summary>Converts a DateTime to a Timestamp</summary>
        public static implicit operator Timestamp(DateTime value) => new Timestamp(value);

        /// <summary>
        /// The default value for dates that are following google.protobuf.Timestamp semantics
        /// </summary>
        private static readonly DateTime TimestampEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    }
}
