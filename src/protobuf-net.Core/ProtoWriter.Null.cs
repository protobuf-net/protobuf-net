using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System.Buffers;
using System.IO;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
        internal static State CreateNull(TypeModel model, SerializationContext context = null)
            => NullProtoWriter.CreateNullProtoWriter(model, context);

        internal sealed class NullProtoWriter : ProtoWriter
        {
            protected internal override State DefaultState() => new State(this);

            internal static State CreateNullProtoWriter(TypeModel model, SerializationContext context)
            {
                var obj = Pool<NullProtoWriter>.TryGet() ?? new NullProtoWriter();
                obj.Init(model, context, true);
                return new State(obj);
            }

            private NullProtoWriter() { } // gets own object cache

            // this is for use as a sub-component of the buffer-writer
            internal NullProtoWriter(NetObjectCache knownObjects) : base(knownObjects) { }

            private protected override void Dispose()
            {
                base.Dispose();
                Pool<NullProtoWriter>.Put(this);
            }

            private protected override bool ImplDemandFlushOnDispose => false;

            private protected override void ImplCopyRawFromStream(ref State state, Stream source)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
                while (true)
                {
                    int bytes = source.Read(buffer, 0, buffer.Length);
                    if (bytes <= 0) break;
                    Advance(bytes);
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }

            protected internal override void WriteMessage<T>(ref State state, T value, IMessageSerializer<T> serializer, PrefixStyle style, bool recursionCheck)
            {
                if (serializer == null) serializer = TypeModel.GetMessageSerializer<T>(Model);
                var len = Measure<T>(this, value, serializer);
                AdvanceSubMessage(ref state, len, style);
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
                WireType = WireType.None;
            }
            protected internal override void WriteSubType<T>(ref State state, T value, ISubTypeSerializer<T> serializer)
            {
                if (serializer == null) serializer = TypeModel.GetSubTypeSerializer<T>(Model);
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
            }

            private protected override void ImplWriteBytes(ref State state, byte[] data, int offset, int length) { }

            private protected override void ImplWriteBytes(ref State state, ReadOnlySequence<byte> data) { }

            private protected override void ImplWriteFixed32(ref State state, uint value) { }

            private protected override void ImplWriteFixed64(ref State state, ulong value) { }

            private protected override void ImplWriteString(ref State state, string value, int expectedBytes) { }

            private protected override int ImplWriteVarint32(ref State state, uint value)
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

            private protected override int ImplWriteVarint64(ref State state, ulong value)
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

            private protected override bool TryFlush(ref State state) => true;
        }
    }
}