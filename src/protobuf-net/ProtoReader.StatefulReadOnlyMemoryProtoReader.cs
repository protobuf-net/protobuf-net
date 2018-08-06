#if PLAT_SPANS
using ProtoBuf.Meta;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            var reader = StatefulReadOnlyMemoryProtoReader.GetRecycled()
                ?? new StatefulReadOnlyMemoryProtoReader();
            reader.Init(out state, source, model, context);
            return reader;
        }
        private class StatefulReadOnlyMemoryProtoReader : ProtoReader
        {
            [ThreadStatic]
            private static StatefulReadOnlyMemoryProtoReader s_lastReader;

            internal static StatefulReadOnlyMemoryProtoReader GetRecycled()
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
                => ReadOnlySequenceProtoReader.TryParseUInt64Varint(this, state.OffsetInCurrent, out value, state.Span);

            private protected override uint ImplReadUInt32Fixed(ref State state)
                => BinaryPrimitives.ReadUInt32LittleEndian(Consume(ref state, 4));

            private protected override ulong ImplReadUInt64Fixed(ref State state)
                => BinaryPrimitives.ReadUInt64LittleEndian(Consume(ref state, 8));

            private protected override string ImplReadString(ref State state, int bytes)
            {
                if (state.RemainingInCurrent < bytes) ThrowEoF(this);
                return ReadOnlySequenceProtoReader.ToString(
                    Consume(ref state, bytes, out var offset), offset, bytes);
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
                => ReadOnlySequenceProtoReader.TryParseUInt32Varint(this, state.OffsetInCurrent, mode == Read32VarintMode.Signed, out value, state.Span);

            private protected override void ImplSkipBytes(ref State state, long count, bool preservePreviewField)
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