using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Declares the default <see cref="ProtoBuf.DataFormat"/> for members of a given type, applied
    /// wherever the member does not state a format itself. An explicit
    /// <see cref="ProtoMemberAttribute.DataFormat"/> always wins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolution mirrors <see cref="CompatibilityLevelAttribute"/>: the declaring contract type
    /// (including its base types), then the module, then the assembly. The declaration applies to
    /// the member's scalar type — for a <c>Nullable&lt;T&gt;</c> member the underlying <c>T</c>,
    /// for a repeated member the element type. Map key/value formats belong to
    /// <see cref="ProtoMapAttribute"/> and are not affected.
    /// </para>
    /// <para>
    /// The motivating case: <c>[assembly: ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]</c>
    /// alongside <c>[assembly: CompatibilityLevel(CompatibilityLevel.Level300)]</c> makes every
    /// undecorated <see cref="Guid"/> member serialize as the 16-byte form.
    /// </para>
    /// </remarks>
    [ImmutableObject(true)]
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Module
        | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        AllowMultiple = true, Inherited = true)]
    public sealed class ProtoDataFormatAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="type">The member type the default applies to.</param>
        /// <param name="dataFormat">The format such members take when they state none.</param>
        public ProtoDataFormatAttribute(Type type, DataFormat dataFormat)
        {
            Type = type;
            DataFormat = dataFormat;
        }

        /// <summary>
        /// The member type the default applies to.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The format such members take when they state none.
        /// </summary>
        public DataFormat DataFormat { get; }
    }
}
