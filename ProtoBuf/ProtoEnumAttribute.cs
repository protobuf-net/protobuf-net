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
            get { return value; }
            set
            {
                if (value < 0 || value > MAX_VALUE)
                {
                    throw new ArgumentOutOfRangeException("Value");
                }
                this.value = value;
            }
        }
        const long MAX_VALUE = 0x7FFFFFFF;
        private long value = -1;
        /// <summary>
        /// The defined name of the enum, as used in .proto
        /// (this name is not used during serialization).
        /// </summary>
        public string Name { get; set; }

    }
}
