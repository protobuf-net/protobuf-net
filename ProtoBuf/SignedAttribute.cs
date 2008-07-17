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
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
    [ImmutableObject(true)]
    public sealed class SignedAttribute : Attribute { }
}
