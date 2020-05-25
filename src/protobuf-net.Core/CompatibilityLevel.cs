using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Defines the compatibility level / conventions to use when encoding common
    /// types into protobuf. When starting a new green-field project the highest
    /// available level can be safely applied, but note that changing the
    /// compatibility level changes the encoding. For this reason, it should not
    /// be casually changed on brown-field projects, unless you are also knowingly
    /// breaking the encoding requirements of pre-existing data. If not specified,
    /// the oldest (lowest number) is assumed, for safety.
    /// </summary>
    public enum CompatibilityLevel
    {
        /// <summary>
        /// Uses bcl.proto for DateTime, TimeSpan, Guid and Decimal, for compatibility
        /// with all versions of protobuf-net, at the expense of being inconvenient
        /// for use with other protobuf implementations
        /// </summary>
        Level240 = 240,
        /// <summary>
        /// Uses WellKnownTypes.Timestamp for DateTime, WellKnownTypes.Duration for TimeSpan,
        /// string for Guid ("N"); Decimal is TBD, tracking https://github.com/protocolbuffers/protobuf/pull/7039
        /// </summary>
        Level300 = 300,
    }

    /// <summary>
    /// Defines the compatibiltiy level to use for an element
    /// </summary>
    [ImmutableObject(true)]
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Module
        | AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple =false, Inherited = true)]
    public sealed class CompatibilityLevelAttribute : Attribute
    {
        /// <summary>
        /// The compatibiltiy level to use for this element
        /// </summary>
        public CompatibilityLevel Level { get; }

        /// <summary>
        /// Create a new CompatibilityLevelAttribute instance
        /// </summary>
        public CompatibilityLevelAttribute(CompatibilityLevel level)
            => Level = level;
    }
}
