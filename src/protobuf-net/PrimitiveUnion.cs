using System.Runtime.InteropServices;

namespace ProtoBuf
{
    /// <summary>Represent multiple 64-bit types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PrimitiveUnion64
    {
        /// <summary>Access the value as an Int64</summary>
        [FieldOffset(0)] public long Int64;
        /// <summary>Access the value as an UInt64</summary>
        [FieldOffset(0)] public ulong UInt64;
        /// <summary>Access the value as an Int32</summary>
        [FieldOffset(0)] public int Int32;
        /// <summary>Access the value as an UInt32</summary>
        [FieldOffset(0)] public uint UInt32;
        /// <summary>Access the value as a Boolean</summary>
        [FieldOffset(0)] public bool Boolean;
        /// <summary>Access the value as a Single</summary>
        [FieldOffset(0)] public float Single;
        /// <summary>Access the value as a Double</summary>
        [FieldOffset(0)] public double Double;
    }

    /// <summary>Represent multiple 32-bit types as a union; this is used as part of OneOf -
    /// note that it is the caller's responsbility to only read/write the value as the same type</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PrimitiveUnion32
    {
        /// <summary>Access the value as an Int32</summary>
        [FieldOffset(0)] public int Int32;
        /// <summary>Access the value as an UInt32</summary>
        [FieldOffset(0)] public uint UInt32;
        /// <summary>Access the value as a Boolean</summary>
        [FieldOffset(0)] public bool Boolean;
        /// <summary>Access the value as a Single</summary>
        [FieldOffset(0)] public float Single;
    }
}
