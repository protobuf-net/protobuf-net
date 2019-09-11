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
        void Serialize(ProtoWriter writer, ref ProtoWriter.State state, ref T obj);

        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        void Deserialize(ProtoReader reader, ref ProtoReader.State state, ref T obj);
    }
}
