namespace ProtoBuf
{
    /// <summary>
    /// Abstract API capable of serializing/deserializing
    /// </summary>
    public interface IProtoSerializer<TBase, TActual> where TActual : TBase
    {
        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void Serialize(ProtoWriter writer, ref ProtoWriter.State state, TActual value);

        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        TActual Deserialize(ProtoReader reader, ref ProtoReader.State state, TBase value);
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing
    /// </summary>
    public interface IProtoSerializer<T> : IProtoSerializer<T, T> { }
}
