namespace ProtoBuf
{
    /// <summary>
    /// Abstract API capable of serializing/deserializing
    /// </summary>
    public interface IProtoSerializer<in TBase, TActual> where TActual : TBase
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
    /// Abstract API capable of serializing/deserializing complex objects with inheritance
    /// </summary>
    public interface IProtoFactory<TBase, TActual> : IProtoSerializer<TBase, TActual>
        where TActual : TBase
    {
        /// <summary>
        /// Populate all possible values from a pre-existing object
        /// </summary>
        void Copy(SerializationContext context, TBase from, TActual to);

        /// <summary>
        /// Create a new instance of the concrete type TActual
        /// </summary>
        TActual Create(SerializationContext context);
    }
}
