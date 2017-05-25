using System.Runtime.InteropServices;

namespace ProtoBuf
{
    /// <summary>Represent multiple 64-bit types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    public struct DiscriminatedUnionRef
    {
        private int _discriminator;
        private object _obj;

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == discriminator;
        /// <summary>Reset if the specified discriminator is assigned</summary>
        public void Reset(int discriminator) { if (_discriminator == discriminator) _discriminator = 0; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public object GetObject(int discriminator) => _discriminator == discriminator ? _obj : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, object value) { _obj = value; _discriminator = discriminator; }

    }

    /// <summary>Represent multiple 64-bit types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion64
    {
        [FieldOffset(0)] private int _discriminator;

        [FieldOffset(4)] private long _long;
        [FieldOffset(4)] private ulong _ulong;
        [FieldOffset(4)] private int _int;
        [FieldOffset(4)] private uint _uint;
        [FieldOffset(4)] private bool _bool;
        [FieldOffset(4)] private float _float;
        [FieldOffset(4)] private double _double;

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == discriminator;
        /// <summary>Reset if the specified discriminator is assigned</summary>
        public void Reset(int discriminator) { if (_discriminator == discriminator) _discriminator = 0; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public long? GetInt64(int discriminator) => _discriminator == discriminator ? (long?)_long : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, long value) { _long = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public int? GetInt32(int discriminator) => _discriminator == discriminator ? (int?)_int : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, int value) { _int = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public ulong? GetUInt64(int discriminator) => _discriminator == discriminator ? (ulong?)_ulong : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, ulong value) { _ulong = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public ulong? GetUInt32(int discriminator) => _discriminator == discriminator ? (uint?)_uint : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, uint value) { _uint = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public bool? GetBoolean(int discriminator) => _discriminator == discriminator ? (bool?)_bool : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, bool value) { _bool = value; _discriminator = discriminator; }


        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public float? GetSingle(int discriminator) => _discriminator == discriminator ? (float?)_float : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, float value) { _float = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public double? GetDouble(int discriminator) => _discriminator == discriminator ? (double?)_double : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, double value) { _double = value; _discriminator = discriminator; }
    }

    /// <summary>Represent multiple 64-bit types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion64Ref
    {
        [FieldOffset(0)] private int _discriminator;

        [FieldOffset(8)] private long _long;
        [FieldOffset(8)] private ulong _ulong;
        [FieldOffset(8)] private int _int;
        [FieldOffset(8)] private uint _uint;
        [FieldOffset(8)] private bool _bool;
        [FieldOffset(8)] private float _float;
        [FieldOffset(8)] private double _double;
        [FieldOffset(16)] private object _obj;

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == discriminator;
        /// <summary>Reset if the specified discriminator is assigned</summary>
        public void Reset(int discriminator) { if (_discriminator == discriminator) _discriminator = 0; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public long? GetInt64(int discriminator) => _discriminator == discriminator ? (long?)_long : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, long value) { _long = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public int? GetInt32(int discriminator) => _discriminator == discriminator ? (int?)_int : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, int value) { _int = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public ulong? GetUInt64(int discriminator) => _discriminator == discriminator ? (ulong?)_ulong : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, ulong value) { _ulong = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public ulong? GetUInt32(int discriminator) => _discriminator == discriminator ? (uint?)_uint : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, uint value) { _uint = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public bool? GetBoolean(int discriminator) => _discriminator == discriminator ? (bool?)_bool : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, bool value) { _bool = value; _discriminator = discriminator; }


        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public float? GetSingle(int discriminator) => _discriminator == discriminator ? (float?)_float : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, float value) { _float = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public double? GetDouble(int discriminator) => _discriminator == discriminator ? (double?)_double : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, double value) { _double = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public object GetObject(int discriminator) => _discriminator == discriminator ? _obj : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, object value) { _obj = value; _discriminator = discriminator; }
    }


    /// <summary>Represent multiple 64-bit types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion32
    {
        [FieldOffset(0)] private int _discriminator;

        [FieldOffset(4)] private int _int;
        [FieldOffset(4)] private uint _uint;
        [FieldOffset(4)] private bool _bool;
        [FieldOffset(4)] private float _float;

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == discriminator;
        /// <summary>Reset if the specified discriminator is assigned</summary>
        public void Reset(int discriminator) { if (_discriminator == discriminator) _discriminator = 0; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public int? GetInt32(int discriminator) => _discriminator == discriminator ? (int?)_int : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, int value) { _int = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public ulong? GetUInt32(int discriminator) => _discriminator == discriminator ? (uint?)_uint : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, uint value) { _uint = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public bool? GetBoolean(int discriminator) => _discriminator == discriminator ? (bool?)_bool : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, bool value) { _bool = value; _discriminator = discriminator; }


        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public float? GetSingle(int discriminator) => _discriminator == discriminator ? (float?)_float : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, float value) { _float = value; _discriminator = discriminator; }
    }

    /// <summary>Represent multiple 64-bit types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion32Ref
    {
        [FieldOffset(0)] private int _discriminator;

        [FieldOffset(4)] private int _int;
        [FieldOffset(4)] private uint _uint;
        [FieldOffset(4)] private bool _bool;
        [FieldOffset(4)] private float _float;
        [FieldOffset(8)] private object _obj;

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == discriminator;
        /// <summary>Reset if the specified discriminator is assigned</summary>
        public void Reset(int discriminator) { if (_discriminator == discriminator) _discriminator = 0; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public int? GetInt32(int discriminator) => _discriminator == discriminator ? (int?)_int : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, int value) { _int = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public ulong? GetUInt32(int discriminator) => _discriminator == discriminator ? (uint?)_uint : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, uint value) { _uint = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public bool? GetBoolean(int discriminator) => _discriminator == discriminator ? (bool?)_bool : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, bool value) { _bool = value; _discriminator = discriminator; }


        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public float? GetSingle(int discriminator) => _discriminator == discriminator ? (float?)_float : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, float value) { _float = value; _discriminator = discriminator; }

        /// <summary>Get the typed value only if the discriminator is a match</summary>
        public object GetObject(int discriminator) => _discriminator == discriminator ? _obj : null;
        /// <summary>Set the typed value and discriminator</summary>
        public void Set(int discriminator, object value) { _obj = value; _discriminator = discriminator; }
    }
}
