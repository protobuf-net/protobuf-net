namespace ProtoBuf
{
    /// <summary>
    /// Represents the well-known empty type
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty")]
    public sealed class Empty
    {
        /// <summary>
        /// Tests an object for equality
        /// </summary>
        public override bool Equals(object obj) => obj is Empty;
        /// <summary>
        /// Tests an object for equality
        /// </summary>
        public override int GetHashCode() => 42;

        /// <summary>
        /// Create a new instance
        /// </summary>
        public Empty() { }

        /// <summary>
        /// A shared Empty instance
        /// </summary>
        public static Empty Default { get; } = new Empty();
    }
}
