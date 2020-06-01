using ProtoBuf.Meta;
using System;

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
        /// Addition information about this serialization operation.
        /// </summary>
        object UserState { get; }
    }
}
