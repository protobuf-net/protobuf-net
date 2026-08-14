using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    /// <summary>
    /// Vectorised sizing for a packed varint column — `notes/gaps.md` B19.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A varint length is a <b>threshold ladder</b>, so it needs no leading-zero intrinsic:
    /// <c>1 + (v &gt;= 2^7) + (v &gt;= 2^14) + …</c>. A vector comparison yields all-ones per lane,
    /// and <b>subtracting</b> that mask adds one — so a block costs four compares and four
    /// accumulates with no horizontal work until the end.
    /// </para>
    /// <para>
    /// Measured at <b>1.8×–6.6×</b> over the scalar loop depending on the value distribution
    /// (`PackedSizeBenchmarks`). The branch-free *scalar* form was measured too and is a
    /// pessimisation — 1.13× to 4.27× worse — so the win here is genuinely SIMD rather than
    /// branch-avoidance, and a "simplify to branchless first" step would make things slower.
    /// </para>
    /// <para>
    /// <b>Only three primitives are needed</b>, because the rest pun: <c>long</c> reinterprets as
    /// <c>ulong</c> (a negative long is a large ulong, and both encode as the same 64-bit
    /// two's-complement varint), enums pun to their underlying type, and zigzag transforms first.
    /// <c>int</c> cannot pun to <c>uint</c>: protobuf sign-extends a negative <c>int32</c> to the
    /// <b>10-byte</b> form, so it needs its own arm.
    /// </para>
    /// <para>
    /// Every result is cross-checked at run time by the caller: <c>WritePacked</c> compares the
    /// bytes actually written against the length calculated here and throws
    /// <c>packed encoding length miscalculation</c> on a mismatch — so an error here is loud
    /// rather than a corrupt payload.
    /// </para>
    /// </remarks>
    internal static class PackedVarintMeasure
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Len(uint value)
            => value < 1u << 7 ? 1 : value < 1u << 14 ? 2 : value < 1u << 21 ? 3 : value < 1u << 28 ? 4 : 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Len(ulong value)
        {
            int len = 1;
            while ((value >>= 7) != 0) len++;
            return len;
        }

        /// <summary>Unsigned 32-bit: 1-5 bytes, four thresholds.</summary>
        internal static long Measure(ReadOnlySpan<uint> values)
        {
            long len = values.Length;   // every value is at least one byte
            int i = 0;
            if (Vector.IsHardwareAccelerated && values.Length >= Vector<uint>.Count)
            {
                Vector<uint> t7 = new(1u << 7), t14 = new(1u << 14),
                             t21 = new(1u << 21), t28 = new(1u << 28), acc = Vector<uint>.Zero;
                for (; i <= values.Length - Vector<uint>.Count; i += Vector<uint>.Count)
                {
                    var v = Load(values, i);
                    acc -= Vector.GreaterThanOrEqual(v, t7);
                    acc -= Vector.GreaterThanOrEqual(v, t14);
                    acc -= Vector.GreaterThanOrEqual(v, t21);
                    acc -= Vector.GreaterThanOrEqual(v, t28);
                }
                for (int lane = 0; lane < Vector<uint>.Count; lane++) len += acc[lane];
            }
            for (; i < values.Length; i++) len += Len(values[i]) - 1;   // the ragged tail
            return len;
        }

        /// <summary>
        /// Signed 32-bit. A negative sign-extends to the <b>10-byte</b> form, so this is not the
        /// unsigned ladder with a cast: the negative lanes are blended in at 10.
        /// </summary>
        internal static long Measure(ReadOnlySpan<int> values)
        {
            long len = values.Length;
            int i = 0;
            if (Vector.IsHardwareAccelerated && values.Length >= Vector<int>.Count)
            {
                Vector<uint> t7 = new(1u << 7), t14 = new(1u << 14),
                             t21 = new(1u << 21), t28 = new(1u << 28), acc = Vector<uint>.Zero;
                var nine = new Vector<uint>(9);      // 10 total, and 1 is already counted
                for (; i <= values.Length - Vector<int>.Count; i += Vector<int>.Count)
                {
                    var s = Load(values, i);
                    var negative = Vector.LessThan(s, Vector<int>.Zero);
                    var v = Vector.AsVectorUInt32(s);
                    // the unsigned ladder for the non-negative lanes...
                    var positive = Vector.AndNot(
                        (Vector.GreaterThanOrEqual(v, t7) & Vector<uint>.One)
                        + (Vector.GreaterThanOrEqual(v, t14) & Vector<uint>.One)
                        + (Vector.GreaterThanOrEqual(v, t21) & Vector<uint>.One)
                        + (Vector.GreaterThanOrEqual(v, t28) & Vector<uint>.One),
                        Vector.AsVectorUInt32(negative));
                    // ...and a flat 9 extra for the negative ones
                    acc += positive + (nine & Vector.AsVectorUInt32(negative));
                }
                for (int lane = 0; lane < Vector<uint>.Count; lane++) len += acc[lane];
            }
            for (; i < values.Length; i++)
            {
                len += values[i] < 0 ? 9 : Len(unchecked((uint)values[i])) - 1;
            }
            return len;
        }

        /// <summary>Unsigned 64-bit: 1-10 bytes, nine thresholds. `long` puns onto this.</summary>
        internal static long Measure(ReadOnlySpan<ulong> values)
        {
            long len = values.Length;
            int i = 0;
            if (Vector.IsHardwareAccelerated && values.Length >= Vector<ulong>.Count)
            {
                var acc = Vector<ulong>.Zero;
                for (; i <= values.Length - Vector<ulong>.Count; i += Vector<ulong>.Count)
                {
                    var v = Load(values, i);
                    for (int shift = 7; shift < 64; shift += 7)
                    {
                        acc -= Vector.GreaterThanOrEqual(v, new Vector<ulong>(1UL << shift));
                    }
                }
                for (int lane = 0; lane < Vector<ulong>.Count; lane++) len += (long)acc[lane];
            }
            for (; i < values.Length; i++) len += Len(values[i]) - 1;
            return len;
        }

        /// <summary>
        /// ZigZag 32-bit. The transform needs <b>no shift instructions</b>: <c>v &lt;&lt; 1</c> is
        /// <c>v + v</c>, and the arithmetic <c>v &gt;&gt; 31</c> is exactly
        /// <c>Vector.LessThan(v, Zero)</c>. That matters because <c>Vector.ShiftLeft</c> is .NET 7+,
        /// so a shift-based form would have no vector path down-level at all.
        /// </summary>
        internal static long MeasureZigZag(ReadOnlySpan<int> values)
        {
            long len = values.Length;
            int i = 0;
            if (Vector.IsHardwareAccelerated && values.Length >= Vector<int>.Count)
            {
                Vector<uint> t7 = new(1u << 7), t14 = new(1u << 14),
                             t21 = new(1u << 21), t28 = new(1u << 28), acc = Vector<uint>.Zero;
                for (; i <= values.Length - Vector<int>.Count; i += Vector<int>.Count)
                {
                    var s = Load(values, i);
                    var zig = Vector.AsVectorUInt32((s + s) ^ Vector.LessThan(s, Vector<int>.Zero));
                    acc -= Vector.GreaterThanOrEqual(zig, t7);
                    acc -= Vector.GreaterThanOrEqual(zig, t14);
                    acc -= Vector.GreaterThanOrEqual(zig, t21);
                    acc -= Vector.GreaterThanOrEqual(zig, t28);
                }
                for (int lane = 0; lane < Vector<uint>.Count; lane++) len += acc[lane];
            }
            for (; i < values.Length; i++)
            {
                len += Len(unchecked((uint)((values[i] << 1) ^ (values[i] >> 31)))) - 1;
            }
            return len;
        }

        /// <summary>ZigZag 64-bit, same identity at 64 bits.</summary>
        internal static long MeasureZigZag(ReadOnlySpan<long> values)
        {
            long len = values.Length;
            int i = 0;
            if (Vector.IsHardwareAccelerated && values.Length >= Vector<long>.Count)
            {
                var acc = Vector<ulong>.Zero;
                for (; i <= values.Length - Vector<long>.Count; i += Vector<long>.Count)
                {
                    var s = Load(values, i);
                    var zig = Vector.AsVectorUInt64((s + s) ^ Vector.LessThan(s, Vector<long>.Zero));
                    for (int shift = 7; shift < 64; shift += 7)
                    {
                        acc -= Vector.GreaterThanOrEqual(zig, new Vector<ulong>(1UL << shift));
                    }
                }
                for (int lane = 0; lane < Vector<ulong>.Count; lane++) len += (long)acc[lane];
            }
            for (; i < values.Length; i++)
            {
                len += Len(unchecked((ulong)((values[i] << 1) ^ (values[i] >> 63)))) - 1;
            }
            return len;
        }

        /// <summary>
        /// The generic entry point: dispatches an <b>array</b> of unconstrained
        /// <typeparamref name="T"/> onto the right ladder, or returns false so the caller keeps its
        /// per-element loop.
        /// </summary>
        /// <remarks>
        /// The array is cast through <c>object</c> rather than reinterpreted with
        /// <c>MemoryMarshal.Cast</c>, because <typeparamref name="T"/> is unconstrained here — the
        /// repeated serializers are generic over reference types too — and <c>Cast</c> demands
        /// <c>struct</c>. Each <c>typeof</c> test folds at JIT time for a value-typed
        /// instantiation, so exactly one arm survives per closed generic.
        /// <para>
        /// <paramref name="count"/> is separate from the array length so a <c>List&lt;T&gt;</c> can
        /// share this by passing its backing array and its <c>Count</c>.
        /// </para>
        /// </remarks>
        internal static bool TryMeasure<T>(T[] values, int count, WireType wireType, out long length)
        {
            length = 0;
            if (count == 0) return false;

            if (wireType == WireType.Varint)
            {
                if (typeof(T) == typeof(uint))
                { length = Measure(new ReadOnlySpan<uint>((uint[])(object)values, 0, count)); return true; }
                if (typeof(T) == typeof(int))
                { length = Measure(new ReadOnlySpan<int>((int[])(object)values, 0, count)); return true; }
                if (typeof(T) == typeof(ulong))
                { length = Measure(new ReadOnlySpan<ulong>((ulong[])(object)values, 0, count)); return true; }
                if (typeof(T) == typeof(long))
                {
                    // puns onto ulong: a negative long IS a large ulong, and both encode as the
                    // same 64-bit two's-complement varint, so the lengths are identical
                    var raw = new ReadOnlySpan<long>((long[])(object)values, 0, count);
                    length = Measure(System.Runtime.InteropServices.MemoryMarshal.Cast<long, ulong>(raw));
                    return true;
                }
            }
            else if (wireType == WireType.SignedVarint)
            {
                if (typeof(T) == typeof(int))
                { length = MeasureZigZag(new ReadOnlySpan<int>((int[])(object)values, 0, count)); return true; }
                if (typeof(T) == typeof(long))
                { length = MeasureZigZag(new ReadOnlySpan<long>((long[])(object)values, 0, count)); return true; }
            }
            return false;
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// The same dispatch over a span, for a <c>List&lt;T&gt;</c> reached through
        /// <c>CollectionsMarshal</c>.
        /// </summary>
        /// <remarks>
        /// Guarded to net5+ because that is where <c>CollectionsMarshal.AsSpan</c> and
        /// <c>MemoryMarshal.CreateReadOnlySpan</c> both live. Down-level a list keeps the
        /// per-element loop, which costs little in practice: protogen emits <b>arrays</b> for
        /// packable scalars and lists only for the shapes that are never packed.
        /// </remarks>
        internal static bool TryMeasure<T>(ReadOnlySpan<T> values, WireType wireType, out long length)
        {
            length = 0;
            if (values.IsEmpty) return false;

            if (wireType == WireType.Varint)
            {
                if (typeof(T) == typeof(uint)) { length = Measure(As<T, uint>(values)); return true; }
                if (typeof(T) == typeof(int)) { length = Measure(As<T, int>(values)); return true; }
                if (typeof(T) == typeof(ulong)) { length = Measure(As<T, ulong>(values)); return true; }
                if (typeof(T) == typeof(long)) { length = Measure(As<T, ulong>(values)); return true; }
            }
            else if (wireType == WireType.SignedVarint)
            {
                if (typeof(T) == typeof(int)) { length = MeasureZigZag(As<T, int>(values)); return true; }
                if (typeof(T) == typeof(long)) { length = MeasureZigZag(As<T, long>(values)); return true; }
            }
            return false;
        }

        /// <summary>
        /// Reinterprets a span of unconstrained <typeparamref name="TFrom"/> — which is why this is
        /// not <c>MemoryMarshal.Cast</c>, whose <c>struct</c> constraint the callers cannot satisfy.
        /// Only ever reached once the element type has been proven by a <c>typeof</c> test.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<TTo> As<TFrom, TTo>(ReadOnlySpan<TFrom> values)
            => System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<TFrom, TTo>(
                    ref System.Runtime.InteropServices.MemoryMarshal.GetReference(values)),
                values.Length);
#endif

        /// <summary>
        /// Loads a vector from a span. The span overload of <c>Vector&lt;T&gt;</c>'s constructor is
        /// .NET 5+, so this goes through <c>Unsafe</c> to keep one implementation across TFMs.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Load<T>(ReadOnlySpan<T> values, int offset) where T : struct
            => Unsafe.ReadUnaligned<Vector<T>>(
                ref Unsafe.As<T, byte>(ref Unsafe.Add(ref System.Runtime.InteropServices.MemoryMarshal
                    .GetReference(values), offset)));
    }
}
