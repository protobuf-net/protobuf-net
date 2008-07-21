using System;

namespace ProtoBuf
{
    /// <summary>
    /// Used to define protocol-buffer specific behavior for
    /// enumerated values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple=false)]
    public sealed class ProtoEnumAttribute : Attribute
    {
        /// <summary>
        /// The specific value to use for this enum during serialization.
        /// </summary>
        public long Value {
            get { return value.GetValueOrDefault(); }
            set
            {
                if (value < 0 || value > MAX_VALUE)
                {
                    throw new ArgumentOutOfRangeException("Value");
                }
                this.value = value;
            }
        }
        /// <summary>
        /// Indicates whether this instance has a customised value mapping
        /// </summary>
        /// <returns>true if a specific value is set</returns>
        public bool HasValue() { return value.HasValue; }
        const long MAX_VALUE = 0x7FFFFFFF;
        private long? value;
        /// <summary>
        /// The defined name of the enum, as used in .proto
        /// (this name is not used during serialization).
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        private string name;

    }
}
