using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace ProtoBuf.Internal
{
    internal readonly struct GuidString : IEquatable<GuidString>
    {
        public Guid Value { get; }
        public GuidString(in Guid value) => Value = value;

        public static implicit operator Guid (in GuidString value) => value.Value;

        public static implicit operator GuidString(in Guid value) => new GuidString(value);

        public bool Equals(GuidString other) => Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public override bool Equals(object obj) => obj is GuidString other && Value.Equals(other.Value);

        internal const int WRITE_LENGTH = 36, READ_LENGTH = 40; // cheaper to wipe in ptr-size chunks

        internal static unsafe GuidString Read(ref ProtoReader.State state)
        {
            // note: we can't use a stackalloc-span here because the compiler doesn't trust
            // state, which is a ref-local (and can store spans), not to store it; since we *don't*
            // do that, we can be evil
            byte* ptr = stackalloc byte[READ_LENGTH];
            var span = new Span<byte>(ptr, READ_LENGTH);
            state.ReadBytes(ref span);
            if (!(Utf8Parser.TryParse(span, out Guid guid, out int bytes) && bytes == span.Length))
            {
                ThrowHelper.Format($"Failed to read Guid: '{Encoding.UTF8.GetString(ptr, span.Length)}'");
            }
            return guid;
        }

        internal void Write(ref ProtoWriter.State state)
        {
            // note: we don't use stackalloc here because the ImplWriteBytes API currently takes ROM<byte>,
            // because it tries to use MemoryMarshal.TryGetArray etc if it needs to write to underlying streams
            var arr = ArrayPool<byte>.Shared.Rent(WRITE_LENGTH);
            try
            {
                if (!(Utf8Formatter.TryFormat(Value, arr, out int bytesWritten)
                && bytesWritten == WRITE_LENGTH))
                {
                    ThrowHelper.Format($"Failed to write Guid: '{Value}'");
                }
                state.WriteBytes(new ReadOnlyMemory<byte>(arr, 0, WRITE_LENGTH));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(arr);
            }
        }
    }
}
