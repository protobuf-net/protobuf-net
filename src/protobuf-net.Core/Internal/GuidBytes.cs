using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text;

namespace ProtoBuf.Internal
{
    internal readonly struct GuidBytes : IEquatable<GuidBytes>
    {
        public Guid Value { get; }
        public GuidBytes(in Guid value) => Value = value;

        public static implicit operator Guid(in GuidBytes value) => value.Value;

        public static implicit operator GuidBytes(in Guid value) => new GuidBytes(value);

        public bool Equals(GuidBytes other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public override bool Equals(object obj) => obj is GuidBytes other && Value.Equals(other.Value);

        internal const int LENGTH = 16;

        internal static unsafe GuidBytes Read(ref ProtoReader.State state)
        {
            // note: we can't use a stackalloc-span here because the compiler doesn't trust
            // state, which is a ref-local (and can store spans), not to store it; since we *don't*
            // do that, we can be evil
            byte* ptr = stackalloc byte[LENGTH * 2];
            var full = new Span<byte>(ptr, LENGTH * 2);
            var partial = full;
            state.ReadBytes(ref partial);
            if (partial.Length != LENGTH)
                ThrowHelper.Format("Failed to read Guid");

            // expand those bytes to hex, backwards so we don't overwrite live data
            int write = LENGTH * 2;
            for (int i = LENGTH - 1; i >= 0; i--)
            {
                var val = ptr[i];
                ptr[--write] = ToHex(val & 0b1111);
                ptr[--write] = ToHex((val >> 4) & 0b1111);
            }
            if (!(Utf8Parser.TryParse(full, out Guid guid, out int bytes, 'N') && bytes == LENGTH * 2))
                ThrowHelper.Format("Failed to read Guid");

            return guid;

            static byte ToHex(int value) => (byte)"0123456789abcdef"[value];
        }

        internal void Write(ref ProtoWriter.State state)
        {
            // note: we don't use stackalloc here because the ImplWriteBytes API currently takes ROM<byte>,
            // because it tries to use MemoryMarshal.TryGetArray etc if it needs to write to underlying streams
            var arr = ArrayPool<byte>.Shared.Rent(LENGTH * 2);
            try
            {
                if (!(Utf8Formatter.TryFormat(Value, arr, out int bytesWritten, new StandardFormat('N')) && bytesWritten == LENGTH * 2))
                    ThrowHelper.Format($"Failed to write Guid");

                // pack down that hex to bytes
                int read = 0;
                for(int i = 0; i < LENGTH; i++)
                {
                    arr[i] = (byte)((FromHex(arr[read++]) << 4) | FromHex(arr[read++]));
                }
                state.WriteBytes(new ReadOnlyMemory<byte>(arr, 0, LENGTH));
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
                throw new ArgumentOutOfRangeException(nameof(value));
            };
        }
    }
}
