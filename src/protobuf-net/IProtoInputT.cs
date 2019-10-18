namespace ProtoBuf
{
    /// <summary>
    /// Represents the ability to serialize values to an output of type <typeparamref name="TOutput"/>
    /// </summary>
    public interface IProtoOutput<TOutput>
    {
        /// <summary>
        /// Serialize the provided value
        /// </summary>
        void Serialize<T>(TOutput destination, T value, object userState = null);
    }
}
