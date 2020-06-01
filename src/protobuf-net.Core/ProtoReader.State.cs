using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Get the default state associated with this reader
        /// </summary>
        protected internal abstract State DefaultState();

        /// <summary>
        /// Holds state used by the deserializer
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public ref partial struct State
        {
            /// <summary>
            /// Release any resources associated with this instance
            /// </summary>
            public void Dispose()
            {
                var reader = _reader;
                this = default;
                reader?.Dispose();
            }
            internal SolidState Solidify() => new SolidState(
                _reader,
                _memory.Slice(OffsetInCurrent, RemainingInCurrent));

            internal State(ProtoReader reader, ReadOnlyMemory<byte> memory)
                : this(reader)
            {
                Init(memory);
            }

            internal State(ProtoReader reader)
            {
                this = default;
                _reader = reader;
            }

#pragma warning disable IDE0044 // make readonly
            private ProtoReader _reader;
#pragma warning restore IDE0044 // make readonly

            internal void Init(ReadOnlyMemory<byte> memory)
            {
                _memory = memory;
                Span = memory.Span;
                OffsetInCurrent = 0;
                RemainingInCurrent = Span.Length;
            }
            internal ReadOnlySpan<byte> Span { get; private set; }
            private ReadOnlyMemory<byte> _memory;

            internal int OffsetInCurrent { get; private set; }
            internal int RemainingInCurrent { get; private set; }

            [MethodImpl(HotPath)]
            internal void Skip(int bytes)
            {
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
            }
            [MethodImpl(HotPath)]
            internal ReadOnlySpan<byte> Consume(int bytes)
            {
                var s = Span.Slice(OffsetInCurrent, bytes);
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
                return s;
            }

            [MethodImpl(HotPath)]
            internal ReadOnlySpan<byte> Consume(int bytes, out int offset)
            {
                offset = OffsetInCurrent;
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
                return Span;
            }

            internal int ReadVarintUInt32(out uint value)
            {
                Debug.Assert(RemainingInCurrent >= 5);
                value = Span[OffsetInCurrent];
                int bytes = (value & 0x80) == 0 ? 1 : ParseVarintUInt32Tail(
                    Span.Slice(OffsetInCurrent), ref value);
                Debug.Assert(value != 0);
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
                return bytes;
            }

            [MethodImpl(HotPath)]
            internal int ParseVarintUInt32(ReadOnlySpan<byte> span, out uint value)
            {
                value = span[0];
                return (value & 0x80) == 0 ? 1 : ParseVarintUInt32Tail(span, ref value);
            }

            [MethodImpl(HotPath)]
            internal int ParseVarintUInt32(ReadOnlySpan<byte> span, int offset, out uint value)
            {
                value = span[offset];
                return (value & 0x80) == 0 ? 1 : ParseVarintUInt32Tail(span.Slice(offset), ref value);
            }
            private int ParseVarintUInt32Tail(ReadOnlySpan<byte> span, ref uint value)
            {
                uint chunk = span[1];
                value = (value & 0x7F) | (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;

                chunk = span[2];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;

                chunk = span[3];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;

                chunk = span[4];
                value |= chunk << 28; // can only use 4 bits from this chunk
                if ((chunk & 0xF0) == 0) return 5;

                ThrowOverflow();
                return 0;
            }

            [MethodImpl(HotPath)]
            internal void Advance(long count) => _reader.Advance(count);

            internal static int TryParseUInt64Varint(ReadOnlySpan<byte> span, int offset, out ulong value)
            {
                if ((uint)offset >= (uint)span.Length)
                {
                    value = 0;
                    return 0;
                }
                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                ulong chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;

                if ((uint)offset >= (uint)span.Length) NoContextThrowEoF();
                chunk = span[offset];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) NoContextThrowOverflow();
                return 10;
            }

            [MethodImpl(HotPath)]
            internal ProtoReader GetReader() => _reader;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void NoContextThrowEoF() => default(State).ThrowEoF();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void NoContextThrowOverflow() => default(State).ThrowOverflow();

        [StructLayout(LayoutKind.Auto)]
        internal readonly struct SolidState : IDisposable
        {
            public void Dispose() => _reader?.Dispose();
            private readonly ReadOnlyMemory<byte> _memory;
            private readonly ProtoReader _reader;
            internal SolidState(ProtoReader reader, ReadOnlyMemory<byte> memory)
            {
                _memory = memory;
                _reader = reader;
            }
            internal State Liquify() => new State(_reader, _memory);
        }
    }
}
