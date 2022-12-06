using ProtoBuf.Internal;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates that a type is defined for protocol-buffer serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface,
        AllowMultiple = false, Inherited = false)]
    public sealed class ProtoContractAttribute : Attribute
    {
        internal const string ReferenceDynamicDisabled = "Reference-tracking and dynamic-type are not currently implemented in this build; they may be reinstated later; this is partly due to doubts over whether the features are adviseable, and partly over confidence in testing all the scenarios (it takes time; that time hasn't get happened); feedback is invited";
        /// <summary>
        /// Gets or sets the defined name of the type. This can be fully qualified , for example <c>.foo.bar.someType</c> if required.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the file that defines this type (as used with <c>import</c> in .proto)
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Gets or sets the fist offset to use with implicit field tags;
        /// only uesd if ImplicitFields is set.
        /// </summary>
        public int ImplicitFirstTag
        {
            get { return implicitFirstTag; }
            set
            {
                if (value < 1) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(ImplicitFirstTag));
                implicitFirstTag = value;
            }
        }
        private int implicitFirstTag;

        /// <summary>
        /// If specified, alternative contract markers (such as markers for XmlSerailizer or DataContractSerializer) are ignored.
        /// </summary>
        public bool UseProtoMembersOnly
        {
            get { return HasFlag(TypeOptions.UseProtoMembersOnly); }
            set { SetFlag(TypeOptions.UseProtoMembersOnly, value); }
        }

        /// <summary>
        /// If specified, do NOT treat this type as a list, even if it looks like one.
        /// </summary>
        public bool IgnoreListHandling
        {
            get { return HasFlag(TypeOptions.IgnoreListHandling); }
            set { SetFlag(TypeOptions.IgnoreListHandling, value); }
        }

        /// <summary>
        /// Gets or sets the mechanism used to automatically infer field tags
        /// for members. This option should be used in advanced scenarios only.
        /// Please review the important notes against the ImplicitFields enumeration.
        /// </summary>
        public ImplicitFields ImplicitFields { get; set; }

        /// <summary>
        /// Enables/disables automatic tag generation based on the existing name / order
        /// of the defined members. This option is not used for members marked
        /// with ProtoMemberAttribute, as intended to provide compatibility with
        /// WCF serialization. WARNING: when adding new fields you must take
        /// care to increase the Order for new elements, otherwise data corruption
        /// may occur.
        /// </summary>
        /// <remarks>If not explicitly specified, the default is assumed from Serializer.GlobalOptions.InferTagFromName.</remarks>
        public bool InferTagFromName
        {
            get { return HasFlag(TypeOptions.InferTagFromName); }
            set
            {
                SetFlag(TypeOptions.InferTagFromName, value);
                SetFlag(TypeOptions.InferTagFromNameHasValue, true);
            }
        }

        /// <summary>
        /// Has a InferTagFromName value been explicitly set? if not, the default from the type-model is assumed.
        /// </summary>
        internal bool InferTagFromNameHasValue
        { // note that this property is accessed via reflection and should not be removed
            get { return HasFlag(TypeOptions.InferTagFromNameHasValue); }
        }

        /// <summary>
        /// Specifies an offset to apply to [DataMember(Order=...)] markers;
        /// this is useful when working with mex-generated classes that have
        /// a different origin (usually 1 vs 0) than the original data-contract.
        /// 
        /// This value is added to the Order of each member.
        /// </summary>
        public int DataMemberOffset { get; set; }

        /// <summary>
        /// If true, the constructor for the type is bypassed during deserialization, meaning any field initializers
        /// or other initialization code is skipped.
        /// </summary>
        public bool SkipConstructor
        {
            get { return HasFlag(TypeOptions.SkipConstructor); }
            set { SetFlag(TypeOptions.SkipConstructor, value); }
        }

        /// <summary>
        /// Should this type be treated as a reference by default? Please also see the implications of this,
        /// as recorded on ProtoMemberAttribute.AsReference
        /// </summary>
        public bool AsReferenceDefault
        {
#if FEAT_DYNAMIC_REF
            get { return HasFlag(OPTIONS_AsReferenceDefault); }
            set { SetFlag(OPTIONS_AsReferenceDefault, value); }
#else
            get => false;
            [Obsolete(ReferenceDynamicDisabled, true)]
            set { if (value != AsReferenceDefault) ThrowHelper.ThrowNotSupportedException(); }
#endif
        }

        /// <summary>
        /// Indicates whether this type should always be treated as a "group" (rather than a string-prefixed sub-message)
        /// </summary>
        public bool IsGroup
        {
            get { return HasFlag(TypeOptions.IsGroup); }
            set
            {
                SetFlag(TypeOptions.IsGroup, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether unknown sub-types should cause serialization failure
        /// </summary>
        public bool IgnoreUnknownSubTypes
        {
            get => HasFlag(TypeOptions.IgnoreUnknownSubTypes);
            set => SetFlag(TypeOptions.IgnoreUnknownSubTypes, value);
        }

        private bool HasFlag(TypeOptions flag) { return (flags & flag) == flag; }
        private void SetFlag(TypeOptions flag, bool value)
        {
            if (value) flags |= flag;
            else flags &= ~flag;
        }

        private TypeOptions flags;

        [Flags]
        private enum TypeOptions : ushort
        {
            InferTagFromName = 1,
            InferTagFromNameHasValue = 2,
            UseProtoMembersOnly = 4,
            SkipConstructor = 8,
            IgnoreListHandling = 16,
#if FEAT_DYNAMIC_REF
            AsReferenceDefault = 32,
#endif
            //EnumPassthru = 64,
            //EnumPassthruHasValue = 128,
            IsGroup = 256,
            IgnoreUnknownSubTypes = 512,
        }

        /// <summary>
        /// Applies only to enums (not to DTO classes themselves); gets or sets a value indicating that an enum should be treated directly as an int/short/etc, rather
        /// than enforcing .proto enum rules. This is useful *in particul* for [Flags] enums.
        /// </summary>
        [Obsolete(ProtoEnumAttribute.EnumValueDeprecated, true)]
        public bool EnumPassthru
        {
            get { return true; }
            set { if (!value) ThrowHelper.ThrowInvalidOperationException($"{nameof(EnumPassthru)} is not longer supported, and is always considered true"); }
        }

        /// <summary>
        /// Defines a surrogate type used for serialization/deserialization purpose.
        /// </summary>
        public Type Surrogate { get; set; }

        /// <summary>
        /// Defines a serializer to use for this type; the serializer must implement ISerializer-T for this type
        /// </summary>
        [DynamicallyAccessedMembers(DynamicAccess.Serializer)]
        public Type Serializer { get; set; }
    }
}