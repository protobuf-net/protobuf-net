using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates that a type is defined for protocol-buffer serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class,
        AllowMultiple = false, Inherited = true)]
    public sealed class ProtoContractAttribute : Attribute
    {
        /// <summary>
        /// The defined name of the type.
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        private string name;
    }
}