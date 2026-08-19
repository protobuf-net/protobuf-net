
using System;
using System.ComponentModel;

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
        /// A synonym for <see cref="Delimited"/>, which is the preferred spelling: when applied to
        /// a sub-message, the value is framed by a start/end tag pair rather than carrying a length
        /// prefix.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is protobuf-net's original name for the encoding, and it is <b>not</b> deprecated —
        /// it remains valid, is what <c>protogen</c> emits into generated code, and is the name
        /// <see cref="object.ToString"/> reports for the value. It is hidden from IntelliSense only
        /// so that new code reaches for <see cref="Delimited"/>, the name the protobuf
        /// <b>editions</b> specification uses (<c>features.message_encoding = DELIMITED</c>).
        /// </para>
        /// <para>
        /// Deprecated in proto3 and reinstated by editions as a first-class choice, this framing is
        /// often the <i>faster</i> option to write: a message carrying no length prefix needs no
        /// size computed before it is emitted.
        /// </para>
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        Group,

        /// <summary>
        /// When applied to members of types such as DateTime or TimeSpan, specifies
        /// that the "well known" standardized representation should be use; DateTime uses Timestamp,
        /// TimeSpan uses Duration.
        /// </summary>
        [Obsolete("This option is replaced with " + nameof(CompatibilityLevel) + ", and is only used for " + nameof(CompatibilityLevel.Level200) + ", where it changes this field to " + nameof(CompatibilityLevel.Level240), false)]
        WellKnown,

        /// <summary>
        /// When applied to a sub-message, indicates that the value is framed by a start/end tag
        /// pair rather than carrying a length prefix — the encoding protobuf <b>editions</b> calls
        /// <c>features.message_encoding = DELIMITED</c>. A synonym for <see cref="Group"/>, which
        /// is the same value under protobuf-net's original name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Prefer this spelling in new code: it matches the specification, and
        /// <see cref="Group"/> is hidden from IntelliSense for that reason. The two are
        /// interchangeable — <see cref="Group"/> is not deprecated, remains what <c>protogen</c>
        /// emits, and is the name <see cref="object.ToString"/> reports for the value.
        /// </para>
        /// <para>
        /// Reinstated by editions as a first-class choice after being deprecated in proto3, and
        /// often the <i>faster</i> option to write: a message carrying no length prefix needs no
        /// size computed before it is emitted.
        /// </para>
        /// </remarks>
        Delimited = Group,
    }
}