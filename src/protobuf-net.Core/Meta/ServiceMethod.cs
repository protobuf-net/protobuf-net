using ProtoBuf.WellKnownTypes;
using System;
namespace ProtoBuf.Meta
{
    /// <summary>
    /// Describes a method of a service.
    /// </summary>
    public sealed class ServiceMethod
    {
        /// <summary>
        /// The name of the method.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type sent by the client.
        /// </summary>
        public Type InputType { get; set; } = typeof(Empty);

        /// <summary>
        /// The type returned from the server.
        /// </summary>
        public Type OutputType { get; set; } = typeof(Empty);

        /// <summary>
        /// Identifies if server streams multiple server messages.
        /// </summary>
        public bool ServerStreaming { get; set; }

        /// <summary>
        /// Identifies if client streams multiple client messages.
        /// </summary>
        public bool ClientStreaming { get; set; }
    }
}
