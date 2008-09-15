using System;

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

        #if NET_3_0

        private bool inferTagFromName;
        /// <summary>
        /// Enables automatic tag generation based on the existing name / order
        /// of the defined members. This option is not used for members marked
        /// with ProtoMemberAttribute, as intended to provide compatibility with
        /// WCF serialization. WARNING: when adding new fields you must take
        /// care to increase the Order for new elements, otherwise data corruption
        /// may occur.
        /// </summary>
        public bool InferTagFromName
        {
            get { return inferTagFromName; }
            set { inferTagFromName = value; }
        }

        #endif
    }
}