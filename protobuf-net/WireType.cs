namespace ProtoBuf
{
    internal enum WireType
    {
        /// <summary>
        /// Base-128 variant-length encoding
        /// </summary>
        Variant = 0,

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
        /// </summary>
        Fixed32 = 5
    }
}
