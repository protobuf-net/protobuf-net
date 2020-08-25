using System;

namespace ProtoBuf.Meta // note: choice of API is deliberate; ProtoBuf.Meta causes lots of namespace confusion (global::NodaTime vs etc), and
{                       // anyone using this API should *already* have ProtoBuf.Meta, making this "just work"
    /// <summary>
    /// Provides extension APIs for protobuf-net's <see cref="RuntimeTypeModel"/>.
    /// </summary>
    public static class NodaTimeExtensions
    {
        // influenced by NodaTime.Serialization.Protobuf/NodaExtensions.cs ToProtobufDuration etc
        // and NodaTime.Serialization.Protobuf/ProtobufExtensions.cs ToNodaDuration etc

        /// <summary>
        /// Registers protobuf-net serialization surrogates for all supported NodaTime primitives.
        /// </summary>
        /// <param name="model">The model to extend (<see cref="RuntimeTypeModel.Default"/> is assumed if it is <c>null</c>)</param>.
        /// <returns>The model instance.</returns>
        public static RuntimeTypeModel AddNodaTime(this RuntimeTypeModel model)
        {
            model ??= RuntimeTypeModel.Default;
            // use surrogates for Duration and Instant - we already have good support for these
            model.SetSurrogate<NodaTime.Duration, WellKnownTypes.Duration>(ToProtoBufDuration, ToNodaTimeDuration)
                .SetSurrogate<NodaTime.Instant, WellKnownTypes.Timestamp>(ToProtoBufTimestamp, ToNodaTimeInstant);

            // the enum has matching values; can just configure the names
            Add(model, typeof(NodaTime.IsoDayOfWeek), ".google.type.DayOfWeek", "google/type/dayofweek.proto", null);

            // use custom serializer for LocalTime/LocalDate
            Add(model, typeof(NodaTime.LocalDate), ".google.type.Date", "google/type/date.proto", typeof(NodaTimeSerializers));
            Add(model, typeof(NodaTime.LocalTime), ".google.type.TimeOfDay", "google/type/timeofday.proto", typeof(NodaTimeSerializers));
            return model;

            static void Add(RuntimeTypeModel model, Type type, string name, string origin, Type serializerType)
            {
                var mt = model.Add(type, true);
                if (name is object) mt.Name = name;
                if (origin is object) mt.Origin = origin;
                if (serializerType is object) mt.SerializerType = serializerType;
            }
        }

        /// <summary>
        /// Converts a NodaTime <see cref="NodaTime.Duration"/> to a protobuf-net <see cref="WellKnownTypes.Duration"/>.
        /// </summary>
        public static WellKnownTypes.Duration ToProtoBufDuration(NodaTime.Duration value)
        {   
            // Deliberately long to keep the later arithmetic in 64-bit.
            long days = value.Days;
            long nanoOfDay = value.NanosecondOfDay;
            long secondOfDay = nanoOfDay / NodaTime.NodaConstants.NanosecondsPerSecond;
            int nanos = value.SubsecondNanoseconds;
            return new WellKnownTypes.Duration(seconds: days * NodaTime.NodaConstants.SecondsPerDay + secondOfDay, nanoseconds: nanos);
        }

        /// <summary>
        /// Converts a protobuf-net <see cref="WellKnownTypes.Duration"/> to a NodaTime <see cref="NodaTime.Duration"/>.
        /// </summary>
        public static NodaTime.Duration ToNodaTimeDuration(WellKnownTypes.Duration value)
        {
            long seconds = value.Seconds;
            long nanos = value.Nanoseconds;
            
            // If either sign is 0, we're fine. Otherwise, they should be the same. Multiplying them
            // together seems the easiest way to check that.
            if (Math.Sign(seconds) * Math.Sign(nanos) == -1)
            {
                throw new ArgumentException($"duration.Seconds and duration.Nanos have different signs: {seconds}s {nanos}ns", nameof(value));
            }
            return NodaTime.Duration.FromSeconds(seconds) + NodaTime.Duration.FromNanoseconds(nanos);
        }

        /// <summary>
        /// Converts a protobuf-net <see cref="WellKnownTypes.Timestamp"/> to a NodaTime <see cref="NodaTime.Instant"/>.
        /// </summary>
        public static NodaTime.Instant ToNodaTimeInstant(WellKnownTypes.Timestamp value)
        {
            // These correspond to 0001-01-01T00:00:00 and 9999-12-31T23:59:59 respectively.
            const long MinValidTimestampSeconds = -62135596800L; 
            const long MaxValidTimestampSeconds = 253402300799L;
            const long NanosecondsPerSecond = 1_000_000_000L;
            long seconds = value.Seconds, nanos = value.Nanoseconds;
            if (
                seconds < MinValidTimestampSeconds ||
                seconds > MaxValidTimestampSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"value.Seconds out of range: {value.Seconds}");
            }

            if (
                nanos < 0 ||
                nanos >= NanosecondsPerSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"value.Nanoseconds out of range: {value.Nanoseconds}");
            }
            return NodaTime.Instant.FromUnixTimeSeconds(seconds)
                .PlusNanoseconds(nanos);
        }
        
        /// <summary>
        /// Converts a NodaTime <see cref="NodaTime.Instant"/> to a protobuf-net <see cref="WellKnownTypes.Timestamp"/>.
        /// </summary>
        public static WellKnownTypes.Timestamp ToProtoBufTimestamp(NodaTime.Instant value)
        {
            if (value < NodaTime.NodaConstants.BclEpoch)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Instant is outside the range of Valid Protobuf timestamps");
            }
            // Truncated towards the start of time, which is what we want...
            var seconds = value.ToUnixTimeSeconds();
            var remainder = value - NodaTime.Instant.FromUnixTimeSeconds(seconds);
            // NanosecondOfDay is probably the most efficient way of turning a small, subsecond, non-negative duration
            // into a number of nanoseconds...
            return new WellKnownTypes.Timestamp(seconds: seconds, nanoseconds: (int) remainder.NanosecondOfDay);
        }
    }
}
