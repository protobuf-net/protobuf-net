using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates that a value should be encoded as via <a href="https://github.com/protocolbuffers/protobuf/blob/master/src/google/protobuf/wrappers.proto">Wrappers.proto</a>, which
    /// is useful for conveying explicit <c>null</c> values (at the expense of some small additional payload size). This functionality
    /// limited to <c>double?</c>, <c>float?</c>, <c>long?</c>, <c>ulong?</c>, <c>int?</c>, <c>uint?</c>, <c>bool?</c>, <c>string</c> and <c>byte[]</c>; usage on any other type
    /// is illegal and will raise an error. No default value (`[DefaultValue(...)]`) can be specified, and no format (or <c>DataFormat.Default</c>) must be used.
    /// When the value is <c>null</c>, nothing is written to the stream; when the value is not <c>null</c>, a length-prefixed message is emitted, and if the value is non-zero/empty,
    /// then the value is written as field 1 (as per <c>Wrappers.proto</c>).
    /// </summary>
    /// <remarks>Adding or removing this attribute is a payload breaking change.</remarks>
    [ImmutableObject(true)]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class WrappedAttribute : Attribute { }

    /// <summary>
    /// Indicates that collection can include <c>null</c> elements/values, and the expense of some small additional payload size; each value (including <c>null</c> values)
    /// is written as a message (length-prefixed by default, although grouped encoding is also supported); the value is then written using proto3
    /// <a href="https://github.com/protocolbuffers/protobuf/blob/master/docs/field_presence.md">field presence</a> rules, i.e. if the value is <c>null</c>,
    /// no field is written, otherwise it is written as field 1 inside the sub-message.
    /// It is an error to use this feature on a non-collection type.
    /// </summary>
    /// <remarks>Adding or removing this attribute is a payload breaking change; this feature can be combined with <see cref="CollectionSupportsNullCollectionAttribute"/>;
    /// the <c>ValueMember.SupportNull</c> feature is identical to <c>[CollectionSupportsNullElements(true)]</c>.</remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class CollectionSupportsNullElementsAttribute : Attribute
    {
        /// <summary>Indicates that the collection message wrapper shold use group encoding; this is more efficient to write, but may be hard to consume in cross-platform scenarios.</summary>
        public bool AsGroup { get; }

        /// <summary>Create a new <see cref="CollectionSupportsNullElementsAttribute"/> instance.</summary>
        public CollectionSupportsNullElementsAttribute(bool asGroup = false) => AsGroup = asGroup;
    }

    /// <summary>
    /// Indicates that collection itself can determing between a <c>null</c> and non-<c>null</c> empty collection, and the expense of some small additional payload size;
    /// the collection is written as though it were a sub-message that has a <c>repeated</c> field representing the elements. A <c>null</c>
    /// collection is not written at all; otherwise, a message (length-prefixed by default, although grouped encoding is also supported) is written, which can contain
    /// zero or more elements as field 1.
    /// It is an error to use this feature on a non-collection type.
    /// <remarks>Adding or removing this attribute is a payload breaking change; this feature can be combined with <see cref="CollectionSupportsNullElementsAttribute"/></remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class CollectionSupportsNullCollectionAttribute : Attribute
    {
        /// <summary>Indicates that the collection message wrapper shold use group encoding; this is more efficient to write, but may be hard to consume in cross-platform scenarios.</summary>
        public bool AsGroup { get; }

        /// <summary>Create a new <see cref="CollectionSupportsNullCollectionAttribute"/> instance.</summary>
        public CollectionSupportsNullCollectionAttribute(bool asGroup = false) => AsGroup = asGroup;
    }
}