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
            // SURFACE-FIRST, deliberately: these are veneers over the existing backend
            // machinery (Impl* virtuals), so every backend - buffer-writer, stream, and
            // crucially the NULL writer, whose Impl* stores are no-ops - serves them today,
            // and the Null backend gives MEASURE MODE for free. The fast presized-region
            // core lands beneath this surface later without touching generated code: the
            // same playbook the reader ran (the generated emission is the contract; the
            // engine swaps underneath).
            //
            // The raw convention, mirrored from the read side: the generator knows every
            // tag and wire form at compile time, so the WireType handshake the stateful API
            // performs (WriteFieldHeader records state; the value write switches on it)
            // is skipped entirely - the tag is a compile-time constant argument and the
            // value write names its own encoding. Every op still leaves WireType = None
            // (AdvanceAndReset), so raw and legacy-mode member writes interleave safely
            // within one body, exactly as the read side's StashTag arms do.

            /// <summary>
            /// Raw-convention field tag write: the tag is a compile-time constant from the
            /// generator ((field &lt;&lt; 3) | wire), written as-is with no state handshake.
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawTag(uint tag)
            {
                var writer = _writer;
                writer._needFlush = true;
                writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, tag));
            }

            /// <summary>Raw-convention varint write (32-bit).</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawVarint32(uint value)
            {
                var writer = _writer;
                writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, value));
            }

            /// <summary>Raw-convention varint write (64-bit); a negative int32/int64 arrives
            /// here sign-extended to the 10-byte form, exactly as the stateful writer emits.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawVarint64(ulong value)
            {
                var writer = _writer;
                writer.AdvanceAndReset(writer.ImplWriteVarint64(ref this, value));
            }

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
                var writer = _writer;
                writer.ImplWriteFixed32(ref this, value);
                writer.AdvanceAndReset(4);
            }

            /// <summary>Raw-convention fixed64 write.</summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawFixed64(ulong value)
            {
                var writer = _writer;
                writer.ImplWriteFixed64(ref this, value);
                writer.AdvanceAndReset(8);
            }

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
                var writer = _writer;
                if (value.Length == 0)
                {
                    writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, 0));
                }
                else
                {
                    var len = UTF8.GetByteCount(value);
                    writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, (uint)len) + len);
                    writer.ImplWriteString(ref this, value, len);
                }
            }

            /// <summary>
            /// Raw-convention bytes write: length prefix plus body. The caller guards null.
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void WriteRawBytes(ReadOnlySpan<byte> value)
            {
                var writer = _writer;
                writer.AdvanceAndReset(writer.ImplWriteVarint32(ref this, (uint)value.Length) + value.Length);
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
            public System.Collections.Generic.Dictionary<object, int> RawLengths => _writer.RawLengths;

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
                out System.Collections.Generic.Dictionary<object, int> lengths)
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
            public static int MeasureRawExtensionData(IExtensible instance)
                => MeasureRawExtensionDataImpl(instance.GetExtensionObject(false));

            /// <summary>The typed-bag form of <see cref="MeasureRawExtensionData(IExtensible)"/>,
            /// keyed per hierarchy layer exactly as the write is.</summary>
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public static int MeasureRawExtensionData(ITypedExtensible instance, Type type)
                => MeasureRawExtensionDataImpl(instance.GetExtensionObject(type, false));

            private static int MeasureRawExtensionDataImpl(IExtension extn)
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
                    return checked((int)(source.Length - source.Position));
                }
                finally { extn.EndQuery(source); }
            }
        }
    }
}
