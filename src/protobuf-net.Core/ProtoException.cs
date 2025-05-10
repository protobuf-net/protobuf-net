﻿using System;

using System.Runtime.Serialization;
namespace ProtoBuf
{
    /// <summary>
    /// Indicates an error during serialization/deserialization of a proto stream.
    /// </summary>
#pragma warning disable SYSLIB0050 // binary formatter - legacy only
    [Serializable]
#pragma warning restore SYSLIB0050 // binary formatter - legacy only
    public class ProtoException : Exception
    {
        /// <summary>Creates a new ProtoException instance.</summary>
        public ProtoException() { }

        /// <summary>Creates a new ProtoException instance.</summary>
        public ProtoException(string message) : base(message) { }

        /// <summary>Creates a new ProtoException instance.</summary>
        public ProtoException(string message, Exception innerException) : base(message, innerException) { }

#pragma warning disable SYSLIB0051 // binary formatter - legacy API only
        /// <summary>Creates a new ProtoException instance.</summary>
        protected ProtoException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#pragma warning restore SYSLIB0051 // binary formatter - legacy API only
    }
}
