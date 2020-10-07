using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ProtoBuf.Internal;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates the known-types to support for an individual
    /// message. This serializes each level in the hierarchy as
    /// a nested message to retain wire-compatibility with
    /// other protocol-buffer implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class ProtoIncludeAttribute : Attribute
    {
        ///<summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="knownType">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type knownType)
            : this(tag, knownType is null ? "" : knownType.AssemblyQualifiedName) { }

        /// <summary>
        /// Creates a new instance of the ProtoIncludeAttribute.
        /// </summary>
        /// <param name="tag">The unique index (within the type) that will identify this data.</param>
        /// <param name="knownTypeName">The additional type to serialize/deserialize.</param>
        public ProtoIncludeAttribute(int tag, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] string knownTypeName)
        {
            if (tag <= 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(tag), "Tags must be positive integers");
            if (string.IsNullOrEmpty(knownTypeName)) ThrowHelper.ThrowArgumentNullException(nameof(knownTypeName), "Known type cannot be blank");
            Tag = tag;
            KnownTypeName = knownTypeName;
        }

        /// <summary>
        /// Gets the unique index (within the type) that will identify this data.
        /// </summary>
        public int Tag { get; }

        /// <summary>
        /// Gets the additional type to serialize/deserialize.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicAccess.ContractType)]
        public string KnownTypeName { get; }

        /// <summary>
        /// Gets the additional type to serialize/deserialize.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicAccess.ContractType)]
        public Type KnownType => TypeModel.ResolveKnownType(KnownTypeName, null);

        /// <summary>
        /// Specifies whether the inherited type's sub-message should be
        /// written with a length-prefix (default), or with group markers.
        /// </summary>
        [DefaultValue(DataFormat.Default)]
        public DataFormat DataFormat { get; set; } = DataFormat.Default;
    }
}
