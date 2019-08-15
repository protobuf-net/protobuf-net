#if PLAT_SPANS
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    static partial class Serializer
    {
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied writer.
        /// </summary>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="destination">The destination stream to write to.</param>
        public static void Serialize<T>(IBufferWriter<byte> destination, T instance, SerializationContext context = null)
        {
#pragma warning disable RCS1165 // Unconstrained type parameter checked for null.
            if (instance != null)
#pragma warning restore RCS1165 // Unconstrained type parameter checked for null.
            {
                var model = RuntimeTypeModel.Default;
                using (var writer = ProtoWriter.Create(out var state, destination, model, context))
                {
                    model.Serialize(writer, ref state, instance);
                    writer.Close(ref state);
                }
            }
        }
    }

    public partial class ProtoWriter
    {
        //        /// <summary>
        //        /// Create a new ProtoWriter that tagets a buffer writer
        //        /// </summary>
        //        public static ProtoWriter CreateForBufferWriter<T>(out State state, T writer, TypeModel model, SerializationContext context = null)
        //            where T : IBufferWriter<byte>
        //        {
        //#pragma warning disable RCS1165 // Unconstrained type parameter checked for null.
        //            if (writer == null) throw new ArgumentNullException(nameof(writer));
        //#pragma warning restore RCS1165 // Unconstrained type parameter checked for null.
        //            state = default;
        //            return new BufferWriterProtoWriter<T>(writer, model, context);
        //        }

        /// <summary>
        /// Create a new ProtoWriter that tagets a buffer writer
        /// </summary>
        public static ProtoWriter Create(out State state, IBufferWriter<byte> writer, TypeModel model, SerializationContext context = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            state = default;

            return BufferWriterProtoWriter<IBufferWriter<byte>>.CreateBufferWriter(writer, model, context);
        }

        private sealed class BufferWriterProtoWriter<T> : ProtoWriter
            where T : IBufferWriter<byte>
        {
            internal static BufferWriterProtoWriter<T> CreateBufferWriter(T writer, TypeModel model, SerializationContext context)
            {
                var obj = Pool<BufferWriterProtoWriter<T>>.TryGet() ?? new BufferWriterProtoWriter<T>();
                obj.Init(model, context);
                obj._writer = writer;
                return obj;
            }

            private protected override void Dispose()
            {
                base.Dispose();
                Pool<BufferWriterProtoWriter<T>>.Put(this);
            }

            protected internal override State DefaultState() => throw new InvalidOperationException("You must retain and pass the state from ProtoWriter.CreateForBufferWriter");

#pragma warning disable IDE0044 // Add readonly modifier
            private T _writer; // not readonly, because T could be a struct - might need in-place state changes
#pragma warning restore IDE0044 // Add readonly modifier
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

            protected internal override void WriteSubItem<TBase, TActual>(ref State state, TActual value, IProtoSerializer<TBase, TActual> serializer,
                PrefixStyle style, bool recursionCheck)
            {
                switch (WireType)
                {
                    case WireType.String:
                    case WireType.Fixed32:
                        PreSubItem(TypeHelper<T>.IsObjectType & recursionCheck ? (object)value : null);
                        WriteWithLengthPrefix<TBase, TActual>(ref state, value, serializer, style);
                        PostSubItem();
                        return;
                    case WireType.StartGroup:
                    default:
                        base.WriteSubItem<TBase, TActual>(ref state, value, serializer, style, recursionCheck);
                        return;
                }
            }

            private void WriteWithLengthPrefix<TBase, TActual>(ref State state, TActual value, IProtoSerializer<TBase, TActual> serializer, PrefixStyle style)
                where TActual : TBase
            {
                long calculatedLength;
                using (var nul = NullProtoWriter.CreateNull(Model, Context, out var nulState))
                {
                    serializer.Serialize(nul, ref nulState, value);
                    nul.Close(ref nulState);
                    calculatedLength = nul._position64;
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
                        throw new NotImplementedException($"Sub-object prefix style not implemented: {style}");
                }
                var oldPos = _position64;
                serializer.Serialize(this, ref state, value);
                var actualLength = (_position64 - oldPos);
                if (actualLength != calculatedLength)
                    throw new InvalidOperationException($"Length mismatch; calculated '{calculatedLength}', actual '{actualLength}'");
            }

            private protected override void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style)
                => throw new NotSupportedException("You must use the WriteSubItem API with this writer type");

            private protected override SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style)
                => throw new NotSupportedException("You must use the WriteSubItem API with this writer type");

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