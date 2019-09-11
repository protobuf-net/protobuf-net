using System;

using System.Runtime.Serialization;
namespace ProtoBuf
{
    /// <summary>
    /// Indicates an error during serialization/deserialization of a proto stream.
    /// </summary>
    [Serializable]
    public class ProtoException : Exception
    {
        /// <summary>Creates a new ProtoException instance.</summary>
        public ProtoException() { }

        /// <summary>Creates a new ProtoException instance.</summary>
        public ProtoException(string message) : base(message) { }

        /// <summary>Creates a new ProtoException instance.</summary>
        public ProtoException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>Creates a new ProtoException instance.</summary>
        protected ProtoException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
