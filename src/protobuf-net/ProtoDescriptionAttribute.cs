using System;

namespace ProtoBuf
{
    /// <summary>
    /// For descriptions of classes, properties, and fields, you can use it to generate corresponding comments when generating .proto files.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property
        | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public class ProtoDescriptionAttribute : Attribute
    {
        /// <summary>
        /// ProtoDescriptionAttribute constructor
        /// </summary>
        public ProtoDescriptionAttribute() { }
        /// <summary>
        /// ProtoDescriptionAttribute constructor
        /// </summary>
        /// <param name="description"></param>
        public ProtoDescriptionAttribute(string description)
        {
            Description = description;
        }
        /// <summary>
        /// Gets or sets the field description
        /// </summary>
        public string Description { set; get; }
    }
}
