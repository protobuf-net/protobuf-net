namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// Represents the well-known empty type
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty")]
    public readonly struct Empty
    {
        public static IProtoSerializer<Empty> Serializer => WellKnownSerializer.Instance;

        /// <summary>
        /// Tests an object for equality
        /// </summary>
        public override bool Equals(object obj) => obj is Empty;
        /// <summary>
        /// Tests an object for equality
        /// </summary>
        public override int GetHashCode() => 42;

        /// <summary>
        /// See object.ToString()
        /// </summary>
        public override string ToString() => nameof(Empty);
    }
}
