using System;

namespace ProtoBuf
{
    /// <summary>when used on a nullable scalar value, indicates that an extra message layer
    /// is added, for compatibility with <c>wrappers.proto</c>, rather than
    /// "field presence";
    /// when used on a collection/dictionary, indicates  that the values can track nulls;
    /// see https://protobuf-net.github.io/protobuf-net/nullwrappers for more information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NullWrappedValueAttribute : Attribute
    {
        /// <summary>Indicates that the collection message wrapper should use group encoding; this is more
        /// efficient to write, but may be hard to consume in cross-platform scenarios; this feature is
        /// usually used for compatibility with protobuf-net v2 <c>SupportNull</c> usage</summary>
        public bool AsGroup { get; set; }
    }

    /// <summary>Indicates that a collection can track the difference between a null and empty collection;
    /// see https://protobuf-net.github.io/protobuf-net/nullwrappers for more information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NullWrappedCollectionAttribute : Attribute
    {
        /// <summary>Indicates that the collection message wrapper should use group encoding; this is more efficient to write, but may be hard to consume in cross-platform scenarios.</summary>
        public bool AsGroup { get; set; }
    }
}