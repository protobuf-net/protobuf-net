using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates the encoding used to represent an individual value in a protobuf stream
    /// </summary>
    public enum WireType
    {
        /// <summary>
        /// Represents an error condition
        /// </summary>
        None = -1,

        /// <summary>
        /// Base-128 variable-length encoding
        /// </summary>
        [Obsolete("This is an embarrassing typo... sorry; see also: Varint")]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        Variant = Varint, // fun fact: by defining Variant first, ToString prefers Varint!

        /// <summary>
        /// Base-128 variable-length encoding
        /// </summary>
        Varint = 0,

        /// <summary>
        /// Fixed-length 8-byte encoding
        /// </summary>
        Fixed64 = 1,

        /// <summary>
        /// Length-variant-prefixed encoding
        /// </summary>
        String = 2,

        /// <summary>
        /// Indicates the start of a group
        /// </summary>
        StartGroup = 3,

        /// <summary>
        /// Indicates the end of a group
        /// </summary>
        EndGroup = 4,

        /// <summary>
        /// Fixed-length 4-byte encoding
        /// </summary>10
        Fixed32 = 5,

        /// <summary>
        /// This is not a formal wire-type in the "protocol buffers" spec, but
        /// denotes a varint that should be interpreted using
        /// zig-zag semantics (so -ve numbers aren't a significant overhead)
        /// </summary>
        [Obsolete("This is an embarrassing typo... sorry; see also: SignedVarint")]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        SignedVariant = SignedVarint, // see fun fact above

        /// <summary>
        /// This is not a formal wire-type in the "protocol buffers" spec, but
        /// denotes a varint that should be interpreted using
        /// zig-zag semantics (so -ve numbers aren't a significant overhead)
        /// </summary>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        SignedVarint = Varint | (1 << 3),
    }
}
