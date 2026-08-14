using BenchmarkDotNet.Attributes;
using ProtoBuf.Meta;
using ProtoBuf.Nano.Bench.Packed;
using System;
using System.Collections.Generic;
using System.IO;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// The packed matrix, measured: classic emit versus the raw writer, per encoding category.
/// </summary>
/// <remarks>
/// <para>
/// <b>999 elements</b>, deliberately: it is not a multiple of 8 (the AVX2 lane count for 32-bit),
/// nor of 4 (64-bit), nor even of 2 — so every vectorised path has a ragged tail and the tail
/// handling is exercised on every run rather than only when someone remembers to test it.
/// </para>
/// <para>
/// Values are drawn with a fixed seed and span the width classes, because the ladder's cost
/// depends entirely on the distribution: a column of small values exits on the first comparison,
/// a column of wide ones runs all four. A quarter of the signed values are negative, which is the
/// case that costs 10 bytes rather than 5 and so cannot be left out.
/// </para>
/// <para>
/// Both models serialize the same instances, so any divergence is emission and not data. The
/// equivalence is separately gated by <c>ClassicVsRawTests</c>.
/// </para>
/// </remarks>
[MemoryDiagnoser(false)]
public class PackedMatrixBenchmarks
{
    private const int N = 999;   // NOT a multiple of 8, 4 or 2: the tail always matters

    private readonly TypeModel _raw = PackedRawModel.Instance;
    private readonly TypeModel _classic = PackedClassicModel.Instance;

    private PackedVarintUnsigned _unsigned;
    private PackedVarintSigned _signed;
    private PackedZigZag _zigzag;
    private PackedFixedInt _fixedInt;
    private PackedFloatingPoint _floats;
    private PackedBools _bools;
    private PackedEnums _enums;

    private readonly MemoryStream _ms = new();

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(12345);

        static T[] Fill<T>(int n, Func<int, T> gen)
        {
            var arr = new T[n];
            for (int i = 0; i < n; i++) arr[i] = gen(i);
            return arr;
        }

        // spread across the width classes: a quarter tiny, a quarter small, a quarter medium,
        // a quarter wide - so no arm is measured on a distribution that flatters it
        uint Spread(int i) => (i & 3) switch
        {
            0 => (uint)rand.Next(0, 128),
            1 => (uint)rand.Next(128, 1 << 14),
            2 => (uint)rand.Next(1 << 14, 1 << 21),
            _ => (uint)rand.Next(1 << 21, int.MaxValue),
        };

        // ...and a quarter of the signed ones negative, which is the 10-byte case
        int Signed(int i) => (i & 3) == 3 ? -(int)(Spread(i) & 0x7FFFFFFF) : (int)(Spread(i) & 0x7FFFFFFF);

        var u32 = Fill(N, Spread);
        var u64 = Fill(N, i => ((ulong)Spread(i) << 20) | Spread(i));
        var i32 = Fill(N, Signed);
        var i64 = Fill(N, i => (long)Signed(i) * 1_000_003L);

        _unsigned = new PackedVarintUnsigned
        {
            U32Array = u32, U32List = new List<uint>(u32),
            U64Array = u64, U64List = new List<ulong>(u64),
        };
        _signed = new PackedVarintSigned
        {
            I32Array = i32, I32List = new List<int>(i32),
            I64Array = i64, I64List = new List<long>(i64),
        };
        _zigzag = new PackedZigZag
        {
            S32Array = i32, S32List = new List<int>(i32),
            S64Array = i64, S64List = new List<long>(i64),
        };
        _fixedInt = new PackedFixedInt
        {
            F32Array = i32, F32List = new List<int>(i32),
            F64Array = i64, F64List = new List<long>(i64),
        };
        var f32 = Fill(N, i => i * 1.5f);
        var f64 = Fill(N, i => i * 1.25d);
        _floats = new PackedFloatingPoint
        {
            SingleArray = f32, SingleList = new List<float>(f32),
            DoubleArray = f64, DoubleList = new List<double>(f64),
        };
        var bools = Fill(N, i => (i % 3) == 0);
        _bools = new PackedBools { BoolArray = bools, BoolList = new List<bool>(bools) };
        var levels = Fill(N, i => (Level)(i & 3));
        _enums = new PackedEnums { EnumArray = levels, EnumList = new List<Level>(levels) };

        // both models must agree byte-for-byte, or the comparison below is meaningless
        foreach (var value in new object[] { _unsigned, _signed, _zigzag, _fixedInt, _floats, _bools, _enums })
        {
            var a = Bytes(_raw, value);
            var b = Bytes(_classic, value);
            if (a != b) throw new InvalidOperationException(
                $"raw and classic disagree for {value.GetType().Name}");
        }
    }

    private static string Bytes(TypeModel model, object value)
    {
        using var ms = new MemoryStream();
        model.Serialize(ms, value);
        return BitConverter.ToString(ms.ToArray());
    }

    private long Write(TypeModel model, object value)
    {
        _ms.Position = 0;
        _ms.SetLength(0);
        model.Serialize(_ms, value);
        return _ms.Length;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("varint-unsigned")]
    public long UnsignedClassic() => Write(_classic, _unsigned);
    [Benchmark, BenchmarkCategory("varint-unsigned")]
    public long UnsignedRaw() => Write(_raw, _unsigned);

    [Benchmark, BenchmarkCategory("varint-signed")]
    public long SignedClassic() => Write(_classic, _signed);
    [Benchmark, BenchmarkCategory("varint-signed")]
    public long SignedRaw() => Write(_raw, _signed);

    [Benchmark, BenchmarkCategory("zigzag")]
    public long ZigZagClassic() => Write(_classic, _zigzag);
    [Benchmark, BenchmarkCategory("zigzag")]
    public long ZigZagRaw() => Write(_raw, _zigzag);

    [Benchmark, BenchmarkCategory("fixed-int")]
    public long FixedIntClassic() => Write(_classic, _fixedInt);
    [Benchmark, BenchmarkCategory("fixed-int")]
    public long FixedIntRaw() => Write(_raw, _fixedInt);

    [Benchmark, BenchmarkCategory("floating")]
    public long FloatsClassic() => Write(_classic, _floats);
    [Benchmark, BenchmarkCategory("floating")]
    public long FloatsRaw() => Write(_raw, _floats);

    [Benchmark, BenchmarkCategory("bool")]
    public long BoolsClassic() => Write(_classic, _bools);
    [Benchmark, BenchmarkCategory("bool")]
    public long BoolsRaw() => Write(_raw, _bools);

    [Benchmark, BenchmarkCategory("enum")]
    public long EnumsClassic() => Write(_classic, _enums);
    [Benchmark, BenchmarkCategory("enum")]
    public long EnumsRaw() => Write(_raw, _enums);
}
