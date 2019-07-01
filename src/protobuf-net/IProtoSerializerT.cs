namespace ProtoBuf
{
    /// <summary>
    /// Abstract API capable of serializing/deserializing
    /// </summary>
    public interface IProtoSerializer<T>
    {
        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void Serialize(ProtoWriter writer, ref ProtoWriter.State state, T value);

        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T Deserialize(ProtoReader reader, ref ProtoReader.State state, T value);
    }
}
