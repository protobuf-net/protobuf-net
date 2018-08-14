#if PLAT_SPANS
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
        /// <summary>
        /// Create a new ProtoWriter that tagets a buffer writer
        /// </summary>
        public static ProtoWriter Create(out ProtoWriter.State state, IBufferWriter<byte> writer, TypeModel model, SerializationContext context = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            state = default;
            return new BufferWriterProtoWriter(writer, model, context);
        }

        private sealed class BufferWriterProtoWriter : ProtoWriter
        {
            private readonly IBufferWriter<byte> _writer;
            internal BufferWriterProtoWriter(IBufferWriter<byte> writer, TypeModel model, SerializationContext context)
                : base(model, context)
                => _writer = writer;

            private protected override bool ImplDemandFlushOnDispose => true;

            private protected override bool TryFlush(ref State state)
            {
                if (state.IsActive)
                {
                    _writer.Advance(state.Flush());
                }
                return true;
            }

            private protected override void ImplWriteFixed32(ref State state, uint value)
            {
                if (state.RemainingInCurrent < 4) GetBuffer(ref state);
                state.WriteFixed32(value);
            }

            private protected override void ImplWriteFixed64(ref State state, ulong value)
            {
                if (state.RemainingInCurrent < 8) GetBuffer(ref state);
                state.WriteFixed64(value);
            }

            private protected override void ImplWriteString(ref State state, string value, int expectedBytes)
            {
                if (expectedBytes <= state.RemainingInCurrent) state.WriteString(value);
                else FallbackWriteString(ref state, value, expectedBytes);
            }

            private void FallbackWriteString(ref State state, string value, int expectedBytes)
            {
                GetBuffer(ref state);
                if (expectedBytes <= state.RemainingInCurrent)
                {
                    state.WriteString(value);
                }
                else
                {
                    throw new NotImplementedException(); // need encoder
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void GetBuffer(ref State state)
            {
                TryFlush(ref state);
                state.Init(_writer.GetMemory(128));
            }

            private protected override void ImplWriteBytes(ref State state, byte[] data, int offset, int length)
            {
                var span = new ReadOnlySpan<byte>(data, offset, length);
                if (length <= state.RemainingInCurrent) state.WriteBytes(span);
                else FallbackWriteBytes(ref state, span);
            }

            private void FallbackWriteBytes(ref State state, ReadOnlySpan<byte> span)
            {
                while (true)
                {
                    GetBuffer(ref state);
                    if (span.Length <= state.RemainingInCurrent)
                    {
                        state.WriteBytes(span);
                        return;
                    }
                    else
                    {
                        state.WriteBytes(span.Slice(0, state.RemainingInCurrent));
                        span = span.Slice(state.RemainingInCurrent);
                    }
                }
            }

            private protected override int ImplWriteVarint32(ref State state, uint value)
            {
                if (state.RemainingInCurrent < 5) GetBuffer(ref state);
                return state.WriteVarint32(value);
            }

            private protected override int ImplWriteVarint64(ref State state, ulong value)
            {
                if (state.RemainingInCurrent < 10) GetBuffer(ref state);
                return state.WriteVarint64(value);
            }

            private protected override void ImplEndSubItem(ref State state, SubItemToken token, PrefixStyle style)
            {
                throw new NotImplementedException();
            }

            private protected override SubItemToken ImplStartSubItem(ref State state, object instance, bool allowFixed)
            {
                throw new NotImplementedException();
            }

            private protected override void ImplCopyRawFromStream(ref State state, Stream source)
            {
                while (true)
                {
                    if (state.RemainingInCurrent == 0) GetBuffer(ref state);

                    int bytes = state.ReadFrom(source);
                    if (bytes <= 0) break;
                    Advance(bytes);
                }
            }
        }
    }
}
#endif