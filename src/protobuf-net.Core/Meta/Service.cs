using System.Collections.Generic;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Describes a service.
    /// </summary>
    public sealed class Service
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The methods available on the service.
        /// </summary>
        public List<ServiceMethod> Methods { get; } = new List<ServiceMethod>();
    }
}
