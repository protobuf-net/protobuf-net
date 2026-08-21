using System;
using ProtoBuf.Internal;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Declares one type as a sub-type of another, for hierarchies that cannot be described by
    /// <see cref="ProtoIncludeAttribute"/> because the base type has never heard of the sub-type —
    /// it lives in another assembly, or is a generic construction the base library cannot name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the compile-time equivalent of <c>MetaType.AddSubType</c>, and is read by the
    /// protobuf-net source generator; it has no effect on the reflection-based model, which is
    /// served by <c>AddSubType</c> itself. The outcome is exactly as though the base type had
    /// carried a matching <see cref="ProtoIncludeAttribute"/> all along.
    /// </para>
    /// <para>
    /// Apply it to a generated model to configure that model alone, or to an <b>assembly</b> or
    /// <b>module</b> to offer the linkage to every model that references it — which is how a
    /// library ships the sub-types it knows about without each consumer restating them.
    /// Declarations accumulate rather than override: two references each naming a sub-type of the
    /// same base give a hierarchy with both. Naming the same sub-type twice, or two sub-types at
    /// one field number, is reported and drops the hierarchy.
    /// </para>
    /// <para>
    /// Note that a sub-type declared here participates in the same all-or-nothing cascade as any
    /// other: if the generator cannot handle it, the hierarchy it joins is dropped too — including
    /// types you own, when the declaration came from a library you merely reference.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly | AttributeTargets.Module,
        AllowMultiple = true, Inherited = false)]
    [Experimental(Experiments.CompileTimeModel, UrlFormat = Experiments.UrlFormat)]
    public sealed class ProtoSubTypeAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance, leaving the framing of the sub-message to protobuf-net (which
        /// today means a length prefix, as <see cref="DataFormat.Default"/> does).
        /// </summary>
        /// <param name="baseType">The type being extended.</param>
        /// <param name="subType">The additional type to serialize/deserialize.</param>
        /// <param name="fieldNumber">The unique index (within <paramref name="baseType"/>) that will identify this data.</param>
        public ProtoSubTypeAttribute(
            [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type baseType,
            [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type subType,
            int fieldNumber)
        {
            BaseType = baseType;
            SubType = subType;
            FieldNumber = fieldNumber;
        }

        /// <summary>
        /// Create a new instance, stating the framing of the sub-message explicitly.
        /// </summary>
        /// <param name="baseType">The type being extended.</param>
        /// <param name="subType">The additional type to serialize/deserialize.</param>
        /// <param name="fieldNumber">The unique index (within <paramref name="baseType"/>) that will identify this data.</param>
        /// <param name="group">
        /// Whether the sub-message is written with group markers (<c>true</c>) or with a length
        /// prefix (<c>false</c>). Stating <c>false</c> is not the same as using the constructor
        /// without this argument: both are length-prefixed today, but only one of them says so.
        /// </param>
        /// <remarks>
        /// A sub-type is always a sub-message, so length-prefixed versus delimited is the only
        /// choice there is to make — which is why this is a <see cref="bool"/> rather than the
        /// <see cref="DataFormat"/> that <see cref="ProtoIncludeAttribute.DataFormat"/> takes, most
        /// of whose values would have nothing to select.
        /// </remarks>
        public ProtoSubTypeAttribute(
            [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type baseType,
            [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type subType,
            int fieldNumber, bool group)
            : this(baseType, subType, fieldNumber) => IsGroup = group;

        /// <summary>
        /// The type being extended.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicAccess.ContractType)]
        public Type BaseType { get; }

        /// <summary>
        /// The additional type to serialize/deserialize.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicAccess.ContractType)]
        public Type SubType { get; }

        /// <summary>
        /// The unique index (within <see cref="BaseType"/>) that will identify this data.
        /// </summary>
        public int FieldNumber { get; }

        /// <summary>
        /// Whether the sub-message is written with group markers rather than a length prefix, or
        /// <c>null</c> where the constructor taking a <c>group</c> argument was not used, leaving
        /// the framing to protobuf-net.
        /// </summary>
        /// <remarks>
        /// Deliberately three-state: "explicitly length-prefixed" and "not stated" are the same
        /// thing on the wire today, and a <see cref="bool"/> would make them the same thing in the
        /// metadata too — which would be a decision, not an omission, if the default ever moves.
        /// </remarks>
        public bool? IsGroup { get; }
    }
}
