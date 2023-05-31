 using System;

 namespace ProtoBuf
{
    /// <summary>
    /// Specifies the type of discriminated union implementation used.
    /// See: <see cref="DiscriminatedUnion32"/>, <see cref="DiscriminatedUnion32Object"/>, etc. 
    /// </summary>
    [Flags]
    public enum DiscriminatedUnionType
    {
        /// <summary>
        /// Zero value. No union type specified
        /// </summary>
        None                      = 0,

        /// <summary>
        /// Represents <see cref="DiscriminatedUnion32"/> type
        /// </summary>
        Standard32                = 1 << 0,
        /// <summary>
        /// Represents <see cref="DiscriminatedUnion64"/> type
        /// </summary>
        Standard64                = 1 << 1,
        /// <summary>
        /// Represents <see cref="DiscriminatedUnion128"/> type
        /// </summary>
        Standard128               = 1 << 2,
        
        /// <summary>
        /// Represents <see cref="DiscriminatedUnionObject"/> type
        /// </summary>
        Object                    = 1 << 3,
        /// <summary>
        /// Represents <see cref="DiscriminatedUnion32Object"/> type
        /// </summary>
        Object32                  = 1 << 4,
        /// <summary>
        /// Represents <see cref="DiscriminatedUnion64Object"/> type
        /// </summary>
        Object64                  = 1 << 5,
        /// <summary>
        /// Represents <see cref="DiscriminatedUnion128Object"/> type
        /// </summary>
        Object128                 = 1 << 6,
    }
    
    /// <summary>
    /// Helpers for <see cref="DiscriminatedUnionType"/>
    /// </summary>
    public static class DiscriminatedUnionTypeExtensions
    {
        /// <summary>
        /// Returns string name of corresponding DiscriminatedUnion for <see cref="DiscriminatedUnionType"/>
        /// </summary>
        public static string GetTypeName(this DiscriminatedUnionType type) => type switch
        {
            DiscriminatedUnionType.Object => nameof(DiscriminatedUnionObject),
            DiscriminatedUnionType.Standard32 => nameof(DiscriminatedUnion32),
            DiscriminatedUnionType.Object32 => nameof(DiscriminatedUnion32Object),
            DiscriminatedUnionType.Standard64 => nameof(DiscriminatedUnion64),
            DiscriminatedUnionType.Object64 => nameof(DiscriminatedUnion64Object),
            DiscriminatedUnionType.Standard128 => nameof(DiscriminatedUnion128),
            DiscriminatedUnionType.Object128 => nameof(DiscriminatedUnion128Object),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}