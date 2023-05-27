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
        None                      = 0,
        Standard32                = 1 << 0,
        Standard64                = 1 << 1,
        Standard128               = 1 << 2,
        
        Object                    = 1 << 3,
        Object32                  = 1 << 4,
        Object64                  = 1 << 5,
        Object128                 = 1 << 6,
        
        IsObjectable = Object | Object32 | Object64 | Object128
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