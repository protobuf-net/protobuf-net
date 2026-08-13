using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    public partial class ProtoWriter
    {
        partial struct State
        {
            /// <summary>
            /// Create a new ProtoWriter that targets a buffer writer
            /// </summary>
            public static State Create(IBufferWriter<byte> writer, TypeModel model, object userState = null)
                => BufferWriterProtoWriter.CreateBufferWriterProtoWriter(writer, model, userState);
        }

        private sealed class BufferWriterProtoWriter : ProtoWriter
        {
            internal static State CreateBufferWriterProtoWriter(IBufferWriter<byte> writer, TypeModel model, object userState)
            {
                if (writer is null) ThrowHelper.ThrowArgumentNullException(nameof(writer));
                var obj = Pool<BufferWriterProtoWriter>.TryGet() ?? new BufferWriterProtoWriter();
                obj.Init(model, userState, true);
                obj._writer = writer;
                return new State(obj);
            }

            internal override void Init(TypeModel model, object userState, bool impactCount)
            {
                base.Init(model, userState, impactCount);
                _nullWriter.Init(model, userState, impactCount: false);
            }

            private IBufferWriter<byte> _writer;

            private BufferWriterProtoWriter()
            {
                // share the *same* known objects key
                _nullWriter = new NullProtoWriter(netCache);
            }

            private protected override void ClearKnownObjects() { }

            private readonly NullProtoWriter _nullWriter;

            internal override void Dispose()
            {
                base.Dispose();
                Pool<BufferWriterProtoWriter>.Put(this);
                // don't cascade dispose to the null one; we're leaving that attached etc
            }

            private protected override void Cleanup()
            {
                base.Cleanup();
                _nullWriter.Cleanup();
                _writer = default;
                // the latch is per-destination, so it must not survive into the next use of a
                // pooled writer - a friendly destination would otherwise inherit the penalty
                if (_ownedLease is not null) BufferPool.ReleaseBufferToPool(ref _ownedLease);
            }

            protected internal override State DefaultState()
            {
                ThrowHelper.ThrowInvalidOperationException("You must retain and pass the state from ProtoWriter.CreateForBufferWriter");
                return default;
            }

            private protected override bool ImplDemandFlushOnDispose => true;

            // the deferred-position invariant (docs/nano-writer.md): bytes written into the
            // leased chunk are uncommitted until TryFlush hands them to the IBufferWriter, and
            // state.OffsetInCurrent is exactly how many those are - so the per-op writer-object
            // position advance is pure duplication of a count the span write already maintains
            private protected override long GetUncommitted(in State state) => state.OffsetInCurrent;

            // ---- when the destination will not give us a usable chunk ----
            //
            // The size passed to GetMemory/GetSpan is a HINT. The documented contract is "at
            // least this much, or throw", but it is a contract we neither control nor can
            // verify: a simplistic destination may hand back a fixed small block however much
            // is asked for, and in the limit one byte at a time.
            //
            // "Large but not large enough" needs nothing - a chunk at least as wide as the
            // widest single op can be written into, and the room checks simply re-lease more
            // often. Only an UNUSABLE chunk is a problem, and the answer is to stop using the
            // destination's memory: lease our own region, and hand the bytes over on flush via
            // BuffersExtensions.Write, which loops GetSpan/Advance internally and so copes with
            // any chunk size the destination cares to offer. The fragmentation becomes its
            // problem, which is where it belongs.
            //
            // The choice LATCHES: a destination that gave an unusable chunk once will do it
            // again, and re-probing would burn a GetMemory call per chunk to learn nothing.

            /// <summary>
            /// The narrowest chunk that can be written into at all: the widest single op is a
            /// 10-byte varint, and the room checks assume that much is available after one test.
            /// </summary>
            private const int UsableLease = 16;

            private byte[] _ownedLease; // non-null once the destination proved unusable

            private protected override bool TryFlush(ref State state)
            {
                if (state.IsActive)
                {
                    int bytes = 0;
                    bool step = false;
                    try
                    {
                        bytes = state.ConsiderWritten();
                        step = true;
                        Advance(bytes); // uncommitted -> committed; position is unchanged across this
                        if (_ownedLease is not null)
                        {
                            // our memory, not theirs: push it across in whatever sizes they will
                            // take. Called STATICALLY and by type name, not as an extension
                            // method, and that is deliberate: `writer.Write(span)` binds to
                            // whichever identically-shaped extension happens to be in scope, and
                            // there is at least one in the wild (CommunityToolkit.HighPerformance)
                            // whose version assumes GetSpan honours the hint - the very thing
                            // being defended against here. Naming the type pins the BCL's
                            // multi-segment implementation and makes that unbindable.
                            if (bytes != 0) BuffersExtensions.Write(_writer, new ReadOnlySpan<byte>(_ownedLease, 0, bytes));
                        }
                        else
                        {
                            _writer.Advance(bytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        var data = ex.Data;
                        if (data is not null)
                        {
                            data.Add("ProtoBuf.Operation", step ? "Advance" : "ConsiderWritten");
                            data.Add("ProtoBuf.Position", _position64);
                            data.Add("ProtoBuf.Flushing", bytes);
                        }
                        throw;
                    }
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
                    try
                    {
                        UTF8.GetBytes(value, 0, value.Length, arr, 0);
                        FallbackWriteBytes(ref state, new ReadOnlySpan<byte>(arr, 0, expectedBytes));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(arr);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void GetBuffer(ref State state)
            {
                var writer = _writer;
                if (writer is null) ThrowNoWriter();
                TryFlush(ref state);

                // _needFlush at LEASE time, not per tag: once a chunk is out, there is
                // uncommitted data until something flushes it, and this is the one place a
                // chunk is taken - which is what lets a span-direct raw op touch no writer
                // state at all (docs/nano-writer.md, buffer-core step 2)
                _needFlush = true;

                // the room checks in this backend ("if (RemainingInCurrent < 10) GetBuffer") only
                // work if the lease is at least as wide as the widest primitive written without
                // re-checking, so the demand has a floor. TypeModel.BufferSize enforces the same
                // floor on the way in; this is belt-and-braces for the model-less path, and the
                // one place that would overrun if the property ever stopped normalising.
                int bytes = Math.Max(model is null ? BufferPool.BUFFER_LENGTH : model.BufferSize,
                    Meta.TypeModel.MinimumBufferSize);
                bool step = false;
                try
                {
                    if (_ownedLease is not null)
                    {
                        // already latched: never ask the destination for memory again
                        state.Init(_ownedLease);
                    }
                    else
                    {
                        var buffer = _writer.GetMemory(bytes);
                        step = true;
                        if (buffer.Length >= UsableLease)
                        {
                            state.Init(buffer);
                        }
                        else
                        {
                            // unusable: take our own region and latch (see the note above).
                            // Nothing was Advance()d, so the chunk we are declining is simply
                            // abandoned, which is what an IBufferWriter permits.
                            _ownedLease = BufferPool.GetBuffer(bytes);
                            state.Init(_ownedLease);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var data = ex.Data;
                    if (data is not null)
                    {
                        data.Add("ProtoBuf.Operation", step ? "Init" : "GetMemory");
                        data.Add("ProtoBuf.Position", _position64);
                        data.Add("ProtoBuf.Requesting", bytes);
                    }
                    throw;
                }

                static void ThrowNoWriter() => throw new InvalidOperationException("Invalid state: there is no writer for this instance");
            }

            private protected override void ImplWriteBytes(ref State state, ReadOnlySpan<byte> bytes)
            {
                if (bytes.Length <= state.RemainingInCurrent) state.LocalWriteBytes(bytes);
                else FallbackWriteBytes(ref state, bytes);
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
                    int remaining = state.RemainingInCurrent;
                    if (span.Length <= remaining)
                    {
                        state.LocalWriteBytes(span);
                        return;
                    }
                    else
                    {
                        state.LocalWriteBytes(span.Slice(0, remaining));
                        span = span.Slice(remaining);
                    }
                }
            }

            private protected override int ImplWriteVarint32(ref State state, uint value)
            {
                if (state.RemainingInCurrent < 5) GetBuffer(ref state);
                return state.LocalWriteVarint32(value);
            }

            internal override int ImplWriteVarint64(ref State state, ulong value)
            {
                if (state.RemainingInCurrent < 10) GetBuffer(ref state);
                return state.LocalWriteVarint64(value);
            }

            protected internal override void WriteMessage<T>(ref State state, T value, ISerializer<T> serializer,
                PrefixStyle style, bool recursionCheck)
            {
                switch (WireType)
                {
                    case WireType.String:
                    case WireType.Fixed32:
                        PreSubItem(ref state, TypeHelper<T>.IsReferenceType & recursionCheck ? (object)value : null);
                        WriteWithLengthPrefix<T>(ref state, value, serializer, style);
                        PostSubItem(ref state);
                        return;
                    case WireType.StartGroup:
                    default:
                        base.WriteMessage<T>(ref state, value, serializer, style, recursionCheck);
                        return;
                }
            }

            internal override void WriteWrappedItem<T>(ref State state, SerializerFeatures features, T value, ISerializer<T> serializer)
            {
                switch (WireType)
                {
                    case WireType.String:
                        serializer ??= TypeModel.ResolveSerializer<T>(Model);
                        long calculatedLength = MeasureAny<T>(_nullWriter, TypeModel.ListItemTag, features, value, serializer);

                        // write length-prefix as varint
                        ImplWriteVarint64(ref state, (ulong)calculatedLength);
                        ResetWireType();

                        if (calculatedLength != 0)
                        {
                            var oldPos = GetPosition(in state);
                            state.WriteAny(TypeModel.ListItemTag, features, value, serializer);
                            var newPos = GetPosition(in state);

                            var actualLength = (newPos - oldPos);
                            if (actualLength != calculatedLength)
                            {
                                ThrowHelper.ThrowInvalidOperationException($"Length mismatch; calculated '{calculatedLength}', actual '{actualLength}'");
                            }
                        }

                        return;
                    case WireType.StartGroup:
                        // forwards-only; can use default implementation
                        base.WriteWrappedItem<T>(ref state, features, value, serializer);
                        return;
                    default:
                        // if we aren't using length-prefix or group... what are we even?
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(WireType));
                        return;
                }
            }

            internal override void WriteWrappedCollection<TCollection, TItem>(ref State state, SerializerFeatures features, TCollection values, RepeatedSerializer<TCollection, TItem> serializer, ISerializer<TItem> valueSerializer)
            {
                switch (WireType)
                {
                    case WireType.String:
                        valueSerializer ??= TypeModel.ResolveSerializer<TItem>(Model);
                        long calculatedLength = MeasureRepeated<TCollection, TItem>(_nullWriter, TypeModel.ListItemTag, features, values, serializer, valueSerializer);

                        // write length-prefix as varint
                        ImplWriteVarint64(ref state, (ulong)calculatedLength);
                        ResetWireType();

                        if (calculatedLength != 0)
                        {
                            var oldPos = GetPosition(in state);
                            serializer.WriteRepeated(ref state, TypeModel.ListItemTag, features, values, valueSerializer);
                            var newPos = GetPosition(in state);

                            var actualLength = (newPos - oldPos);
                            if (actualLength != calculatedLength)
                            {
                                ThrowHelper.ThrowInvalidOperationException($"Length mismatch; calculated '{calculatedLength}', actual '{actualLength}'");
                            }
                        }

                        return;
                    case WireType.StartGroup:
                        // forwards-only; can use default implementation
                        base.WriteWrappedCollection<TCollection, TItem>(ref state, features, values, serializer, valueSerializer);
                        return;
                    default:
                        // if we aren't using length-prefix or group... what are we even?
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(WireType));
                        return;
                }
            }

            internal override void WriteWrappedMap<TCollection, TKey, TValue>(ref State state, SerializerFeatures features, TCollection values, MapSerializer<TCollection, TKey, TValue> serializer, SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
            {
                switch (WireType)
                {
                    case WireType.String:
                        long calculatedLength = MeasureMap<TCollection, TKey, TValue>(_nullWriter, TypeModel.ListItemTag, features, values, serializer, keyFeatures, valueFeatures, keySerializer, valueSerializer);

                        // write length-prefix as varint
                        ImplWriteVarint64(ref state, (ulong)calculatedLength);
                        ResetWireType();

                        if (calculatedLength != 0)
                        {
                            var oldPos = GetPosition(in state);
                            serializer.WriteMap(ref state, TypeModel.ListItemTag, features, values, keyFeatures, valueFeatures, keySerializer, valueSerializer);
                            var newPos = GetPosition(in state);

                            var actualLength = (newPos - oldPos);
                            if (actualLength != calculatedLength)
                            {
                                ThrowHelper.ThrowInvalidOperationException($"Length mismatch; calculated '{calculatedLength}', actual '{actualLength}'");
                            }
                        }

                        return;
                    case WireType.StartGroup:
                        // forwards-only; can use default implementation
                        base.WriteWrappedMap(ref state, features, values, serializer, keyFeatures, valueFeatures, keySerializer, valueSerializer);
                        return;
                    default:
                        // if we aren't using length-prefix or group... what are we even?
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(WireType));
                        return;
                }
            }

            protected internal override void WriteSubType<T>(ref State state, T value, ISubTypeSerializer<T> serializer)
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

            private void WriteWithLengthPrefix<T>(ref State state, T value, ISerializer<T> serializer, PrefixStyle style)
                => WriteMeasuredWithLengthPrefix<T>(_nullWriter, ref state, value, serializer, style);

            private void WriteWithLengthPrefix<T>(ref State state, T value, ISubTypeSerializer<T> serializer)
                where T : class
            {
                serializer ??= TypeModel.GetSubTypeSerializer<T>(Model);
                long calculatedLength = Measure<T>(_nullWriter, value, serializer);
                
                // we'll always use varint here
                ImplWriteVarint64(ref state, (ulong)calculatedLength);
                ResetWireType();
                var oldPos = GetPosition(in state);
                serializer.WriteSubType(ref state, value);
                var newPos = GetPosition(in state);

                var actualLength = (newPos - oldPos);
                if (actualLength != calculatedLength)
                {
                    ThrowHelper.ThrowInvalidOperationException($"Length mismatch; calculated '{calculatedLength}', actual '{actualLength}'");
                }
            }

            private protected override void ImplEndLengthPrefixedSubItem(ref State state, SubItemToken token, PrefixStyle style)
                => ThrowHelper.ThrowNotSupportedException("You must use the WriteMessage API with this writer type");

            private protected override SubItemToken ImplStartLengthPrefixedSubItem(ref State state, object instance, PrefixStyle style)
            {
                ThrowHelper.ThrowNotSupportedException("You must use the WriteMessage API with this writer type");
                return default;
            }

            private protected override void ImplCopyRawFromStream(ref State state, Stream source)
            {
                while (true)
                {
                    if (state.RemainingInCurrent == 0) GetBuffer(ref state);

                    // ReadFrom lands in the leased chunk, so it advances the uncommitted
                    // offset itself; there is nothing to account for here
                    if (state.ReadFrom(source) <= 0) break;
                }
            }
        }
    }
}