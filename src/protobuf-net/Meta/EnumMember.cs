using System;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Describes a named constant integer, i.e. an enum value
    /// </summary>
    public readonly struct EnumMember
    {
        /// <summary>
        /// Gets the declared name of this enum member
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of this enum member
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Create a new named enum value
        /// </summary>
        public EnumMember(object value, string name)
        {
            Name = name;
            Value = value;
        }

        internal bool HasValue => Value != null && !string.IsNullOrWhiteSpace(Name);

        internal int? TryGetInt32() => TryGetInt32(Value);
        internal static int? TryGetInt32(object value)
        {
            if (value != null)
            {
                var type = value.GetType();
                if (type.IsEnum) type = Enum.GetUnderlyingType(type);

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte: return (sbyte)value;
                    case TypeCode.Int16: return (short)value;
                    case TypeCode.Int32: return (int)value;
                    case TypeCode.Byte: return (byte)value;
                    case TypeCode.UInt16: return (ushort)value;
                    case TypeCode.UInt32:
                        var u32 = (uint)value;
                        if (u32 <= int.MaxValue) return (int)u32;
                        break;
                    case TypeCode.UInt64:
                        var u64 = (ulong)value;
                        if (u64 <= int.MaxValue) return (int)u64;
                        break;
                    case TypeCode.Int64:
                        var i64 = (long)value;
                        if (i64 >= int.MinValue && i64 <= int.MaxValue) return (int)i64;
                        break;
                }
            }
            return default;
        }
    }
}
