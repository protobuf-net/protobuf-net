using System;
using System.Reflection;

namespace ProtoBuf
{
    /// <summary>
    /// Declares that the value type to which it is applied is a strongly typed wrapper
    /// for a single, primitive type: For example a "CustomerID" struct that simply contains an
    /// int64. 
    /// Fields having that strong type are serialized as if it had just been just that in64
    /// value, rather than as a nested structure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct,
        AllowMultiple = false, Inherited = true)]
    public sealed class ProtoScalarAttribute : Attribute
    {
        public DataFormat DataFormat { get; set; }
    }
}

