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

            protected internal override void WriteMessage<T>(ref State state, T value, ISerializer<T> serializer, PrefixStyle style, bool recursionCheck)
            {
                var len = Measure<T>(this, value, serializer ?? TypeModel.GetSerializer<T>(Model));
                AdvanceSubMessage(ref state, len, style);
            }

            internal override void WriteWrappedItem<T>(ref State state, SerializerFeatures features, T value, ISerializer<T> serializer)
            {
                var len = MeasureAny<T>(this, TypeModel.ListItemTag, features, value, serializer ?? TypeModel.GetSerializer<T>(Model));
                AdvanceSubMessage(ref state, len, PrefixStyle.Base128); // only supported styles are group+varint
            }
            internal override void WriteWrappedCollection<TCollection, TItem>(ref State state, SerializerFeatures features, TCollection values, RepeatedSerializer<TCollection, TItem> serializer, ISerializer<TItem> valueSerializer)
            {
                var len = MeasureRepeated<TCollection, TItem>(this, TypeModel.ListItemTag, features, values, serializer, valueSerializer ?? TypeModel.GetSerializer<TItem>(Model));
                AdvanceSubMessage(ref state, len, PrefixStyle.Base128); // only supported styles are group+varint
            }

            internal override void WriteWrappedMap<TCollection, TKey, TValue>(ref State state, SerializerFeatures features, TCollection values, MapSerializer<TCollection, TKey, TValue> serializer, SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
            {
                var len = MeasureMap<TCollection, TKey, TValue>(this, TypeModel.ListItemTag, features, values, serializer, keyFeatures, valueFeatures, keySerializer, valueSerializer);
                AdvanceSubMessage(ref state, len, PrefixStyle.Base128); // only supported styles are group+varint
            }

            private void AdvanceSubMessage(ref State state, long length, PrefixStyle style)
            {
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
                                preamble = ImplWriteVarint64(ref state, (ulong)length);
                                break;
                            default:
                                state.ThrowInvalidSerializationOperation();
                                preamble = default;
                                break;
                        }
                        break;
                    case WireType.StartGroup:
                        // the start group is already written, so w just need to leave the end group
                        preamble = ImplWriteVarint32(ref state, (uint)(fieldNumber << 3));
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
                if (serializer is null) serializer = TypeModel.GetSubTypeSerializer<T>(Model);
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
                int bytes;
                switch(style)
                {
                    case PrefixStyle.Fixed32BigEndian:
                    case PrefixStyle.Fixed32:
                        bytes = 4;
                        break;
                    case PrefixStyle.Base128:
                        bytes = ImplWriteVarint64(ref state, (ulong)len);
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

            private protected override void ImplWriteBytes(ref State state, ReadOnlySpan<byte> data) { }

            private protected override void ImplWriteBytes(ref State state, ReadOnlySequence<byte> data) { }

            private protected override void ImplWriteFixed32(ref State state, uint value) { }

            private protected override void ImplWriteFixed64(ref State state, ulong value) { }

            private protected override void ImplWriteString(ref State state, string value, int expectedBytes) { }

            [MethodImpl(ProtoReader.HotPath)]
            private protected override int ImplWriteVarint32(ref State state, uint value) => MeasureUInt32(value);

            [MethodImpl(ProtoReader.HotPath)]
            internal override int ImplWriteVarint64(ref State state, ulong value) => MeasureUInt64(value);

            private protected override bool TryFlush(ref State state) => true;
        }

        [MethodImpl(ProtoReader.HotPath)]
        internal static int MeasureUInt32(uint value)
        {
#if PLAT_INTRINSICS
            return ((31 - System.Numerics.BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;
#else
            int count = 1;
            while ((value >>= 7) != 0)
            {
                count++;
            }
            return count;
#endif
        }

        [MethodImpl(ProtoReader.HotPath)]
        internal static int MeasureUInt64(ulong value)
        {
#if PLAT_INTRINSICS
            return ((63 - System.Numerics.BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;
#else
            int count = 1;
            while ((value >>= 7) != 0)
            {
                count++;
            }
            return count;
#endif
        }
    }
}