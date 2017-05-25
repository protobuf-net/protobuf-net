using System.Runtime.InteropServices;

namespace ProtoBuf
{
    /// <summary>Represent multiple types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    public struct DiscriminatedUnionObject
    {
        private int _discriminator;

        /// <summary>The value typed as Object</summary>
        public readonly object Object;

        private DiscriminatedUnionObject(int discriminator) : this()
        {
            _discriminator = ~discriminator; // avoids issues with default value / 0
        }

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == ~discriminator;

        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnionObject(int discriminator, object value) : this(discriminator) { Object = value; }

        /// <summary>Reset a value if the specified discriminator is assigned</summary>
        public static void Reset(ref DiscriminatedUnionObject value, int discriminator)
        {
            if (value.Is(discriminator)) value = default(DiscriminatedUnionObject);
        }
    }

    /// <summary>Represent multiple types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion64
    {
        [FieldOffset(0)] private int _discriminator;

        /// <summary>The value typed as Int64</summary>
        [FieldOffset(8)] public readonly long Int64;
        /// <summary>The value typed as UInt64</summary>
        [FieldOffset(8)] public readonly ulong UInt64;
        /// <summary>The value typed as Int32</summary>
        [FieldOffset(8)] public readonly int Int32;
        /// <summary>The value typed as UInt32</summary>
        [FieldOffset(8)] public readonly uint UInt32;
        /// <summary>The value typed as Boolean</summary>
        [FieldOffset(8)] public bool Boolean;
        /// <summary>The value typed as Single</summary>
        [FieldOffset(8)] public float Single;
        /// <summary>The value typed as Double</summary>
        [FieldOffset(8)] public double Double;

        private DiscriminatedUnion64(int discriminator) : this()
        {
            _discriminator = ~discriminator; // avoids issues with default value / 0
        }

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == ~discriminator;

        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64(int discriminator, long value) : this(discriminator) { Int64 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64(int discriminator, int value) : this(discriminator) { Int32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64(int discriminator, ulong value) : this(discriminator) { UInt64 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64(int discriminator, uint value) : this(discriminator) { UInt32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64(int discriminator, float value) : this(discriminator) { Single = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64(int discriminator, double value) : this(discriminator) { Double = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64(int discriminator, bool value) : this(discriminator) { Boolean = value; }

        /// <summary>Reset a value if the specified discriminator is assigned</summary>
        public static void Reset(ref DiscriminatedUnion64 value, int discriminator)
        {
            if (value.Is(discriminator)) value = default(DiscriminatedUnion64);
        }
    }

    /// <summary>Represent multiple types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion64Object
    {
        [FieldOffset(0)] private int _discriminator;

        /// <summary>The value typed as Int64</summary>
        [FieldOffset(8)] public readonly long Int64;
        /// <summary>The value typed as UInt64</summary>
        [FieldOffset(8)] public readonly ulong UInt64;
        /// <summary>The value typed as Int32</summary>
        [FieldOffset(8)] public readonly int Int32;
        /// <summary>The value typed as UInt32</summary>
        [FieldOffset(8)] public readonly uint UInt32;
        /// <summary>The value typed as Boolean</summary>
        [FieldOffset(8)] public bool Boolean;
        /// <summary>The value typed as Single</summary>
        [FieldOffset(8)] public float Single;
        /// <summary>The value typed as Double</summary>
        [FieldOffset(8)] public double Double;
        /// <summary>The value typed as Double</summary>
        [FieldOffset(16)] public object Object;

        private DiscriminatedUnion64Object(int discriminator) : this()
        {
            _discriminator = ~discriminator; // avoids issues with default value / 0
        }

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == ~discriminator;

        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, long value) : this(discriminator) { Int64 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, int value) : this(discriminator) { Int32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, ulong value) : this(discriminator) { UInt64 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, uint value) : this(discriminator) { UInt32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, float value) : this(discriminator) { Single = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, double value) : this(discriminator) { Double = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, bool value) : this(discriminator) { Boolean = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion64Object(int discriminator, object value) : this(discriminator) { Object = value; }

        /// <summary>Reset a value if the specified discriminator is assigned</summary>
        public static void Reset(ref DiscriminatedUnion64Object value, int discriminator)
        {
            if (value.Is(discriminator)) value = default(DiscriminatedUnion64Object);
        }
    }

    /// <summary>Represent multiple types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion32
    {
        [FieldOffset(0)] private int _discriminator;

        /// <summary>The value typed as Int32</summary>
        [FieldOffset(4)] public readonly int Int32;
        /// <summary>The value typed as UInt32</summary>
        [FieldOffset(4)] public readonly uint UInt32;
        /// <summary>The value typed as Boolean</summary>
        [FieldOffset(4)] public bool Boolean;
        /// <summary>The value typed as Single</summary>
        [FieldOffset(4)] public float Single;

        private DiscriminatedUnion32(int discriminator) : this()
        {
            _discriminator = ~discriminator; // avoids issues with default value / 0
        }

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == ~discriminator;

        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32(int discriminator, int value) : this(discriminator) { Int32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32(int discriminator, uint value) : this(discriminator) { UInt32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32(int discriminator, float value) : this(discriminator) { Single = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32(int discriminator, bool value) : this(discriminator) { Boolean = value; }

        /// <summary>Reset a value if the specified discriminator is assigned</summary>
        public static void Reset(ref DiscriminatedUnion32 value, int discriminator)
        {
            if (value.Is(discriminator)) value = default(DiscriminatedUnion32);
        }
    }

    /// <summary>Represent multiple types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DiscriminatedUnion32Object
    {
        [FieldOffset(0)] private int _discriminator;

        /// <summary>The value typed as Int32</summary>
        [FieldOffset(4)] public readonly int Int32;
        /// <summary>The value typed as UInt32</summary>
        [FieldOffset(4)] public readonly uint UInt32;
        /// <summary>The value typed as Boolean</summary>
        [FieldOffset(4)] public bool Boolean;
        /// <summary>The value typed as Single</summary>
        [FieldOffset(4)] public float Single;
        /// <summary>The value typed as Double</summary>
        [FieldOffset(8)] public object Object;

        private DiscriminatedUnion32Object(int discriminator) : this()
        {
            _discriminator = ~discriminator; // avoids issues with default value / 0
        }

        /// <summary>Indicates whether the specified discriminator is assigned</summary>
        public bool Is(int discriminator) => _discriminator == ~discriminator;

        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32Object(int discriminator, int value) : this(discriminator) { Int32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32Object(int discriminator, uint value) : this(discriminator) { UInt32 = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32Object(int discriminator, float value) : this(discriminator) { Single = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32Object(int discriminator, bool value) : this(discriminator) { Boolean = value; }
        /// <summary>Create a new discriminated union value</summary>
        public DiscriminatedUnion32Object(int discriminator, object value) : this(discriminator) { Object = value; }

        /// <summary>Reset a value if the specified discriminator is assigned</summary>
        public static void Reset(ref DiscriminatedUnion32Object value, int discriminator)
        {
            if (value.Is(discriminator)) value = default(DiscriminatedUnion32Object);
        }
    }
}
