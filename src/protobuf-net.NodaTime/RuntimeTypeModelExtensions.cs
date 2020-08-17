using System;

namespace ProtoBuf.Meta // note: choice of API is deliberate; ProtoBuf.Meta causes lots of namespace confusion (global::NodaTime vs etc), and
{                       // anyone using this API should *already* have ProtoBuf.Meta, making this "just work"
    /// <summary>
    /// Provides extension APIs for protobuf-net's <see cref="RuntimeTypeModel"/>.
    /// </summary>
    public static class RuntimeTypeModelExtensions
    {
        /// <summary>
        /// Registers protobuf-net serialization surrogates for all supported NodaTime primitives.
        /// </summary>
        /// <param name="model">The model to extend (<see cref="RuntimeTypeModel.Default"/> is assumed if it is <c>null</c>)</param>.
        /// <returns>The model instance.</returns>
        public static RuntimeTypeModel AddNodaTimeSurrogates(this RuntimeTypeModel model)
            => (model ?? RuntimeTypeModel.Default).SetSurrogate<NodaTime.Duration, WellKnownTypes.Duration>(ToProtobufDuration, FromProtobufDuration);

        /// <summary>
        /// Converts a NodaTime <see cref="NodaTime.Duration"/> to a protobuf-net <see cref="WellKnownTypes.Duration"/>.
        /// </summary>
        public static WellKnownTypes.Duration ToProtobufDuration(NodaTime.Duration duration)
        {   // influenced by NodaTime.Serialization.Protobuf/NodaExtensions.cs ToProtobufDuration
            
            // Deliberately long to keep the later arithmetic in 64-bit.
            long days = duration.Days;
            long nanoOfDay = duration.NanosecondOfDay;
            long secondOfDay = nanoOfDay / NodaTime.NodaConstants.NanosecondsPerSecond;
            int nanos = duration.SubsecondNanoseconds;
            return new WellKnownTypes.Duration(seconds: days * NodaTime.NodaConstants.SecondsPerDay + secondOfDay, nanoseconds: nanos);
        }

        /// <summary>
        /// Converts a protobuf-net <see cref="WellKnownTypes.Duration"/> to a NodaTime <see cref="NodaTime.Duration"/>.
        /// </summary>
        public static NodaTime.Duration FromProtobufDuration(WellKnownTypes.Duration duration)
        {   // influenced by NodaTime.Serialization.Protobuf/ProtobufExtensions.cs ToNodaDuration
            long seconds = duration.Seconds;
            long nanos = duration.Nanoseconds;
            
            // If either sign is 0, we're fine. Otherwise, they should be the same. Multiplying them
            // together seems the easiest way to check that.
            if (Math.Sign(seconds) * Math.Sign(nanos) == -1)
            {
                throw new ArgumentException($"duration.Seconds and duration.Nanos have different signs: {seconds}s {nanos}ns", nameof(duration));
            }
            return NodaTime.Duration.FromSeconds(seconds) + NodaTime.Duration.FromNanoseconds(nanos);
        }
    }
}
