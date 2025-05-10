﻿using ProtoBuf.Internal;
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Describes a named constant integer, i.e. an enum value
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct EnumMember : IEquatable<EnumMember>, IComparable<EnumMember>
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
        /// Create a new named enum value; the value can be of the expected
        /// enum type, or an appropriate boxed enum value
        /// </summary>
        public EnumMember(object value, string name)
        {
            Name = name;
            Value = value;
        }

        internal bool HasValue => Value is not null && !string.IsNullOrWhiteSpace(Name);

        internal int? TryGetInt32() => TryGetInt32(Value);
        internal static int? TryGetInt32(object value)
        {
            if (value is not null)
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

        /// <summary>
        /// Creates a copy of this definition with a different name
        /// </summary>
        public EnumMember WithName(string name) => new EnumMember(Value, name);
        /// <summary>
        /// Creates a copy of this definition with a different value
        /// </summary>
        public EnumMember WithValue(object value) => new EnumMember(value, Name);

        /// <summary>
        /// Converts the declared value in accordance with the provided type
        /// </summary>
        public EnumMember Normalize(Type type)
            => WithValue(Normalize(Value, type));

        /// <summary>Compare a member to an enum value</summary>
        public bool Equals<T>(T value) where T : unmanaged
            => Equals(Normalize(Value, typeof(T)), Normalize(value, typeof(T)));

        /// <inheritdoc/>
        public override string ToString() => $"{Name}={Value}";

        /// <inheritdoc/>
        public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ (Value?.GetHashCode() ?? 0);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is EnumMember em && Equals(em);

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(EnumMember other) => string.Equals(Name, other.Name) && object.Equals(Value, other.Value);

        /// <summary>
        /// Indicates whether two values are considered equal.
        /// </summary>
        public static bool operator ==(EnumMember x, EnumMember y) => x.Equals(y);

        /// <summary>
        /// Indicates whether two values are considered equal.
        /// </summary>
        public static bool operator !=(EnumMember x, EnumMember y) => !x.Equals(y);

        static object Normalize(object value, Type type)
            => Convert.ChangeType(value, type.IsEnum ? Enum.GetUnderlyingType(type) : type);

        /// <summary>
        /// Create an EnumMember instance from an enum value
        /// </summary>
        public static EnumMember Create<T>(T value) where T : unmanaged
            => new EnumMember(value, value.ToString()).Normalize(typeof(T));

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                ThrowHelper.ThrowInvalidOperationException("All enum declarations must have valid names");
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
        public int CompareTo(EnumMember other)
        {
            var thisValue = TryGetInt32();
            var otherValue = other.TryGetInt32();

            if (!thisValue.HasValue && !otherValue.HasValue) return 0;
            if (!thisValue.HasValue) return 1;
            if (!otherValue.HasValue) return -1;

            if (thisValue.Value < 0 && otherValue.Value >= 0)
            {
                return 1;
            }
            if (otherValue.Value < 0 && thisValue.Value >= 0)
            {
                return -1;
            }

            return thisValue.Value.CompareTo(otherValue.Value);
        }
    }
}
