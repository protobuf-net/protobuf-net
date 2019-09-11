using ProtoBuf.Meta;
using System.Buffers;
using System.IO;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
        private sealed class NullProtoWriter : ProtoWriter
        {
            protected internal override State DefaultState() => default;

            public NullProtoWriter(TypeModel model, SerializationContext context) : base(model, context) { }

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

            private protected override SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style)
                => new SubItemToken(_position64);

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
                        ThrowException(this);
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
                int count = 1;
                while ((value >>= 7) != 0)
                {
                    count++;
                }
                return count;
            }

            private protected override int ImplWriteVarint64(ref State state, ulong value)
            {
                int count = 1;
                while ((value >>= 7) != 0)
                {
                    count++;
                }
                return count;
            }

            private protected override bool TryFlush(ref State state) => true;
        }
    }
}