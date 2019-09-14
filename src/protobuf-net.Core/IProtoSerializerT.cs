namespace ProtoBuf
{
    /// <summary>
    /// Abstract API capable of serializing/deserializing
    /// </summary>
    public interface IBasicSerializer<in T>
    {
        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void Serialize(ProtoWriter writer, ref ProtoWriter.State state, T value);
    }

    public interface IBasicDeserializer<T>
    {
        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T Deserialize(ProtoReader reader, ref ProtoReader.State state, T value);
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing objects as part of a type hierarchy
    /// </summary>
    public interface ISubTypeSerializer<T> where T : class
    {
        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void Serialize(ProtoWriter writer, ref ProtoWriter.State state, T value);

        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T Deserialize<TCreate>(ProtoReader reader, ref ProtoReader.State state, object value) where TCreate : T;
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing complex objects with inheritance
    /// </summary>
    public interface IProtoFactory<T> where T : class
    {
        /// <summary>
        /// Create a new instance of the type
        /// </summary>
        T Create(SerializationContext context);
    }
}
