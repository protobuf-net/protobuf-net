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
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void ThrowNullRepeatedContents<T>()
                => Internal.ThrowHelper.ThrowNullRepeatedContents<T>();
        }
    }
}
