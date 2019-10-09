using ProtoBuf.Meta;

namespace ProtoBuf
{
    /// <summary>
    /// Represents common state during a serialization operation; this instance should not be stored - it may be reused later with different meaning
    /// </summary>
    public interface ISerializationContext
    {
        /// <summary>
        /// The type-model that represents the operation
        /// </summary>
        TypeModel Model { get; }
        /// <summary>
        /// The serialization-context specified for the operation
        /// </summary>
        SerializationContext Context { get; }
    }
}
