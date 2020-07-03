using System.Collections.Immutable;

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
        public string Name { get; }

        /// <summary>
        /// The methods available on the service.
        /// </summary>
        public ImmutableArray<ServiceMethod> Methods { get; }

        /// <summary>
        /// Creates a new <see cref="Service"/> instance.
        /// </summary>
        public Service(string name, ImmutableArray<ServiceMethod> methods)
        {
            Name = name;
            Methods = methods.IsEmpty ? ImmutableArray<ServiceMethod>.Empty : methods;
        }
    }
}
