using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Holds state used by the deserializer
        /// </summary>
        public ref struct State
        {
            internal static readonly Type ByRefType = typeof(State).MakeByRefType();
            internal static readonly Type[] ByRefTypeArray = new[] { ByRefType };
#if PLAT_SPANS
            internal SolidState Solidify() => new SolidState(
                _memory.Slice(OffsetInCurrent, RemainingInCurrent));

            internal State(ReadOnlyMemory<byte> memory) : this() => Init(memory);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Skip(int bytes)
            {
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnlySpan<byte> Consume(int bytes)
            {
                var s = Span.Slice(OffsetInCurrent, bytes);
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
                return s;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnlySpan<byte> Consume(int bytes, out int offset)
            {
                offset = OffsetInCurrent;
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
                return Span;
            }

            internal int ReadVarintUInt32(out uint tag)
            {
                Debug.Assert(RemainingInCurrent >= 5);
                tag = Span[OffsetInCurrent];
                int bytes = (tag & 0x80) == 0 ? 1 : ParseVarintUInt32Tail(
                    Span.Slice(OffsetInCurrent + 1), ref tag);
                Debug.Assert(tag != 0);
                OffsetInCurrent += bytes;
                RemainingInCurrent -= bytes;
                return bytes;
            }

            private static int ParseVarintUInt32Tail(ReadOnlySpan<byte> span, ref uint value)
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

                ThrowOverflow(null);
                return 0;
            }
#else
            internal SolidState Solidify() => default;
            internal int ReadVarintUInt32(out uint tag) => throw new NotImplementedException();
            internal int RemainingInCurrent => 0;
#endif
        }

        internal readonly struct SolidState
        {
#if PLAT_SPANS
            private readonly ReadOnlyMemory<byte> _memory;
            internal SolidState(ReadOnlyMemory<byte> memory) => _memory = memory;
            internal State Liquify() => new State(_memory);
#else
            internal State Liquify() => default;
#endif
        }
    }
}
