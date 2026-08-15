using ProtoBuf.Internal;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    partial class ProtoWriter
    {
        public ref partial struct State
        {
            // ---- the raw PACKED write surface (notes/packed-writes.md) ----
            //
            // A packed column is one tag over a whole payload, so the generator emits exactly two
            // calls for it: a measure and a write, symmetric and both taking a SPAN. The span is
            // produced at the CALL SITE, because the generator knows the collection shape (array,
            // List<T> via CollectionsMarshal, ImmutableArray<T> via AsSpan) and the element type,
            // including an enum's underlying type - so this surface is per element ENCODING and
            // never per collection shape, and an enum column arrives already punned and is
            // thereafter indistinguishable from the primitive one.
            //
            // FRAMING LIVES HERE, NOT AT THE CALL SITE, and that is the one deliberate exception
            // to "decide at the call site". protobuf-net's rules are byte-visible and not
            // obvious - an EMPTY collection writes a zero-length header, a SINGLE element is
            // written UNPACKED with its own per-element header, and only two-or-more is actually
            // packed - so inlining them per member is how they drift, and every drift is a wire
            // bug that only a byte oracle catches. Measure and write call the same decision, so
            // they agree by construction rather than by review.
            //
            // Hence the FIELD NUMBER argument rather than a pre-encoded tag, which looks
            // inconsistent with the scalar surface above and is not: for a single scalar the tag
            // encode can cost as much as the value it introduces, which is what the narrowed tag
            // ladder is for; against a whole packed payload it is noise. And the single-element
            // arm needs the number anyway, since it writes a per-element header.

            /// <summary>The tag is a varint, so a large field number costs more than one byte.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int TagLength(int fieldNumber) => MeasureUInt32((uint)fieldNumber << 3);

            /// <summary>
            /// The framing arithmetic, shared by every measure below: what a packed member costs
            /// in total, given the payload it would write and the per-element cost of the
            /// single-element form.
            /// </summary>
            private static long PackedTotal(int fieldNumber, int count, long payload, long single)
            {
                var tag = TagLength(fieldNumber);
                if (count == 0) return tag + 1;              // zero-length header: tag, then 0x00
                if (count == 1) return tag + single;         // written unpacked, with its own header
                return tag + MeasureUInt64((ulong)payload) + payload;
            }

            /// <summary>Writes the header for a packed payload, or the two degenerate forms.</summary>
            /// <returns>true if the caller should go on to write the packed body.</returns>
            private bool WritePackedHeader(int fieldNumber, int count, long payload)
            {
                if (count == 0)
                {
                    WriteRawTag(((uint)fieldNumber << 3) | 2);
                    WriteRawVarint32(0);
                    return false;
                }
                if (count == 1) return false;                // caller writes the single element unpacked
                WriteRawTag(((uint)fieldNumber << 3) | 2);
                WriteRawVarint64((ulong)payload);
                return true;
            }

            // ---- varint ----

            /// <summary>Total bytes for a packed <c>uint32</c> column, framing included.</summary>
            public static long MeasureRawPackedVarint(int fieldNumber, ReadOnlySpan<uint> values)
                => PackedTotal(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.Measure(values) : 0,
                    values.Length == 1 ? MeasureUInt32(values[0]) : 0);

            /// <summary>Writes a packed <c>uint32</c> column.</summary>
            public void WriteRawPackedVarint(int fieldNumber, ReadOnlySpan<uint> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTag((uint)fieldNumber << 3);
                    WriteRawVarint32(values[0]);
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.Measure(values) : 0)) return;
                PackedVarintMeasure.WritePackedUInt32(ref this, values);
            }

            /// <summary>
            /// Total bytes for a packed <c>int32</c> column. Not the unsigned overload with a
            /// cast: a negative <c>int32</c> sign-extends to the ten-byte form.
            /// </summary>
            public static long MeasureRawPackedVarint(int fieldNumber, ReadOnlySpan<int> values)
                => PackedTotal(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.Measure(values) : 0,
                    values.Length == 1 ? MeasureUInt64(unchecked((ulong)(long)values[0])) : 0);

            /// <summary>Writes a packed <c>int32</c> column.</summary>
            public void WriteRawPackedVarint(int fieldNumber, ReadOnlySpan<int> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTag((uint)fieldNumber << 3);
                    WriteRawVarint64(unchecked((ulong)(long)values[0]));
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.Measure(values) : 0)) return;
                PackedVarintMeasure.WritePackedInt32(ref this, values);
            }

            /// <summary>
            /// Total bytes for a packed 64-bit column. <c>long</c> puns onto this at the call
            /// site: a negative <c>long</c> is a large <c>ulong</c>, and both encode as the same
            /// 64-bit two's-complement varint.
            /// </summary>
            public static long MeasureRawPackedVarint(int fieldNumber, ReadOnlySpan<ulong> values)
                => PackedTotal(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.Measure(values) : 0,
                    values.Length == 1 ? MeasureUInt64(values[0]) : 0);

            /// <summary>Writes a packed 64-bit varint column.</summary>
            public void WriteRawPackedVarint(int fieldNumber, ReadOnlySpan<ulong> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTag((uint)fieldNumber << 3);
                    WriteRawVarint64(values[0]);
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.Measure(values) : 0)) return;
                PackedVarintMeasure.WritePackedUInt64(ref this, values);
            }

            // ---- zigzag ----
            //
            // The measure is already vectorised (the transform needs no shift instructions: v << 1
            // is v + v, and the arithmetic v >> 31 IS Vector.LessThan(v, Zero)). The WRITE is still
            // per element here — it already drops the enumerator, the virtual dispatch and the
            // wire-type switch that the stateful path pays, which is most of the win. A tier-1
            // blit is available in principle, since a zigzag value below 128 means an original in
            // [-64, 63], but it needs the transform materialised before the uniformity test, so it
            // is a different shape from the plain varint one rather than a parameter of it.

            /// <summary>Total bytes for a packed <c>sint32</c> column.</summary>
            public static long MeasureRawPackedZigZag(int fieldNumber, ReadOnlySpan<int> values)
                => PackedTotal(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.MeasureZigZag(values) : 0,
                    values.Length == 1 ? MeasureUInt32(Zig(values[0])) : 0);

            /// <summary>Writes a packed <c>sint32</c> column.</summary>
            public void WriteRawPackedZigZag(int fieldNumber, ReadOnlySpan<int> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTag((uint)fieldNumber << 3);
                    WriteRawZigZag32(values[0]);
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.MeasureZigZag(values) : 0)) return;
                foreach (var value in values) WriteRawZigZag32(value);
            }

            /// <summary>Total bytes for a packed <c>sint64</c> column.</summary>
            public static long MeasureRawPackedZigZag(int fieldNumber, ReadOnlySpan<long> values)
                => PackedTotal(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.MeasureZigZag(values) : 0,
                    values.Length == 1 ? MeasureUInt64(Zig(values[0])) : 0);

            /// <summary>Writes a packed <c>sint64</c> column.</summary>
            public void WriteRawPackedZigZag(int fieldNumber, ReadOnlySpan<long> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTag((uint)fieldNumber << 3);
                    WriteRawZigZag64(values[0]);
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length,
                    values.Length > 1 ? PackedVarintMeasure.MeasureZigZag(values) : 0)) return;
                foreach (var value in values) WriteRawZigZag64(value);
            }

            // ---- bool ----
            //
            // A bool LOOKS like a varint and behaves like a fixed width: false is 0x00 and true is
            // 0x01, so the payload length IS the count and, on any sane runtime, the span already
            // IS the payload. The scan guards that "on any sane runtime" - the CLI permits a bool
            // whose byte is neither 0 nor 1, and a blit would put it on the wire verbatim.

            /// <summary>Total bytes for a packed <c>bool</c> column.</summary>
            public static long MeasureRawPackedBool(int fieldNumber, int count)
                => PackedTotal(fieldNumber, count, count, 1);

            /// <summary>Writes a packed <c>bool</c> column.</summary>
            public void WriteRawPackedBool(int fieldNumber, ReadOnlySpan<bool> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTagBool(((uint)fieldNumber << 3), values[0]);
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length, values.Length)) return;
                var raw = System.Runtime.InteropServices.MemoryMarshal.AsBytes(values);
                if (PackedVarintMeasure.AllCanonicalBools(raw))
                {
                    WriteRawBytesBody(raw);
                    return;
                }
                foreach (var value in values) WriteRawVarint32(value ? 1u : 0u);
            }

            // ---- fixed width ----
            //
            // O(1) to measure and, on a little-endian machine, a straight block copy to write -
            // the CLR's in-memory layout for these IS the wire layout. float/double/int/long are
            // punned onto these two at the call site, where the generator knows the type.

            /// <summary>Total bytes for a packed 4-byte fixed-width column.</summary>
            public static long MeasureRawPackedFixed32(int fieldNumber, int count)
                => PackedTotal(fieldNumber, count, count * 4L, 4);

            /// <summary>Total bytes for a packed 8-byte fixed-width column.</summary>
            public static long MeasureRawPackedFixed64(int fieldNumber, int count)
                => PackedTotal(fieldNumber, count, count * 8L, 8);

            /// <summary>Writes a packed 4-byte fixed-width column.</summary>
            public void WriteRawPackedFixed32(int fieldNumber, ReadOnlySpan<uint> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTag(((uint)fieldNumber << 3) | 5);
                    WriteRawFixed32(values[0]);
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length, values.Length * 4L)) return;
                if (BitConverter.IsLittleEndian)
                {
                    WriteRawBytesBody(System.Runtime.InteropServices.MemoryMarshal.AsBytes(values));
                    return;
                }
                foreach (var value in values) WriteRawFixed32(value);
            }

            /// <summary>Writes a packed 8-byte fixed-width column.</summary>
            public void WriteRawPackedFixed64(int fieldNumber, ReadOnlySpan<ulong> values)
            {
                if (values.Length == 1)
                {
                    WriteRawTag(((uint)fieldNumber << 3) | 1);
                    WriteRawFixed64(values[0]);
                    return;
                }
                if (!WritePackedHeader(fieldNumber, values.Length, values.Length * 8L)) return;
                if (BitConverter.IsLittleEndian)
                {
                    WriteRawBytesBody(System.Runtime.InteropServices.MemoryMarshal.AsBytes(values));
                    return;
                }
                foreach (var value in values) WriteRawFixed64(value);
            }
        }
    }
}
