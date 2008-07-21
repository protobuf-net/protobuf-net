using System;
using System.ComponentModel;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates that protocol-buffer serialization should use "zigzag" encoding;
    /// this is useful when a signed integer may frequently have negative values,
    /// significantly reducing the space required - but means that positive values
    /// may require an additional byte slightly sooner (one bit sooner, or a factor
    /// of two).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
    public sealed class ProtoMemberAttribute : Attribute
    {
        /// <summary>
        /// Creates a new ProtoMemberAttribute instance.
        /// </summary>
        /// <param name="tag">Specifies the unique tag used to identify this member within the type.</param>
        public ProtoMemberAttribute(int tag) {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag");
            Tag = tag;
        }
        
        /// <summary>
        /// Specifies the original name defined in the .proto; not used
        /// during serialization.
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        private string name;
        /// <summary>
        /// Specifies the data-format to be used when encoding this value.
        /// </summary>
        public DataFormat DataFormat { get { return dataFormat; } set { dataFormat = value; } }
        private DataFormat dataFormat; 
        /// <summary>
        /// Specifies the unique tag used to identify this member within the type.
        /// </summary>
        public int Tag { get { return tag; } private set { tag = value; } }
        private int tag;
        /// <summary>
        /// Indicates whether this member is mandatory.
        /// </summary>
        public bool IsRequired { get { return isRequired; } set { isRequired = value; } }
        private bool isRequired;
    }
}
