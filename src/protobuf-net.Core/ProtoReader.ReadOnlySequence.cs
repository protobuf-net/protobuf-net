using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Creates a new reader against a multi-segment buffer
        /// </summary>
        /// <param name="source">The source buffer</param>
        /// <param name="state">Reader state</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        public static ProtoReader Create(out State state, ReadOnlySequence<byte> source, TypeModel model, SerializationContext context = null)
        {
            var reader = ReadOnlySequenceProtoReader.GetRecycled()
                ?? new ReadOnlySequenceProtoReader();
            reader.Init(out state, source, model, context);
            return reader;
        }

        /// <summary>
        /// Creates a new reader against a multi-segment buffer
        /// </summary>
        /// <param name="source">The source buffer</param>
        /// <param name="state">Reader state</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        public static ProtoReader Create(out State state, ReadOnlyMemory<byte> source, TypeModel model, SerializationContext context = null)
            => Create(out state, new ReadOnlySequence<byte>(source), model, context);

        private sealed class ReadOnlySequenceProtoReader : ProtoReader
        {
            protected internal override State DefaultState() => throw new InvalidOperationException("You must retain and pass the state from ProtoReader.Create");

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static string ToString(ReadOnlySpan<byte> span, int offset, int bytes)
            {
#if PLAT_SPAN_OVERLOADS
                return UTF8.GetString(span.Slice(offset, bytes));
#else
                unsafe
                {
                    fixed (byte* sPtr = &MemoryMarshal.GetReference(span))
                    {
                        var bPtr = sPtr + offset;
                        int chars = UTF8.GetCharCount(bPtr, bytes);
                        string s = new string('\0', chars);
                        fixed (char* cPtr = s)
                        {
                            UTF8.GetChars(bPtr, bytes, cPtr, chars);
                        }
                        return s;
                    }
                }
#endif
            }
            internal static int TryParseUInt32Varint(ProtoReader @this, ref State state, int offset, bool trimNegative, out uint value, ReadOnlySpan<byte> span)
            {
                if ((uint)offset >= (uint)span.Length)
                {
                    value = 0;
                    return 0;
                }

                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this, ref state);
                uint chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this, ref state);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this, ref state);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this, ref state);
                chunk = span[offset++];
                value |= chunk << 28; // can only use 4 bits from this chunk
                if ((chunk & 0xF0) == 0) return 5;

                if (trimNegative // allow for -ve values
                    && (chunk & 0xF0) == 0xF0
                    && offset + 4 < (uint)span.Length
                        && span[offset] == 0xFF
                        && span[offset + 1] == 0xFF
                        && span[offset + 2] == 0xFF
                        && span[offset + 3] == 0xFF
                        && span[offset + 4] == 0x01)
                {
                    return 10;
                }

                ThrowOverflow(@this, ref state);
                return 0;
            }

            [ThreadStatic]
            private static ReadOnlySequenceProtoReader s_lastReader;
            private ReadOnlySequence<byte>.Enumerator _source;

            internal static ReadOnlySequenceProtoReader GetRecycled()
            {
                var tmp = s_lastReader;
                s_lastReader = null;
                return tmp;
            }

            internal override void Recycle()
            {
                Dispose();
                s_lastReader = this;
            }

            public override void Dispose()
            {
                base.Dispose();
                _source = default;
            }

            internal void Init(out State state, ReadOnlySequence<byte> source, TypeModel model, SerializationContext context)
            {
                base.Init(model, context);
                _source = source.GetEnumerator();
                state = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetSomeData(ref State state, bool throwIfEOF = true)
            {
                var data = state.RemainingInCurrent;
                return data == 0 ? ReadNextBuffer(ref state, throwIfEOF) : data;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private int ReadNextBuffer(ref State state, bool throwIfEOF)
            {
                do
                {
                    if (!_source.MoveNext())
                    {
                        if (throwIfEOF) ThrowEoF(this, ref state);
                        return 0;
                    }
                    state.Init(_source.Current);
                } while (state.Span.IsEmpty);
                return state.Span.Length;
            }

            private protected override int ImplTryReadUInt64VarintWithoutMoving(ref State state, out ulong value)
            {
                return state.RemainingInCurrent >= 10
                    ? State.TryParseUInt64Varint(state.Span, state.OffsetInCurrent, out value)
                    : ViaStackAlloc(ref state, out value);

                int ViaStackAlloc(ref State s, out ulong val)
                {
                    Span<byte> span = stackalloc byte[10];
                    Span<byte> target = span;

                    int available = 0;
                    if (s.RemainingInCurrent != 0)
                    {
                        int take = Math.Min(s.RemainingInCurrent, target.Length);
                        Peek(ref s, take).CopyTo(target);
                        target = target.Slice(available);
                        available += take;
                    }

                    var iterCopy = _source;
                    while (!target.IsEmpty && iterCopy.MoveNext())
                    {
                        var nextBuffer = iterCopy.Current.Span;
                        var take = Math.Min(nextBuffer.Length, target.Length);

                        nextBuffer.Slice(0, take).CopyTo(target);
                        target = target.Slice(take);
                        available += take;
                    }

                    if (available != 10) span = span.Slice(0, available);
                    return ProtoReader.State.TryParseUInt64Varint(span, 0, out val);
                }
            }

            private protected override uint ImplReadUInt32Fixed(ref State state)
            {
                return state.RemainingInCurrent >= 4
                    ? BinaryPrimitives.ReadUInt32LittleEndian(Consume(ref state, 4))
                    : ViaStackAlloc(ref state);

                uint ViaStackAlloc(ref State st)
                {
                    Span<byte> span = stackalloc byte[4];
                    // manually inline ImplReadBytes because of compiler restriction
                    var target = span;
                    while (!target.IsEmpty)
                    {
                        var take = Math.Min(GetSomeData(ref st), target.Length);
                        Consume(ref st, take).CopyTo(target);
                        target = target.Slice(take);
                    }
                    return BinaryPrimitives.ReadUInt32LittleEndian(span);
                }
            }

            private protected override ulong ImplReadUInt64Fixed(ref State state)
            {
                return state.RemainingInCurrent >= 8
                    ? BinaryPrimitives.ReadUInt64LittleEndian(Consume(ref state, 8))
                    : ViaStackAlloc(ref state);

                ulong ViaStackAlloc(ref State st)
                {
                    Span<byte> span = stackalloc byte[8];
                    // manually inline ImplReadBytes because of compiler restriction
                    var target = span;
                    while (!target.IsEmpty)
                    {
                        var take = Math.Min(GetSomeData(ref st), target.Length);
                        Consume(ref st, take).CopyTo(target);
                        target = target.Slice(take);
                    }
                    return BinaryPrimitives.ReadUInt64LittleEndian(span);
                }
            }

            private protected override string ImplReadString(ref State state, int bytes)
            {
                return state.RemainingInCurrent >= bytes
                    ? ToString(Consume(ref state, bytes, out var offset), offset, bytes)
                    : ImplReadStringMultiSegment(ref state, bytes);
            }

            private string ImplReadStringMultiSegment(ref State state, int bytes)
            {
                // we should probably do the work with a Decoder,
                // but this works for today
                using (var mem = MemoryPool<byte>.Shared.Rent(bytes))
                {
                    var span = mem.Memory.Span;
                    ImplReadBytes(ref state, span, bytes);
                    return ToString(span, 0, bytes);
                }
            }

            private void ImplReadBytes(ref State state, Span<byte> target, int bytesToRead)
            {
                if (state.RemainingInCurrent >= bytesToRead) Consume(ref state, bytesToRead).CopyTo(target);
                else Looped(ref state, target);

                void Looped(ref State st, Span<byte> ttarget)
                {
                    var bytesRead = 0;
                    while (bytesRead < bytesToRead)
                    {
                        var take = Math.Min(GetSomeData(ref st), bytesToRead - bytesRead);
                        Consume(ref st, take).CopyTo(ttarget);
                        ttarget = ttarget.Slice(take);
                        bytesRead += take;
                    }
                }
            }

            private protected override void ImplReadBytes(ref State state, ArraySegment<byte> target)
                => ImplReadBytes(ref state, new Span<byte>(target.Array, target.Offset, target.Count), target.Count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Consume(ref State state, int bytes)
            {
                Advance(bytes);
                return state.Consume(bytes);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Consume(ref State state, int bytes, out int offset)
            {
                Advance(bytes);
                return state.Consume(bytes, out offset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Peek(ref State state, int bytes)
                => state.Span.Slice(state.OffsetInCurrent, bytes);

            private protected override int ImplTryReadUInt32VarintWithoutMoving(ref State state, Read32VarintMode mode, out uint value)
            {
                return state.RemainingInCurrent >= 10
                    ? TryParseUInt32Varint(this, ref state, state.OffsetInCurrent,
                        mode == Read32VarintMode.Signed, out value, state.Span)
                    : ViaStackAlloc(ref state, mode, out value);

                unsafe int ViaStackAlloc(ref State s, Read32VarintMode m, out uint val)
                {
                    byte* stack = stackalloc byte[10]; // because otherwise compiler is convinced we're screwing up
                    Span<byte> span = new Span<byte>(stack, 10);
                    Span<byte> target = span;
                    var currentBuffer = Peek(ref s, Math.Min(target.Length, s.RemainingInCurrent));
                    currentBuffer.CopyTo(target);
                    int available = currentBuffer.Length;
                    target = target.Slice(available);

                    var iterCopy = _source;
                    while (!target.IsEmpty && iterCopy.MoveNext())
                    {
                        var nextBuffer = iterCopy.Current.Span;
                        var take = Math.Min(nextBuffer.Length, target.Length);

                        nextBuffer.Slice(0, take).CopyTo(target);
                        target = target.Slice(take);
                        available += take;
                    }
                    if (available != 10) span = span.Slice(0, available);
                    return TryParseUInt32Varint(this, ref s, 0, m == Read32VarintMode.Signed, out val, span);
                }
            }

            private protected override void ImplSkipBytes(ref State state, long count)
            {
                if (state.RemainingInCurrent >= count) Skip(ref state, (int)count);
                else Looped(ref state, count);

                void Looped(ref State st, long ccount)
                {
                    while (ccount != 0)
                    {
                        var take = (int)Math.Min(GetSomeData(ref st), ccount);
                        Skip(ref st, take);
                        ccount -= take;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Skip(ref State state, int bytes)
            {
                state.Skip(bytes);
                Advance(bytes);
            }

            private protected override bool IsFullyConsumed(ref State state)
                => GetSomeData(ref state, false) == 0;
        }
    }
}