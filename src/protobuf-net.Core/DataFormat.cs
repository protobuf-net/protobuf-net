
using System;

namespace ProtoBuf
{
    /// <summary>
    /// Sub-format to use when serializing/deserializing data
    /// </summary>
    public enum DataFormat
    {
        /// <summary>
        /// Uses the default encoding for the data-type.
        /// </summary>
        Default,

        /// <summary>
        /// When applied to signed integer-based data (including Decimal), this
        /// indicates that zigzag variant encoding will be used. This means that values
        /// with small magnitude (regardless of sign) take a small amount
        /// of space to encode.
        /// </summary>
        ZigZag,

        /// <summary>
        /// When applied to signed integer-based data (including Decimal), this
        /// indicates that two's-complement variant encoding will be used.
        /// This means that any -ve number will take 10 bytes (even for 32-bit),
        /// so should only be used for compatibility.
        /// </summary>
        TwosComplement,

        /// <summary>
        /// When applied to signed integer-based data (including Decimal), this
        /// indicates that a fixed amount of space will be used.
        /// </summary>
        FixedSize,

        /// <summary>
        /// When applied to a sub-message, indicates that the value should be treated
        /// as group-delimited: framed by a start/end tag pair rather than carrying a
        /// length prefix.
        /// </summary>
        /// <remarks>
        /// This is the encoding that protobuf <b>editions</b> calls
        /// <c>features.message_encoding = DELIMITED</c>; <see cref="Delimited"/> is the
        /// same value under that name. Deprecated in proto3 and reinstated by editions as
        /// a first-class choice, it is often the <i>faster</i> option to write, since a
        /// message with no length prefix needs no size computed before it is emitted.
        /// </remarks>
        Group,

        /// <summary>
        /// When applied to members of types such as DateTime or TimeSpan, specifies
        /// that the "well known" standardized representation should be use; DateTime uses Timestamp,
        /// TimeSpan uses Duration.
        /// </summary>
        [Obsolete("This option is replaced with " + nameof(CompatibilityLevel) + ", and is only used for " + nameof(CompatibilityLevel.Level200) + ", where it changes this field to " + nameof(CompatibilityLevel.Level240), false)]
        WellKnown,

        /// <summary>
        /// A synonym for <see cref="Group"/>, matching the name protobuf <b>editions</b> uses for
        /// this encoding: <c>features.message_encoding = DELIMITED</c>. The two are the same value
        /// and behave identically; prefer whichever matches the vocabulary you are working in.
        /// </summary>
        /// <remarks>
        /// <see cref="Group"/> is <b>not</b> deprecated and is not going anywhere: it is what
        /// protobuf-net has always called this, it is what <c>protogen</c> emits into generated
        /// code, and obsoleting it would raise warnings in generated files consumers cannot edit.
        /// This member exists so that code written against the editions specification can say what
        /// the specification says.
        /// </remarks>
        Delimited = Group,
    }
}