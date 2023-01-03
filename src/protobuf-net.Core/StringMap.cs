using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    /// <summary>
    /// Not yet implemented
    /// </summary>
    public interface IStringParser<T>
    {
        /// <summary>
        /// Parse the provided payload as a value of <typeparamref name="T"/>.
        /// </summary>
        T Parse(ReadOnlyMemory<byte> bytes);

        /// <summary>
        /// Attempt to format a <paramref name="value"/> of <typeparamref name="T"/> by providing either a well-known payload via <paramref name="bytes"/> (returning <c>true</c> if possible), or by measuring <paramref name="value"/> and providing the <paramref name="length"/> (returning <c>false</c>).
        /// </summary>
        /// <returns></returns>
        bool TryFormat(T value, out ReadOnlyMemory<byte> bytes, out int length);

        /// <summary>
        /// Format a <paramref name="value"/> of <typeparamref name="T"/> into <paramref name="bytes"/>, the length of which was provided via <see cref="TryFormat(T, out ReadOnlyMemory{byte}, out int)"/>.
        /// </summary>
        void Format(T value, Memory<byte> bytes);
    }

    /// <summary>
    /// Not yet implemented
    /// </summary>
    public sealed class StringMap : IStringParser<string>
    {
        private readonly ImmutableDictionary<string, ReadOnlyMemory<byte>> _byString;
        private readonly ImmutableDictionary<ReadOnlyMemory<byte>, string> _byBytes;

        /// <summary>
        /// Create a new <see cref="StringMap"/> from the provided <paramref name="values"/>.
        /// </summary>
        public StringMap(params string[] values)
        {
            if (values is null || values.Length == 0)
            {
                _byString = ImmutableDictionary<string, ReadOnlyMemory<byte>>.Empty;
                _byBytes = ImmutableDictionary<ReadOnlyMemory<byte>, string>.Empty;
            }
            else
            {
                var byStringBuilder = ImmutableDictionary.CreateBuilder(StringComparer.Ordinal, BytesComparer.Instance);
                var byBytesBuilder = ImmutableDictionary.CreateBuilder(BytesComparer.Instance, StringComparer.Ordinal);

                foreach (var value in values)
                {
                    if (!(string.IsNullOrEmpty(value) || byStringBuilder.ContainsKey(value)))
                    {
                        ReadOnlyMemory<byte> bytes = ProtoWriter.UTF8.GetBytes(value);
                        byStringBuilder[value] = bytes;
                        byBytesBuilder[bytes] = value;
                    }
                }
                _byString = byStringBuilder.ToImmutable();
                _byBytes = byBytesBuilder.ToImmutable();
            }
        }

        /// <inheritdoc/>
        public string Parse(ReadOnlyMemory<byte> bytes)
        {
            if (bytes.IsEmpty) return "";
            if (_byBytes.TryGetValue(bytes, out var s)) return s;
#if PLAT_SPAN_OVERLOADS
            return ProtoWriter.UTF8.GetString(bytes.Span);
#else
            unsafe
            {
                fixed (byte* ptr = bytes.Span)
                {
                    return ProtoWriter.UTF8.GetString(ptr, bytes.Length);
                }
            }
#endif
        }

        /// <inheritdoc/>
        public bool TryFormat(string value, out ReadOnlyMemory<byte> bytes, out int length)
        {
            if (string.IsNullOrEmpty(value))
            {
                bytes = default;
                length = 0;
                return true;
            }
            if (_byString.TryGetValue(value, out bytes))
            {
                length = bytes.Length;
                return true;
            }
            length = ProtoWriter.UTF8.GetByteCount(value);
            return false;
        }

        /// <inheritdoc/>
        public void Format(string value, Memory<byte> bytes)
        {
            int count;
#if PLAT_SPAN_OVERLOADS
            count = ProtoWriter.UTF8.GetBytes(value.AsSpan(), bytes.Span);
#else
            unsafe
            {
                fixed (char* cPtr = value)
                fixed (byte* bPtr = bytes.Span)
                {
                    count = ProtoWriter.UTF8.GetBytes(cPtr, value.Length, bPtr, bytes.Length);
                }
            }
#endif
            Debug.Assert(count == bytes.Length);
        }

        internal sealed class BytesComparer : IEqualityComparer<ReadOnlyMemory<byte>>
        {
            internal static readonly BytesComparer Instance = new();
            private BytesComparer() { }

            public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
                => x.Span.SequenceEqual(y.Span);

            public int GetHashCode(ReadOnlyMemory<byte> obj)
            {
                if (obj.IsEmpty) return 0;
                var b8 = obj.Span;
                var hash = b8.Length;

#if NETCOREAPP3_1_OR_GREATER // from benchmarks, on netx benefit is nil (or negative) vs ulong approach
                if (Vector.IsHardwareAccelerated && b8.Length >= (Vector<int>.Count * sizeof(int)))
                {
                    var b32Vec = MemoryMarshal.Cast<byte, Vector<int>>(b8);
                    var rollup = Vector<int>.Zero;
                    foreach (ref readonly var value in b32Vec)
                    {
                        rollup = Vector.Add(Vector.Multiply(37, rollup), value);
                    }
#if NET6_0_OR_GREATER
                    hash = 37 * hash + Vector.Sum(rollup);
#else
                    hash = 37 * hash + Vector.Dot(rollup, Vector<int>.One);
#endif
                    b8 = b8.Slice(b32Vec.Length * Vector<int>.Count * sizeof(int));
                }
#endif
                if (b8.Length >= sizeof(ulong))
                {
                    var b64 = MemoryMarshal.Cast<byte, ulong>(b8);
                    foreach (var value in b64)
                    {
                        hash = 37 * hash + value.GetHashCode();
                    }
                    b8 = b8.Slice(b64.Length * sizeof(ulong));
                }
                // mop up anything at the end
                foreach (var value in b8)
                {
                    hash = 37 * hash + value;
                }
                return hash;
            }
        }
    }
}
