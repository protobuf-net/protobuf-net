using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text;

namespace ProtoBuf.Internal
{
    //internal readonly struct GuidBytes : IEquatable<GuidBytes>
    //{
    //    internal readonly Guid ValueField;
    //    public Guid Value => ValueField;
    //    public GuidBytes(in Guid value) => ValueField = value;
    //    public static implicit operator Guid(in GuidBytes value) => value.ValueField;
    //    public static implicit operator GuidBytes(in Guid value) => new GuidBytes(value);
    //    public bool Equals(in GuidBytes other) => ValueField.Equals(other.ValueField);
    //    bool IEquatable<GuidBytes>.Equals(GuidBytes other) => ValueField.Equals(other.ValueField);
    //    public override int GetHashCode() => ValueField.GetHashCode();
    //    public override string ToString() => ValueField.ToString();
    //    public override bool Equals(object obj) => obj is GuidBytes other && ValueField.Equals(other.ValueField);
    //}

    //internal readonly struct GuidString : IEquatable<GuidString>
    //{
    //    internal readonly Guid ValueField;
    //    public Guid Value => ValueField;
    //    public GuidString(in Guid value) => ValueField = value;
    //    public static implicit operator Guid(in GuidString value) => value.ValueField;
    //    public static implicit operator GuidString(in Guid value) => new GuidString(value);
    //    public bool Equals(in GuidString other) => ValueField.Equals(other.ValueField);
    //    bool IEquatable<GuidString>.Equals(GuidString other) => ValueField.Equals(other.ValueField);
    //    public override int GetHashCode() => ValueField.GetHashCode();
    //    public override string ToString() => ValueField.ToString();
    //    public override bool Equals(object obj) => obj is GuidString other && ValueField.Equals(other.ValueField);
    //}

    internal static class GuidHelper
    {
        internal const int WRITE_BYTES_LENGTH = 16, WRITE_STRING_LENGTH = 36, MAX_LENGTH = 40;

        internal static unsafe Guid Read(ref ProtoReader.State state)
        {
            // note: we can't use a stackalloc-span here because the compiler doesn't trust
            // state, which is a ref-local (and can store spans), not to store it; since we *don't*
            // do that, we can be evil
            byte* ptr = stackalloc byte[MAX_LENGTH];
            var available = state.ReadBytes(new Span<byte>(ptr, MAX_LENGTH));

            char standardFormat;
            switch (available.Length)
            {
                case 0:
                    return Guid.Empty;
                case 16: // treat as big-endian bytes
                    // expand those bytes to hex, backwards so we don't overwrite live data
                    int write = 32;
                    for (int i = 15; i >= 0; i--)
                    {
                        var val = ptr[i];
                        ptr[--write] = ToHex(val & 0b1111);
                        ptr[--write] = ToHex((val >> 4) & 0b1111);
                    }
                    available = new Span<byte>(ptr, 32);
                    standardFormat = 'N';
                    break;
                case 32: // no hyphens
                    standardFormat = 'N';
                    break;
                case 36: // hyphens
                    standardFormat = 'D';
                    break;
                default:
                    ThrowHelper.Format($"Unexpected Guid length: {available.Length}");
                    return default;
            }

            if (!(Utf8Parser.TryParse(available, out Guid guid, out int bytes, standardFormat) && bytes == available.Length))
                ThrowHelper.Format($"Failed to read Guid: '{Encoding.UTF8.GetString(ptr, available.Length)}'");

            return guid;
            static byte ToHex(int value) => (byte)"0123456789abcdef"[value];
        }

        internal static void Write(ref ProtoWriter.State state, in Guid value, bool asBytes)
        {
            if (value.Equals(Guid.Empty))
            {
                state.WriteBytes(default(ReadOnlyMemory<byte>));
                return;
            }

            // note: we don't use stackalloc here because the ImplWriteBytes API currently takes ROM<byte>,
            // because it tries to use MemoryMarshal.TryGetArray etc if it needs to write to underlying streams
            var arr = ArrayPool<byte>.Shared.Rent(MAX_LENGTH);
            try
            {
                if (!(Utf8Formatter.TryFormat(value, arr, out int bytesWritten, asBytes ? 'N' : 'D')))
                    ThrowHelper.Format($"Failed to write Guid: '{value}'");

                if (asBytes)
                {
                    // pack down that hex to bytes
                    int read = 0;
                    Debug.Assert(bytesWritten == 2  * WRITE_BYTES_LENGTH, $"expected {2 * WRITE_BYTES_LENGTH} bytes, got {bytesWritten}");
                    for (int i = 0; i < WRITE_BYTES_LENGTH; i++)
                    {
                        arr[i] = (byte)((FromHex(arr[read++]) << 4) | FromHex(arr[read++]));
                    }
                    bytesWritten = WRITE_BYTES_LENGTH;
                }
                state.WriteBytes(new ReadOnlyMemory<byte>(arr, 0, bytesWritten));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(arr);
            }

            static int FromHex(int value)
            {
                if (value >= '0' && value <= '9')
                    return value - '0';
                if (value >= 'a' && value <= 'f')
                    return 10 + value - 'a';
                if (value >= 'A' && value <= 'F')
                    return 10 + value - 'A';
                Throw(value);
                return default;

                static void Throw(int value)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value), $"Unexpected hex character: '{(char)value}'");
                }
            };
        }
    }
}
