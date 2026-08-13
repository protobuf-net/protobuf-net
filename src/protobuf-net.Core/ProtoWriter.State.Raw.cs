using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    partial class ProtoWriter
    {
        public ref partial struct State
        {
            // ---- the raw write surface (docs/nano-writer.md) ----
            //
            // These are SPAN-DIRECT where the backend has a leased chunk with room: the store
            // goes straight into State's own span, touching the writer object not at all - no
            // virtual Impl* hop, no position advance (position is derived from the offset the
            // store maintains, see the invariant on ProtoWriter), no wire-type reset. Where
            // there is no room - or no span at all, which is the stream and NULL backends -
            // control falls out-of-line to the veneer over the Impl* virtuals, so every
            // backend still serves the whole surface and the Null one still gives MEASURE
            // MODE for free.
            //
            // The raw convention, mirrored from the read side: the generator knows every
            // tag and wire form at compile time, so the WireType handshake the stateful API
            // performs (WriteFieldHeader records state; the value write switches on it) is
            // skipped entirely - the tag is a compile-time constant argument and the value
            // write names its own encoding.
            //
            // WIRE-TYPE, and why nothing here writes it: it is None on entry to any serializer
            // body (every framing path resets before handing over) and every stateful op
            // resets after itself, so a raw body starts at None and stays there - which is
            // what lets raw and legacy-mode member writes interleave within one body, exactly
            // as the read side's StashTag arms do. Cut 1 reset it per-op only because the
            // veneers shared AdvanceAndReset.
            //
            // _needFlush likewise moves to LEASE time (the backend sets it when it takes a
            // chunk) rather than being stamped per tag; the slow tag path still sets it, for
            // the backends that never lease.

            // the widest encoding each op can emit without re-checking; a lease is never
            // smaller than the largest of these (see BufferWriterProtoWriter.MinimumLease)
            private const int MaxVarint32 = 5, MaxVarint64 = 10;

            /// <summary>
            /// The number of bytes written so far. Under the writer's deferred-position
            /// invariant this is DERIVED - the bytes the backend has committed, plus whatever
            /// it is still holding in its pending buffer - so that a write op does not have to
            /// maintain a position alongside the buffer offset it is already advancing.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public readonly long Position64 => _writer.GetPosition(in this);

            /// <summary>
            /// Raw-convention field tag write: the tag is a compile-time constant from the
            /// generator ((field &lt;&lt; 3) | wire), written as-is with no state handshake.
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawTag(uint tag)
            {
                // the single-byte arm is the dominant case (fields 1-15) and the argument is a
                // compile-time constant at every generated call site, so the test folds away
                if (tag < 0x80 && RemainingInCurrent >= 1) LocalWriteByte((byte)tag);
                else if (RemainingInCurrent >= MaxVarint32) LocalWriteVarint32(tag);
                else SlowWriteRawTag(tag);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowWriteRawTag(uint tag)
            {
                var writer = _writer;
                writer._needFlush = true; // a backend that never leases has no other hook
                writer.ImplWriteVarint32(ref this, tag);
            }

            /// <summary>Raw-convention varint write (32-bit).</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawVarint32(uint value)
            {
                if (RemainingInCurrent >= MaxVarint32) LocalWriteVarint32(value);
                else SlowWriteRawVarint32(value);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowWriteRawVarint32(uint value)
                => _writer.ImplWriteVarint32(ref this, value);

            /// <summary>Raw-convention varint write (64-bit); a negative int32/int64 arrives
            /// here sign-extended to the 10-byte form, exactly as the stateful writer emits.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawVarint64(ulong value)
            {
                if (RemainingInCurrent >= MaxVarint64) LocalWriteVarint64(value);
                else SlowWriteRawVarint64(value);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowWriteRawVarint64(ulong value)
                => _writer.ImplWriteVarint64(ref this, value);

            /// <summary>Raw-convention zig-zag write (32-bit).</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawZigZag32(int value)
                => WriteRawVarint32(unchecked((uint)((value << 1) ^ (value >> 31))));

            /// <summary>Raw-convention zig-zag write (64-bit).</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawZigZag64(long value)
                => WriteRawVarint64(unchecked((ulong)((value << 1) ^ (value >> 63))));

            /// <summary>Raw-convention fixed32 write.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawFixed32(uint value)
            {
                if (RemainingInCurrent >= 4) LocalWriteFixed32(value);
                else SlowWriteRawFixed32(value);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowWriteRawFixed32(uint value)
                => _writer.ImplWriteFixed32(ref this, value);

            /// <summary>Raw-convention fixed64 write.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawFixed64(ulong value)
            {
                if (RemainingInCurrent >= 8) LocalWriteFixed64(value);
                else SlowWriteRawFixed64(value);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowWriteRawFixed64(ulong value)
                => _writer.ImplWriteFixed64(ref this, value);

            /// <summary>Raw-convention float write (fixed32 bits). The netfx arm reinterprets
            /// via Unsafe: SingleToInt32Bits does not exist down-level.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawSingle(float value)
            {
#if NET7_0_OR_GREATER
                WriteRawFixed32(unchecked((uint)BitConverter.SingleToInt32Bits(value)));
#else
                WriteRawFixed32(Unsafe.As<float, uint>(ref value));
#endif
            }

            /// <summary>Raw-convention double write (fixed64 bits).</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawDouble(double value)
                => WriteRawFixed64(unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));

            /// <summary>
            /// Raw-convention string write: length prefix plus UTF-8 body (the tag was written
            /// by the caller, per the raw convention). The caller guards null - a null member
            /// is simply not written - and an empty string is a zero-length prefix, exactly as
            /// the stateful path emits.
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawString(string value)
            {
                if (value.Length == 0)
                {
                    WriteRawVarint32(0);
                    return;
                }
                var len = UTF8.GetByteCount(value);
                // prefix and body land in one chunk or neither does, so the fast path needs
                // room for both; a string straddling a boundary is the slow path's problem
                if (RemainingInCurrent >= MaxVarint32 + len)
                {
                    LocalWriteVarint32((uint)len);
                    LocalWriteString(value);
                }
                else SlowWriteRawString(value, len);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowWriteRawString(string value, int len)
            {
                var writer = _writer;
                writer.ImplWriteVarint32(ref this, (uint)len);
                writer.ImplWriteString(ref this, value, len);
            }

            /// <summary>
            /// Raw-convention bytes write: length prefix plus body. The caller guards null.
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawBytes(ReadOnlySpan<byte> value)
            {
                if (RemainingInCurrent >= MaxVarint32 + value.Length)
                {
                    LocalWriteVarint32((uint)value.Length);
                    if (value.Length != 0) LocalWriteBytes(value);
                }
                else SlowWriteRawBytes(value);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SlowWriteRawBytes(ReadOnlySpan<byte> value)
            {
                var writer = _writer;
                writer.ImplWriteVarint32(ref this, (uint)value.Length);
                if (value.Length != 0) writer.ImplWriteBytes(ref this, value);
            }

            /// <summary>
            /// Throws for a null element inside a collection, matching the stateful repeated
            /// write; generated raw loops call this so the failure is the same exception with
            /// the same message, rather than a bare NullReferenceException from the write.
            /// Static rather than instance, so a generated Measure_ body - pure arithmetic,
            /// no state - can report the same failure at the same point.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static void ThrowNullRepeatedContents<T>()
                => Internal.ThrowHelper.ThrowNullRepeatedContents<T>();

            // ---- the raw measure surface (docs/nano-writer.md, the measure-first cut) ----
            //
            // Generated Measure_ statics size a sub-message by PURE ARITHMETIC - no writer, no
            // State, no virtual dispatch - so the raw write can emit an exact length prefix and
            // then the body, instead of the stateful engine's reserve-and-patch. These are the
            // only pieces of that arithmetic the generator cannot fold at compile time.

            /// <summary>Raw-convention varint measure (32-bit): the encoded byte count.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static int MeasureRawVarint32(uint value) => MeasureUInt32(value);

            /// <summary>Raw-convention varint measure (64-bit); a negative int32/int64 arrives
            /// sign-extended to the 10-byte form, exactly as the raw write encodes it.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static int MeasureRawVarint64(ulong value) => MeasureUInt64(value);

            /// <summary>
            /// Raw-convention string measure: the length prefix plus the UTF-8 body, exactly
            /// what <see cref="WriteRawString"/> emits. The caller guards null.
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static int MeasureRawString(string value)
            {
                var len = value.Length == 0 ? 0 : UTF8.GetByteCount(value);
                return MeasureUInt32((uint)len) + len;
            }

            /// <summary>
            /// The measure pass's length cache (docs/nano-writer.md, the <c>??=</c> design):
            /// sub-message lengths keyed by reference identity, populated by the generated
            /// Measure_ statics and consumed at the write sites, so each object is measured
            /// once per root serialize however deep or shared it is. Cleared per root by the
            /// writer; value-type contracts have no identity and bypass it.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public System.Collections.Generic.Dictionary<object, long> RawLengths => _writer.RawLengths;

            /// <summary>
            /// The remaining nesting budget for a generated measure recursion, honouring
            /// <see cref="Meta.TypeModel.MaxDepth"/> exactly as the raw reader's depth cap does.
            /// Only the measure walk needs guarding: the write recursion that follows traverses
            /// the identical graph the measure just proved finite.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public int RawDepthBudget
            {
                get
                {
                    var writer = _writer;
                    return (writer.Model is null ? Meta.TypeModel.DefaultMaxDepth : writer.Model.MaxDepth) - writer.Depth;
                }
            }

            /// <summary>
            /// Recovers the raw measure inputs - the depth budget and the length cache - from a
            /// serialization context, where that context is a writer. The generated
            /// <c>IMeasuringSerializer</c> implementations call this so the classic engine's
            /// measure hook (<c>ProtoWriter.Measure</c>) lands on Measure_ arithmetic instead of
            /// a null-writer traversal - which is how a MIXED shape (a map entry, a non-native
            /// collection element, a measurable member of an unmeasurable parent) benefits from
            /// the measure pass without being native itself. A message body's length does not
            /// depend on its framing, so no wire-type test is needed; a non-writer context
            /// returns false, the caller reports a non-positive length, and the engine measures
            /// by writing, exactly as before.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static bool TryMeasureRaw(ISerializationContext context, out int depthBudget,
                out System.Collections.Generic.Dictionary<object, long> lengths)
            {
                if (context is ProtoWriter writer)
                {
                    depthBudget = (writer.Model is null ? Meta.TypeModel.DefaultMaxDepth : writer.Model.MaxDepth) - writer.Depth;
                    lengths = writer.RawLengths;
                    return true;
                }
                depthBudget = 0;
                lengths = null;
                return false;
            }

            /// <summary>Throws for an exhausted measure budget - a cyclic or pathologically deep
            /// object graph - mirroring the stateful writer's depth failure.</summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static void ThrowRawTooDeep()
                => throw new InvalidOperationException("Maximum model depth exceeded (see "
                    + nameof(Meta.TypeModel) + "." + nameof(Meta.TypeModel.MaxDepth) + ") while measuring");

            /// <summary>
            /// Raw-convention measure of an instance's extension blob: the stored bytes carry
            /// their own field headers, so the size is simply the blob's length. The query
            /// stream must be seekable - every <see cref="Extensible"/>/buffer-backed extension
            /// is - since the measure must not consume what the write is about to copy.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static long MeasureRawExtensionData(IExtensible instance)
                => MeasureRawExtensionDataImpl(instance.GetExtensionObject(false));

            /// <summary>The typed-bag form of <see cref="MeasureRawExtensionData(IExtensible)"/>,
            /// keyed per hierarchy layer exactly as the write is.</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static long MeasureRawExtensionData(ITypedExtensible instance, Type type)
                => MeasureRawExtensionDataImpl(instance.GetExtensionObject(type, false));

            private static long MeasureRawExtensionDataImpl(IExtension extn)
            {
                if (extn is null) return 0;
                var source = extn.BeginQuery();
                try
                {
                    if (!source.CanSeek)
                    {
                        // a custom IExtension yielding a forward-only query stream cannot be
                        // measured without consuming it; ClassicEmit is the escape hatch
                        Internal.ThrowHelper.ThrowNotSupportedException(
                            "Extension data cannot be measured over a non-seekable query stream; consider [ProtoModel(ClassicEmit = true)]");
                    }
                    return source.Length - source.Position;
                }
                finally { extn.EndQuery(source); }
            }
        }
    }
}
