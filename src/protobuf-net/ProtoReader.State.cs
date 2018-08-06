using System;
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
#else
            internal SolidState Solidify() => default;
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
