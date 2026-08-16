using BenchmarkDotNet.Attributes;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// How many bytes will this varint need? Never benchmarked against alternatives until now -
/// NanoBench had thirteen files, all decode-side plus the two serialize composites.
/// </summary>
/// <remarks>
/// <para>
/// Constant field tags are already folded to literals by the generator, so what actually reaches
/// this at run time is narrower than it looks: sub-message LENGTH PREFIXES, and runtime scalar
/// values. The length-prefix case is the hot one and its distribution is not uniform - most
/// messages are well under 16KiB, so one or two bytes dominates. That is why the distributions
/// below matter at least as much as the strategies: a comparison ladder wins on small-dominant
/// data and loses on uniform.
/// </para>
/// <para>
/// The down-level (net472) arm of the shipped code is <see cref="Loop"/> - one iteration per 7
/// bits, no intrinsic - and is the arm nobody looks at.
/// </para>
/// </remarks>
[MemoryDiagnoser(false)]
public class VarintMeasureBenchmarks
{
    private const int N = 4096;
    private uint[] _u32 = [];
    private ulong[] _u64 = [];

    /// <summary>
    /// small: everything fits one byte - the dominant real case.
    /// prefix: sub-message lengths, heavily weighted to 1-2 bytes, which is what the hot caller
    ///   actually feeds in.
    /// uniform: the adversarial case for any branch ladder.
    /// wide: values needing the long forms, including the sign-extended negative int32 shape.
    /// </summary>
    [Params("small", "prefix", "uniform", "wide")]
    public string Distribution { get; set; } = "prefix";

    [GlobalSetup]
    public void Setup()
    {
        // fixed seed: the point is to compare strategies, so every run must see identical data
        var rand = new Random(12345);
        _u32 = new uint[N];
        _u64 = new ulong[N];
        for (int i = 0; i < N; i++)
        {
            uint v = Distribution switch
            {
                "small" => (uint)rand.Next(0, 128),
                "prefix" => rand.Next(100) switch
                {
                    < 55 => (uint)rand.Next(0, 128),        // 1 byte
                    < 90 => (uint)rand.Next(128, 16384),    // 2 bytes
                    < 98 => (uint)rand.Next(16384, 1 << 21),// 3 bytes
                    _ => (uint)rand.Next(1 << 21, int.MaxValue),
                },
                "wide" => (uint)rand.Next(1 << 21, int.MaxValue),
                _ => unchecked((uint)rand.Next(int.MinValue, int.MaxValue)),
            };
            _u32[i] = v;
            // the wide arm also exercises the 10-byte form a negative int32 sign-extends into
            _u64[i] = Distribution == "wide" && (i & 1) == 0
                ? unchecked((ulong)(long)(int)v) : v;
        }

        // AGREEMENT BEFORE TIMING: a strategy that is wrong would simply look fast. Checked
        // against the shipped form over the actual data, plus the boundary values, since the
        // random sample may miss an exact power-of-two edge.
        foreach (var v in _u32)
        {
            Check(v, Current(v), Log2Div(v), nameof(Log2Div));
            Check(v, Current(v), MulShift(v), nameof(MulShift));
            Check(v, Current(v), Ladder(v), nameof(Ladder));
            Check(v, Current(v), Table(v), nameof(Table));
            Check(v, Current(v), Loop(v), nameof(Loop));
            Check(v, Current(v), Switch(v), nameof(Switch));
            Check(v, Current(v), SwitchShift(v), nameof(SwitchShift));
        }
        for (int bit = 0; bit < 32; bit++)
        {
            foreach (var v in new[] { (1u << bit) - 1, 1u << bit })
            {
                Check(v, Current(v), Log2Div(v), nameof(Log2Div));
                Check(v, Current(v), MulShift(v), nameof(MulShift));
                Check(v, Current(v), Ladder(v), nameof(Ladder));
                Check(v, Current(v), Table(v), nameof(Table));
                Check(v, Current(v), Loop(v), nameof(Loop));
                Check(v, Current(v), Switch(v), nameof(Switch));
                Check(v, Current(v), SwitchShift(v), nameof(SwitchShift));
            }
        }
        foreach (var v in _u64)
        {
            Check(v, Current64(v), Ladder64(v), nameof(Ladder64));
            Check(v, Current64(v), Hybrid64(v), nameof(Hybrid64));
            Check(v, Current64(v), Loop64(v), nameof(Loop64));
            Check(v, Current64(v), Table64(v), nameof(Table64));
            Check(v, Current64(v), MulShift64(v), nameof(MulShift64));
        }
        for (int bit = 0; bit < 64; bit++)
        {
            foreach (var v in new[] { (1ul << bit) - 1, 1ul << bit })
            {
                Check(v, Current64(v), Ladder64(v), nameof(Ladder64));
                Check(v, Current64(v), Hybrid64(v), nameof(Hybrid64));
                Check(v, Current64(v), Loop64(v), nameof(Loop64));
                Check(v, Current64(v), Table64(v), nameof(Table64));
                Check(v, Current64(v), MulShift64(v), nameof(MulShift64));
            }
        }
        Console.WriteLine($"// {Distribution}: all strategies agree with the shipped form");

        static void Check(ulong value, int expected, int actual, string name)
        {
            if (expected != actual)
                throw new InvalidOperationException($"{name}({value}) = {actual}, expected {expected}");
        }
    }

    // ---- strategies, 32-bit ----

    /// <summary>The shipped intrinsic form.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Current(uint value) => ((31 - BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;

    /// <summary>Same intent via Log2; worth knowing whether it emits identically.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Log2Div(uint value) => (BitOperations.Log2(value | 1) / 7) + 1;

    /// <summary>Replaces the divide-by-7 with a multiply-shift, to test whether the JIT's own
    /// lowering of the constant divide is already optimal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MulShift(uint value)
        => (int)(((uint)BitOperations.Log2(value | 1) * 37) >> 8) + 1;

    /// <summary>The shipped DOWN-LEVEL form: one iteration per 7 bits, no intrinsic.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Loop(uint value)
    {
        int count = 1;
        while ((value >>= 7) != 0) count++;
        return count;
    }

    /// <summary>Comparison ladder: no intrinsic needed, and near-perfect prediction when small
    /// values dominate - which is the realistic case.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Ladder(uint value)
        => value < 1u << 7 ? 1
        : value < 1u << 14 ? 2
        : value < 1u << 21 ? 3
        : value < 1u << 28 ? 4 : 5;

    private static ReadOnlySpan<byte> LengthByLeadingZeros =>
        [5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3,
         3, 3, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1];

    /// <summary>Trades the arithmetic for a load from a u8 blob.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Table(uint value) => LengthByLeadingZeros[BitOperations.LeadingZeroCount(value | 1)];

    /// <summary>Switch expression over the leading-zero count - the compiler may emit a jump
    /// table. Note this repo's DispatchResults.md found a jump table LOSING to a predicted
    /// comparison chain on ordered data, so this is a real question rather than a formality.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Switch(uint value) => BitOperations.LeadingZeroCount(value | 1) switch
    {
        0 or 1 or 2 or 3 => 5,
        4 or 5 or 6 or 7 or 8 or 9 or 10 => 4,
        11 or 12 or 13 or 14 or 15 or 16 or 17 => 3,
        18 or 19 or 20 or 21 or 22 or 23 or 24 => 2,
        _ => 1,
    };

    /// <summary>Switch expression over the value's own magnitude, rather than over lzcnt.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SwitchShift(uint value) => (value >> 7) switch
    {
        0 => 1,
        < 1u << 7 => 2,
        < 1u << 14 => 3,
        < 1u << 21 => 4,
        _ => 5,
    };

    // ---- runners; summed so nothing is elided ----

    [Benchmark(Baseline = true)]
    public int U32_Current() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += Current(a[i]); return t; }

    [Benchmark]
    public int U32_Log2Div() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += Log2Div(a[i]); return t; }

    [Benchmark]
    public int U32_MulShift() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += MulShift(a[i]); return t; }

    [Benchmark]
    public int U32_Ladder() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += Ladder(a[i]); return t; }

    [Benchmark]
    public int U32_Table() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += Table(a[i]); return t; }

    [Benchmark]
    public int U32_Switch() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += Switch(a[i]); return t; }

    [Benchmark]
    public int U32_SwitchShift() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += SwitchShift(a[i]); return t; }

    [Benchmark]
    public int U32_Loop() { var a = _u32; int t = 0; for (int i = 0; i < a.Length; i++) t += Loop(a[i]); return t; }

    // ---- 64-bit: the length-prefix path, and where a ladder gets longer ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Current64(ulong value) => ((63 - BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Loop64(ulong value)
    {
        int count = 1;
        while ((value >>= 7) != 0) count++;
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Ladder64(ulong value)
        => value < 1ul << 7 ? 1
        : value < 1ul << 14 ? 2
        : value < 1ul << 21 ? 3
        : value < 1ul << 28 ? 4
        : value < 1ul << 35 ? 5
        : value < 1ul << 42 ? 6
        : value < 1ul << 49 ? 7
        : value < 1ul << 56 ? 8
        : value < 1ul << 63 ? 9 : 10;

    /// <summary>Small values are overwhelmingly common, so try one predicted compare before the
    /// general form.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Hybrid64(ulong value)
        => value < 1ul << 7 ? 1 : ((63 - BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;

    private static ReadOnlySpan<byte> Length64ByLeadingZeros =>
        [10,  9,  9,  9,  9,  9,  9,  9,  8,  8,  8,  8,  8,  8,  8,  7,
          7,  7,  7,  7,  7,  7,  6,  6,  6,  6,  6,  6,  6,  5,  5,  5,
          5,  5,  5,  5,  4,  4,  4,  4,  4,  4,  4,  3,  3,  3,  3,  3,
          3,  3,  2,  2,  2,  2,  2,  2,  2,  1,  1,  1,  1,  1,  1,  1,
          1];

    /// <summary>The gap in the first pass: Table dominated for u32, so it very likely does here.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Table64(ulong value) => Length64ByLeadingZeros[BitOperations.LeadingZeroCount(value | 1)];

    /// <summary>The divide removed, as MulShift does for 32-bit - worth 18% there.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MulShift64(ulong value)
        => (int)(((uint)BitOperations.Log2(value | 1) * 37) >> 8) + 1;

    [Benchmark]
    public int U64_Table() { var a = _u64; int t = 0; for (int i = 0; i < a.Length; i++) t += Table64(a[i]); return t; }

    [Benchmark]
    public int U64_MulShift() { var a = _u64; int t = 0; for (int i = 0; i < a.Length; i++) t += MulShift64(a[i]); return t; }

    [Benchmark]
    public int U64_Current() { var a = _u64; int t = 0; for (int i = 0; i < a.Length; i++) t += Current64(a[i]); return t; }

    [Benchmark]
    public int U64_Ladder() { var a = _u64; int t = 0; for (int i = 0; i < a.Length; i++) t += Ladder64(a[i]); return t; }

    [Benchmark]
    public int U64_Hybrid() { var a = _u64; int t = 0; for (int i = 0; i < a.Length; i++) t += Hybrid64(a[i]); return t; }

    [Benchmark]
    public int U64_Loop() { var a = _u64; int t = 0; for (int i = 0; i < a.Length; i++) t += Loop64(a[i]); return t; }
}
