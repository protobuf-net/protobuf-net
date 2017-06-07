using System;

namespace ProtoBuf
{
    /// <summary>
    /// Controls the formatting of elements in a dictionary, and indicates that
    /// "map" rules should be used: duplicates *replace* earlier values, rather
    /// than throwing an exception
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ProtoMapAttribute : Attribute
    {
        /// <summary>
        /// Describes the data-format used to store the key
        /// </summary>
        public DataFormat KeyFormat { get; set; }
        /// <summary>
        /// Describes the data-format used to store the value
        /// </summary>
        public DataFormat ValueFormat { get; set; }
    }
}
