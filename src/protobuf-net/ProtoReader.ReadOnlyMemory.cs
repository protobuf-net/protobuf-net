#if PLAT_SPANS
using ProtoBuf.Meta;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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
        public static ProtoReader Create(out State state, ReadOnlyMemory<byte> source, TypeModel model, SerializationContext context = null)
        {
            var reader = ReadOnlyMemoryProtoReader.GetRecycled()
                ?? new ReadOnlyMemoryProtoReader();
            reader.Init(out state, source, model, context);
            return reader;
        }
        private class ReadOnlyMemoryProtoReader : ProtoReader
        {
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
            internal static int TryParseUInt32Varint(ProtoReader @this, int offset, bool trimNegative, out uint value, ReadOnlySpan<byte> span)
            {
                if ((uint)offset >= (uint)span.Length)
                {
                    value = 0;
                    return 0;
                }

                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                uint chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
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

                ThrowOverflow(@this);
                return 0;
            }
            internal static int TryParseUInt64Varint(ProtoReader @this, int offset, out ulong value, ReadOnlySpan<byte> span)
            {
                if ((uint)offset >= (uint)span.Length)
                {
                    value = 0;
                    return 0;
                }
                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                ulong chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) ThrowOverflow(@this);
                return 10;
            }

            [ThreadStatic]
            private static ReadOnlyMemoryProtoReader s_lastReader;

            internal static ReadOnlyMemoryProtoReader GetRecycled()
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

            internal void Init(out State state, ReadOnlyMemory<byte> source, TypeModel model, SerializationContext context)
            {
                base.Init(model, context);
                state = default;
                state.Init(source);
            }

            private protected override int ImplTryReadUInt64VarintWithoutMoving(ref State state, out ulong value)
                => TryParseUInt64Varint(this, state.OffsetInCurrent, out value, state.Span);

            private protected override uint ImplReadUInt32Fixed(ref State state)
                => BinaryPrimitives.ReadUInt32LittleEndian(Consume(ref state, 4));

            private protected override ulong ImplReadUInt64Fixed(ref State state)
                => BinaryPrimitives.ReadUInt64LittleEndian(Consume(ref state, 8));

            private protected override string ImplReadString(ref State state, int bytes)
            {
                if (state.RemainingInCurrent < bytes) ThrowEoF(this);
                return ToString(Consume(ref state, bytes, out var offset), offset, bytes);
            }

            private protected override void ImplReadBytes(ref State state, ArraySegment<byte> target)
                => ImplReadBytes(ref state, new Span<byte>(target.Array, target.Offset, target.Count));

            private void ImplReadBytes(ref State state, Span<byte> span)
                => Consume(ref state, span.Length).CopyTo(span);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Consume(ref State state, int bytes)
            {
                var span = state.Span.Slice(state.OffsetInCurrent, bytes);
                state.Consume(bytes);
                Advance(bytes);
                return span;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Consume(ref State state, int bytes, out int offset)
            {
                offset = state.OffsetInCurrent;
                state.Consume(bytes);
                Advance(bytes);
                return state.Span;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Peek(ref State state, int bytes)
                => state.Span.Slice(state.OffsetInCurrent, bytes);

            private protected override int ImplTryReadUInt32VarintWithoutMoving(ref State state, Read32VarintMode mode, out uint value)
                => TryParseUInt32Varint(this, state.OffsetInCurrent, mode == Read32VarintMode.Signed, out value, state.Span);

            private protected override void ImplSkipBytes(ref State state, long count)
            {
                if (count > state.RemainingInCurrent) ThrowEoF(this);
                Skip(ref state, (int)count);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Skip(ref State state, int bytes)
            {
                state.Consume(bytes);
                Advance(bytes);
            }

            private protected override bool IsFullyConsumed(ref State state)
                => state.RemainingInCurrent == 0;
        }
    }
}
#endif