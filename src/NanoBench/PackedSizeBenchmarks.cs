using BenchmarkDotNet.Attributes;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// Sizing a packed varint column — gap B19. How long is the payload for N integers?
/// </summary>
/// <remarks>
/// <para>
/// This is the ONLY part of packed writing that costs anything to size: <c>WritePacked</c> handles
/// the fixed widths in O(1) (<c>count * 4</c>, <c>count * 8</c>), so floats and the fixed integer
/// forms need no measuring at all. The varint arm measures per element, which is where a span and
/// a vector unit might help.
/// </para>
/// <para>
/// <b>No leading-zero intrinsic is needed</b>, which is what makes this plausible: a varint length
/// is a threshold ladder, so <c>1 + (v >= 2^7) + (v >= 2^14) + ...</c> vectorises as four compares
/// and four accumulates per block. A vector comparison yields all-ones per lane, and subtracting
/// that mask adds one — so there is no horizontal work until the very end.
/// </para>
/// <para>
/// The distributions matter more than the strategies, as they did for the varint measure work:
/// real field values are overwhelmingly small, and a ladder that exits early on the first
/// comparison is already cheap in scalar form.
/// </para>
/// </remarks>
[MemoryDiagnoser(false)]
public class PackedSizeBenchmarks
{
    [Params(16, 256, 4096)]
    public int Count { get; set; }

    /// <summary>
    /// small: every value one byte, which is what most real columns look like.
    /// mixed: a realistic spread across the widths.
    /// wide: everything at four or five bytes, the adversarial case for an early-exit ladder.
    /// </summary>
    [Params("small", "mixed", "wide")]
    public string Distribution { get; set; } = "small";

    private uint[] _values = [];

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(12345);   // fixed seed: comparing strategies, not data
        _values = new uint[Count];
        for (int i = 0; i < Count; i++)
        {
            _values[i] = Distribution switch
            {
                "small" => (uint)rand.Next(0, 128),
                "wide" => (uint)rand.Next(1 << 21, int.MaxValue),
                _ => rand.Next(4) switch
                {
                    0 => (uint)rand.Next(0, 128),
                    1 => (uint)rand.Next(128, 1 << 14),
                    2 => (uint)rand.Next(1 << 14, 1 << 21),
                    _ => (uint)rand.Next(1 << 21, int.MaxValue),
                },
            };
        }

        // the arms must agree, or the comparison is meaningless - and a packed length that is
        // wrong by one byte is a corrupt payload, which WritePacked's own validation would catch
        if (Scalar() != Vectorised() || Scalar() != Ladder())
        {
            throw new InvalidOperationException(
                $"arms disagree: scalar={Scalar()}, vectorised={Vectorised()}, ladder={Ladder()}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int VarintLen(uint value)
        => value < 1u << 7 ? 1
        : value < 1u << 14 ? 2
        : value < 1u << 21 ? 3
        : value < 1u << 28 ? 4 : 5;

    /// <summary>The shape a per-element measure has today: one call per value.</summary>
    [Benchmark(Baseline = true)]
    public long Scalar()
    {
        var values = _values;
        long len = 0;
        for (int i = 0; i < values.Length; i++) len += VarintLen(values[i]);
        return len;
    }

    /// <summary>
    /// Branch-free scalar: the same threshold ladder, accumulated as bools rather than branched.
    /// Included because it isolates how much of any win is SIMD and how much is just removing the
    /// branches — a distinction the vector arm alone cannot make.
    /// </summary>
    [Benchmark]
    public long Ladder()
    {
        var values = _values;
        long len = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            var v = values[i];
            len += (v >= 1u << 7 ? 1 : 0) + (v >= 1u << 14 ? 1 : 0)
                 + (v >= 1u << 21 ? 1 : 0) + (v >= 1u << 28 ? 1 : 0);
        }
        return len;
    }

    /// <summary>The vectorised ladder: four compares and four accumulates per block.</summary>
    [Benchmark]
    public long Vectorised()
    {
        var values = _values;      // the ARRAY overload of Vector<T>, since the
        long len = values.Length;  // span one is net5+ and this project is also net472
        int i = 0;

        if (Vector.IsHardwareAccelerated && values.Length >= Vector<uint>.Count)
        {
            var t7 = new Vector<uint>(1u << 7);
            var t14 = new Vector<uint>(1u << 14);
            var t21 = new Vector<uint>(1u << 21);
            var t28 = new Vector<uint>(1u << 28);
            var acc = Vector<uint>.Zero;

            for (; i <= values.Length - Vector<uint>.Count; i += Vector<uint>.Count)
            {
                var v = new Vector<uint>(values, i);
                // a true lane is all-ones; SUBTRACTING it adds one, so no masking is needed
                acc -= Vector.GreaterThanOrEqual(v, t7);
                acc -= Vector.GreaterThanOrEqual(v, t14);
                acc -= Vector.GreaterThanOrEqual(v, t21);
                acc -= Vector.GreaterThanOrEqual(v, t28);
            }

            for (int lane = 0; lane < Vector<uint>.Count; lane++) len += acc[lane];
        }

        for (; i < values.Length; i++)      // the tail, and the whole span when not accelerated
        {
            var v = values[i];
            len += (v >= 1u << 7 ? 1 : 0) + (v >= 1u << 14 ? 1 : 0)
                 + (v >= 1u << 21 ? 1 : 0) + (v >= 1u << 28 ? 1 : 0);
        }
        return len;
    }
}
