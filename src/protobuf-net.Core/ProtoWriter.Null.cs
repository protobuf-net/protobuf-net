using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
        internal static State CreateNull(TypeModel model, object userState, long abortAfter)
            => NullProtoWriter.CreateNullProtoWriter(model, userState, abortAfter);

        internal sealed class NullProtoWriter : ProtoWriter
        {
            protected internal override State DefaultState() => new State(this);

            internal static State CreateNullProtoWriter(TypeModel model, object userState, long abortAfter)
            {
                var obj = Pool<NullProtoWriter>.TryGet() ?? new NullProtoWriter();
                obj.Init(model, userState, true);
                obj._abortAfter = abortAfter < 0 ? long.MaxValue : abortAfter;
                return new State(obj);
            }

            private long _abortAfter;

            private NullProtoWriter() { } // gets own object cache

            // this is for use as a sub-component of the buffer-writer
            internal NullProtoWriter(NetObjectCache knownObjects)
                : base(knownObjects)
            {
                _abortAfter = long.MaxValue;
            }

            internal override void Dispose()
            {
                base.Dispose();
                Pool<NullProtoWriter>.Put(this);
            }

            private protected override bool ImplDemandFlushOnDispose => false;

            // this writer only ever counts; see ProtoWriter.IsMeasuring
            private protected override bool IsMeasuringPass => true;

            private protected override void ImplCopyRawFromStream(ref State state, Stream source)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
                try
                {
                    while (true)
                    {
                        int bytes = source.Read(buffer, 0, buffer.Length);
                        if (bytes <= 0) break;
                        Advance(bytes);
                        CheckOversized(ref state);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void CheckOversized(long max, long actual)
            {
                if (max >= 0 & actual > max) ThrowHelper.ThrowProtoException($"Length {actual} exceeds constrained size of {max} bytes");
            }

            [MethodImpl(ProtoReader.HotPath)]
            private void CheckOversized(ref State state)
            {
                var position = state.GetPosition();
                if (position > _abortAfter) CheckOversized(_abortAfter, position);
            }

            /// <summary>
            /// Measuring a sub-message recurses exactly as writing one does, so it needs the same
            /// depth and recursion guard.
            /// </summary>
            /// <remarks>
            /// Without this, a cyclic graph overflows the STACK here instead of throwing
            /// "Possible recursion detected": the measure walk re-enters through Measure ->
            /// serializer.Write -> WriteMessage and never touches PreSubItem, which is where both
            /// guards live. The classic reserve-and-back-fill path was immune only because it goes
            /// through StartSubItem, which does call PreSubItem - so the exposure has always been
            /// specific to the measure-first backends.
            /// </remarks>
            protected internal override void WriteMessage<T>(ref State state, T value, ISerializer<T> serializer, PrefixStyle style, bool recursionCheck)
            {
                PreSubItem(ref state, TypeHelper<T>.IsReferenceType & recursionCheck ? (object)value : null);
                var len = Measure<T>(this, value, serializer ?? TypeModel.ResolveSerializer<T>(Model));
                AdvanceSubMessage(ref state, len, style); // leaves WireType = None, which PostSubItem demands
                PostSubItem(ref state);
            }

            internal override void WriteWrappedItem<T>(ref State state, SerializerFeatures features, T value, ISerializer<T> serializer)
            {
                var len = MeasureAny<T>(this, TypeModel.ListItemTag, features, value, serializer ?? TypeModel.ResolveSerializer<T>(Model));
                AdvanceSubMessage(ref state, len, PrefixStyle.Base128); // only supported styles are group+varint
            }
            internal override void WriteWrappedCollection<TCollection, TItem>(ref State state, SerializerFeatures features, TCollection values, RepeatedSerializer<TCollection, TItem> serializer, ISerializer<TItem> valueSerializer)
            {
                var len = MeasureRepeated<TCollection, TItem>(this, TypeModel.ListItemTag, features, values, serializer, valueSerializer ?? TypeModel.ResolveSerializer<TItem>(Model));
                AdvanceSubMessage(ref state, len, PrefixStyle.Base128); // only supported styles are group+varint
            }

            internal override void WriteWrappedMap<TCollection, TKey, TValue>(ref State state, SerializerFeatures features, TCollection values, MapSerializer<TCollection, TKey, TValue> serializer, SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
            {
                var len = MeasureMap<TCollection, TKey, TValue>(this, TypeModel.ListItemTag, features, values, serializer, keyFeatures, valueFeatures, keySerializer, valueSerializer);
                AdvanceSubMessage(ref state, len, PrefixStyle.Base128); // only supported styles are group+varint
            }

            private void AdvanceSubMessage(ref State state, long length, PrefixStyle style)
            {
                // note: the ImplWrite* arms below account for their own preamble (see the
                // stores above), so only the arms that write nothing contribute here
                long preamble;
                switch (WireType)
                {
                    case WireType.String:
                    case WireType.Fixed32:
                        switch (style)
                        {
                            case PrefixStyle.None:
                                preamble = 0;
                                break;
                            case PrefixStyle.Fixed32:
                            case PrefixStyle.Fixed32BigEndian:
                                preamble = 4;
                                break;
                            case PrefixStyle.Base128:
                                ImplWriteVarint64(ref state, (ulong)length);
                                preamble = 0;
                                break;
                            default:
                                state.ThrowInvalidSerializationOperation();
                                preamble = default;
                                break;
                        }
                        break;
                    case WireType.StartGroup:
                        // the start group is already written, so w just need to leave the end group
                        ImplWriteVarint32(ref state, (uint)(fieldNumber << 3));
                        preamble = 0;
                        break;
                    default:
                        state.ThrowInvalidSerializationOperation();
                        preamble = default;
                        break;
                }
                Advance(preamble + length);
                CheckOversized(ref state);
                WireType = WireType.None;
            }
            protected internal override void WriteSubType<T>(ref State state, T value, ISubTypeSerializer<T> serializer)
            {
                serializer ??= TypeModel.GetSubTypeSerializer<T>(Model);
                var len = Measure<T>(this, value, serializer);
                AdvanceSubMessage(ref state, len, PrefixStyle.Base128);
            }

            private protected override SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style)
            {
                WireType = WireType.None;
                return new SubItemToken(_position64);
            }

            private protected override void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style)
            {
                var len = _position64 - token.value64;
                int bytes; // as above: only the arms that write nothing contribute here
                switch(style)
                {
                    case PrefixStyle.Fixed32BigEndian:
                    case PrefixStyle.Fixed32:
                        bytes = 4;
                        break;
                    case PrefixStyle.Base128:
                        ImplWriteVarint64(ref state, (ulong)len);
                        bytes = 0;
                        break;
                    default:
                        state.ThrowInvalidSerializationOperation();
                        goto case PrefixStyle.None;
                    case PrefixStyle.None:
                        bytes = 0;
                        break;
                }
                Advance(bytes);
                CheckOversized(ref state);
            }

            // this writer has no pending buffer, so every store commits immediately: the
            // stores ARE the measurement, and each accounts for itself rather than relying
            // on the caller to advance (the deferred-position invariant, docs/nano-writer.md)

            private protected override void ImplWriteBytes(ref State state, ReadOnlySpan<byte> data)
                => Advance(data.Length);

            private protected override void ImplWriteBytes(ref State state, ReadOnlySequence<byte> data)
                => Advance(data.Length);

            private protected override void ImplWriteFixed32(ref State state, uint value)
                => Advance(4);

            private protected override void ImplWriteFixed64(ref State state, ulong value)
                => Advance(8);

            private protected override void ImplWriteString(ref State state, string value, int expectedBytes)
                => Advance(expectedBytes);

            [MethodImpl(ProtoReader.HotPath)]
            private protected override int ImplWriteVarint32(ref State state, uint value)
            {
                var count = MeasureUInt32(value);
                Advance(count);
                return count;
            }

            [MethodImpl(ProtoReader.HotPath)]
            internal override int ImplWriteVarint64(ref State state, ulong value)
            {
                var count = MeasureUInt64(value);
                Advance(count);
                return count;
            }

            private protected override bool TryFlush(ref State state) => true;
        }

        [MethodImpl(ProtoReader.HotPath)]
        internal static int MeasureInt32(int value)
            => value < 0 ? 10 : MeasureUInt32((uint)value);

        // Raced against six alternatives across four value distributions
        // (src/NanoBench/VarintMeasureResults.md). A table indexed by the leading-zero count
        // wins at 0.45/0.46 of the previous form for 32/64-bit, and - the reason it is the
        // right CHOICE rather than merely the fastest - it is distribution-independent, so it
        // needs no bet on the data. A comparison ladder beats it on small-only input and loses
        // elsewhere; a switch/jump-table loses outright, with thirty times the variance.
        //
        // Down-level has no LeadingZeroCount, so it takes the ladder - which still beats the
        // shift loop it replaces in every distribution, and that loop was worse than the
        // INTRINSIC baseline on wide values.
        //
        // The tables are derived, not typed: bytes = ceil(bits / 7) where bits = width - lzcnt.
        // A hand-written one was wrong at its second entry when this was first attempted.
#if PLAT_INTRINSICS
        private static ReadOnlySpan<byte> VarintLength32 =>
        [
             5,  5,  5,  5,  4,  4,  4,  4,  4,  4,  4,  3,  3,  3,  3,  3,
             3,  3,  2,  2,  2,  2,  2,  2,  2,  1,  1,  1,  1,  1,  1,  1,
             1
        ];

        private static ReadOnlySpan<byte> VarintLength64 =>
        [
            10,  9,  9,  9,  9,  9,  9,  9,  8,  8,  8,  8,  8,  8,  8,  7,
             7,  7,  7,  7,  7,  7,  6,  6,  6,  6,  6,  6,  6,  5,  5,  5,
             5,  5,  5,  5,  4,  4,  4,  4,  4,  4,  4,  3,  3,  3,  3,  3,
             3,  3,  2,  2,  2,  2,  2,  2,  2,  1,  1,  1,  1,  1,  1,  1,
             1
        ];
#endif

        [MethodImpl(ProtoReader.HotPath)]
        internal static int MeasureUInt32(uint value)
        {
#if PLAT_INTRINSICS
            return VarintLength32[System.Numerics.BitOperations.LeadingZeroCount(value | 1)];
#else
            return value < 1u << 7 ? 1
                : value < 1u << 14 ? 2
                : value < 1u << 21 ? 3
                : value < 1u << 28 ? 4 : 5;
#endif
        }

        [MethodImpl(ProtoReader.HotPath)]
        internal static int MeasureInt64(long value)
            => value < 0 ? 10 : MeasureUInt64((ulong)value);

        [MethodImpl(ProtoReader.HotPath)]
        internal static int MeasureUInt64(ulong value)
        {
#if PLAT_INTRINSICS
            return VarintLength64[System.Numerics.BitOperations.LeadingZeroCount(value | 1)];
#else
            return value < 1ul << 7 ? 1
                : value < 1ul << 14 ? 2
                : value < 1ul << 21 ? 3
                : value < 1ul << 28 ? 4
                : value < 1ul << 35 ? 5
                : value < 1ul << 42 ? 6
                : value < 1ul << 49 ? 7
                : value < 1ul << 56 ? 8
                : value < 1ul << 63 ? 9 : 10;
#endif
        }
    }
}