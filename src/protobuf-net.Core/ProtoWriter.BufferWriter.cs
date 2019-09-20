using ProtoBuf.Internal;
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
        public static ProtoWriter Create(out State state, IBufferWriter<byte> writer, TypeModel model, SerializationContext context = null)
        {
            if (writer == null) ThrowHelper.ThrowArgumentNullException(nameof(writer));

            var protoWriter = BufferWriterProtoWriter.CreateBufferWriter(writer, model, context);
            state = new State(protoWriter);
            return protoWriter;
        }

        private sealed class BufferWriterProtoWriter : ProtoWriter
        {
            internal static BufferWriterProtoWriter CreateBufferWriter(IBufferWriter<byte> writer, TypeModel model, SerializationContext context)
            {
                var obj = Pool<BufferWriterProtoWriter>.TryGet() ?? new BufferWriterProtoWriter();
                obj.Init(model, context);
                obj._writer = writer;
                return obj;
            }

            private protected override void Dispose()
            {
                base.Dispose();
                Pool<BufferWriterProtoWriter>.Put(this);
            }

            private protected override void Cleanup()
            {
                base.Cleanup();
                _writer = default;
            }

            protected internal override State DefaultState()
            {
                ThrowHelper.ThrowInvalidOperationException("You must retain and pass the state from ProtoWriter.CreateForBufferWriter");
                return default;
            }

            private IBufferWriter<byte> _writer;

            private BufferWriterProtoWriter() { }

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
                state.LocalWriteFixed32(value);
            }

            private protected override void ImplWriteFixed64(ref State state, ulong value)
            {
                if (state.RemainingInCurrent < 8) GetBuffer(ref state);
                state.LocalWriteFixed64(value);
            }

            private protected override void ImplWriteString(ref State state, string value, int expectedBytes)
            {
                if (expectedBytes <= state.RemainingInCurrent) state.LocalWriteString(value);
                else FallbackWriteString(ref state, value, expectedBytes);
            }

            private void FallbackWriteString(ref State state, string value, int expectedBytes)
            {
                GetBuffer(ref state);
                if (expectedBytes <= state.RemainingInCurrent)
                {
                    state.LocalWriteString(value);
                }
                else
                {
                    // could use encoder, but... this is pragmatic
                    var arr = ArrayPool<byte>.Shared.Rent(expectedBytes);
                    UTF8.GetBytes(value, 0, value.Length, arr, 0);
                    ImplWriteBytes(ref state, arr, 0, expectedBytes);
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
                if (length <= state.RemainingInCurrent) state.LocalWriteBytes(span);
                else FallbackWriteBytes(ref state, span);
            }

            private protected override void ImplWriteBytes(ref State state, ReadOnlySequence<byte> data)
            {
                if (data.IsSingleSegment)
                {
                    var span = data.First.Span;
                    if (span.Length <= state.RemainingInCurrent) state.LocalWriteBytes(span);
                    else FallbackWriteBytes(ref state, span);
                }
                else
                {
                    foreach (var segment in data)
                    {
                        var span = segment.Span;
                        if (span.Length <= state.RemainingInCurrent) state.LocalWriteBytes(span);
                        else FallbackWriteBytes(ref state, span);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void FallbackWriteBytes(ref State state, ReadOnlySpan<byte> span)
            {
                while (true)
                {
                    GetBuffer(ref state);
                    if (span.Length <= state.RemainingInCurrent)
                    {
                        state.LocalWriteBytes(span);
                        return;
                    }
                    else
                    {
                        state.LocalWriteBytes(span.Slice(0, state.RemainingInCurrent));
                        span = span.Slice(state.RemainingInCurrent);
                    }
                }
            }

            private protected override int ImplWriteVarint32(ref State state, uint value)
            {
                if (state.RemainingInCurrent < 5) GetBuffer(ref state);
                return state.LocalWriteVarint32(value);
            }

            private protected override int ImplWriteVarint64(ref State state, ulong value)
            {
                if (state.RemainingInCurrent < 10) GetBuffer(ref state);
                return state.LocalWriteVarint64(value);
            }

            protected internal override void WriteSubItem<T>(ref State state, T value, IProtoSerializer<T> serializer,
                PrefixStyle style, bool recursionCheck)
            {
                switch (WireType)
                {
                    case WireType.String:
                    case WireType.Fixed32:
                        PreSubItem(TypeHelper<T>.IsObjectType & recursionCheck ? (object)value : null);
                        WriteWithLengthPrefix<T>(ref state, value, serializer, style);
                        PostSubItem();
                        return;
                    case WireType.StartGroup:
                    default:
                        base.WriteSubItem<T>(ref state, value, serializer, style, recursionCheck);
                        return;
                }
            }

            protected internal override void WriteSubType<T>(ref State state, T value, IProtoSubTypeSerializer<T> serializer)
            {
                switch (WireType)
                {
                    case WireType.String:
                    case WireType.Fixed32:
                        WriteWithLengthPrefix<T>(ref state, value, serializer);
                        return;
                    case WireType.StartGroup:
                    default:
                        base.WriteSubType<T>(ref state, value, serializer);
                        return;
                }
            }

            private void WriteWithLengthPrefix<T>(ref State state, T value, IProtoSerializer<T> serializer, PrefixStyle style)
            {
                long calculatedLength;
                using (var nullWriter = NullProtoWriter.CreateNullImpl(Model, Context, out var nulState))
                {
                    try
                    {
                        serializer.Write(nullWriter, ref nulState, value);
                        nullWriter.Close(ref nulState);
                        calculatedLength = nullWriter.GetPosition(ref state);
                    }
                    catch
                    {
                        nullWriter.Abandon();
                        throw;
                    }
                }

                switch (style)
                {
                    case PrefixStyle.None:
                        break;
                    case PrefixStyle.Base128:
                        AdvanceAndReset(ImplWriteVarint64(ref state, (ulong)calculatedLength));
                        break;
                    case PrefixStyle.Fixed32:
                    case PrefixStyle.Fixed32BigEndian:
                        ImplWriteFixed32(ref state, checked((uint)calculatedLength));
                        if (style == PrefixStyle.Fixed32BigEndian)
                            state.ReverseLast32();
                        AdvanceAndReset(4);
                        break;
                    default:
                        ThrowHelper.ThrowNotImplementedException($"Sub-object prefix style not implemented: {style}");
                        break;
                }
                var oldPos = GetPosition(ref state);
                serializer.Write(this, ref state, value);
                var newPos = GetPosition(ref state);

                var actualLength = (newPos - oldPos);
                if (actualLength != calculatedLength)
                {
                    ThrowHelper.ThrowInvalidOperationException($"Length mismatch; calculated '{calculatedLength}', actual '{actualLength}'");
                }
            }

            private void WriteWithLengthPrefix<T>(ref State state, T value, IProtoSubTypeSerializer<T> serializer)
                where T : class
            {
                long calculatedLength;
                using (var nullWriter = NullProtoWriter.CreateNullImpl(Model, Context, out var nulState))
                {
                    try
                    {
                        serializer.WriteSubType(nullWriter, ref nulState, value);
                        nullWriter.Close(ref nulState);
                        calculatedLength = nullWriter.GetPosition(ref state);
                    }
                    catch
                    {
                        nullWriter.Abandon();
                        throw;
                    }
                }

                AdvanceAndReset(ImplWriteVarint64(ref state, (ulong)calculatedLength));
                var oldPos = GetPosition(ref state);
                serializer.WriteSubType(this, ref state, value);
                var newPos = GetPosition(ref state);

                var actualLength = (newPos - oldPos);
                if (actualLength != calculatedLength)
                {
                    ThrowHelper.ThrowInvalidOperationException($"Length mismatch; calculated '{calculatedLength}', actual '{actualLength}'");
                }
            }

            private protected override void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style)
                => ThrowHelper.ThrowNotSupportedException("You must use the WriteSubItem API with this writer type");

            private protected override SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style)
            {
                ThrowHelper.ThrowNotSupportedException("You must use the WriteSubItem API with this writer type");
                return default;
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