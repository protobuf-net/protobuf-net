using System;

namespace ProtoBuf.NetExtensions
{
    /// <summary>
    /// Indicates the known-types to support for an individual
    /// property. IMPORTANT: note that this feature is an
    /// implemenation-specific extension to the .proto spec,
    /// and it will not be possible to generate a 100% compatible
    /// .proto from a type that uses this attribute. Under .proto,
    /// each separate type will have to load into a separate field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,
        AllowMultiple = true, Inherited = true)]
    public sealed class ProtoIncludeAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="knownType">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, Type knownType)
            : this(tag, knownType == null ? "" : knownType.AssemblyQualifiedName) { }

        /// <summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="knownTypeName">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, string knownTypeName)
        {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag", "Tags must be positive integers");
            if (string.IsNullOrEmpty(knownTypeName)) throw new ArgumentNullException("knownTypeName", "Known type cannot be blank");
            Tag = tag;
            KnownTypeName = knownTypeName;
        }

        /// <summary>
        /// Gets the unique index (within the type) that will identify this data.
        /// </summary>
        public int Tag { get { return tag; } private set { tag = value; } }
        private int tag;

        /// <summary>
        /// Gets the additional type to serialize/deserialize.
        /// </summary>
        public string KnownTypeName { get { return name; } private set { name = value; } }
        private string name;

        /// <summary>
        /// Gets the additional type to serialize/deserialize.
        /// </summary>
        public Type KnownType
        {
            get
            {
                return Type.GetType(KnownTypeName);
            }
        }
    }
}
