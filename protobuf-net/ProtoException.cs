using System;
using System.Collections.Generic;
using System.Text;
#if REMOTING
using System.Runtime.Serialization;
#endif
namespace ProtoBuf
{
    /// <summary>
    /// Indicates an error during serialization/deserialization of a proto stream
    /// </summary>
    public sealed class ProtoException : Exception
    {
        /// <summary>
        /// Creates a new ProtoException instance.
        /// </summary>
        internal ProtoException(string message) : base(message) { }
        /// <summary>
        /// Creates a new ProtoException instance.
        /// </summary>
        internal ProtoException(string message, Exception innerException) : base(message, innerException) { }

#if REMOTING
        /// <summary>
        /// Creates a new ProtoException instance.
        /// </summary>
        private ProtoException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }
}
