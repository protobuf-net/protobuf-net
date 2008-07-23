using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates that a type is defined for protocol-buffer serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum,
        AllowMultiple = false, Inherited = true)]
    public sealed class ProtoContractAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the defined name of the type.
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        private string name;
    }
}