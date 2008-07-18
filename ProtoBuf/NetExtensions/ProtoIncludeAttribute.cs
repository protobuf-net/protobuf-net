using System;
using System.ComponentModel;

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
    [ImmutableObject(true)]
    public sealed class ProtoIncludeAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="type">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, Type type)
            : this(tag, type == null ? "" : type.AssemblyQualifiedName) {}
        /// <summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="typeName">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, string typeName)
        {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag");
            if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");
            Tag = tag;
            TypeName = typeName;
        }
        /// <summary>
        /// The unique index (within the type) that will identify this data.
        /// </summary>
        public int Tag { get; private set; }
        /// <summary>
        /// The additional type to serialize/deserialize.
        /// </summary>
        public string TypeName { get; private set; }
        /// <summary>
        /// The additional type to serialize/deserialize.
        /// </summary>
        public Type Type
        {
            get
            {
                try
                {
                    return Type.GetType(TypeName);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
