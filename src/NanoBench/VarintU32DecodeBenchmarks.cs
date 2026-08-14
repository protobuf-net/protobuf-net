#if NET7_0_OR_GREATER // BitOperations/Bmi2; the strategy study is a modern-TFM concern
using BenchmarkDotNet.Attributes;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// Strategies for decoding a u32 varint (strict, 1-5 bytes) from a byte[] with plenty of data in
/// the buffer: the harness pads the tail so an 8-byte unaligned load is always safe, which is the
/// "lots of data in the current buffer" assumption made explicit - the buffer-end tail takes a
/// slow path in real code and is not what this measures. Little-endian assumed throughout.
///
/// Length 1-5 are uniform streams (flattering to branchy strategies - the predictor learns the
/// length); Length=0 is the shuffled mixed stream, which is where branchless earns its keep.
/// Every strategy is validated against the reference in GlobalSetup: wrong sum or wrong total
/// length throws before a single measurement is taken.
///
/// Not yet covered, deliberately: buffer offset/alignment variation, the tolerant 10-byte value
/// path (negative int32 arrives sign-extended; tags are strict-5, values need a rarely-taken
/// 6-10 byte spill), arm64, and call-shape variants for the winner. See notes/nano-core.md.
///
/// The loops hoist the array root ref once per batch: this approximates the intended reader
/// layout, where the root lives in a C# 11 ref field (.NET 7+) and the position is a byte offset
/// applied with Unsafe.Add - down-level TFMs use arr[index] behind per-TFM inlined accessors.
/// </summary>
public class VarintU32DecodeBenchmarks
{
    /// <summary>1-5 = uniform streams of that encoded length; 0 = shuffled mix of 1-5.</summary>
    [Params(1, 2, 3, 4, 5, 0)]
    public int Length;

    private const int Count = 1024;
    private byte[] _data = [];
    private uint _expectedSum;
    private int _expectedBytes;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(12345);
        var values = new uint[Count];
        for (int i = 0; i < Count; i++)
        {
            int len = Length == 0 ? 1 + (i % 5) : Length;
            // uniform within the values that encode to exactly len bytes
            uint min = len == 1 ? 0u : 1u << (7 * (len - 1));
            ulong max = len == 5 ? uint.MaxValue : (1ul << (7 * len)) - 1;
            values[i] = min + (uint)(rng.NextDouble() * (max - min));
        }
        if (Length == 0)
        {
            rng.Shuffle(values);
        }

        var data = new byte[Count * 5 + 8]; // +8: unaligned u64 loads never run off the end
        int offset = 0;
        foreach (var value in values)
        {
            uint v = value;
            _expectedSum += v;
            while (v >= 0x80)
            {
                data[offset++] = (byte)(v | 0x80);
                v >>= 7;
            }
            data[offset++] = (byte)v;
        }
        _expectedBytes = offset;
        _data = data;

        Validate(ByteLoop, nameof(ByteLoop));
        Validate(ByteUnrolled, nameof(ByteUnrolled));
        Validate(EarlyExit1Then4, nameof(EarlyExit1Then4));
        Validate(EarlyExit2Then8, nameof(EarlyExit2Then8));
        Validate(Load4Then1, nameof(Load4Then1));
        Validate(Load8TzcntSwar, nameof(Load8TzcntSwar));
        Validate(Load8TzcntPext, nameof(Load8TzcntPext));
        Validate(Load8Switch, nameof(Load8Switch));
    }

    private delegate uint Decoder(ref byte src, out int len);

    private void Validate(Decoder decoder, string name)
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += decoder(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        if (sum != _expectedSum || offset != _expectedBytes)
        {
            throw new InvalidOperationException(
                $"{name} is wrong: sum {sum} vs {_expectedSum}, bytes {offset} vs {_expectedBytes}");
        }
    }

    // ---------------------------------------------------------------- strategies

    /// <summary>The classic: one byte at a time, checking the continuation bit.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ByteLoop(ref byte src, out int len)
    {
        uint value = src;
        if ((value & 0x80) == 0) { len = 1; return value; }
        value &= 0x7F;
        int shift = 7, i = 1;
        while (true)
        {
            uint b = Unsafe.Add(ref src, i++);
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) { len = i; return value; }
            shift += 7;
        }
    }

    /// <summary>Fully unrolled if-chain (the Google.Protobuf shape) - no loop-carried shift.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ByteUnrolled(ref byte src, out int len)
    {
        uint value = src;
        if ((value & 0x80) == 0) { len = 1; return value; }
        value &= 0x7F;
        uint b = Unsafe.Add(ref src, 1);
        value |= (b & 0x7F) << 7;
        if ((b & 0x80) == 0) { len = 2; return value; }
        b = Unsafe.Add(ref src, 2);
        value |= (b & 0x7F) << 14;
        if ((b & 0x80) == 0) { len = 3; return value; }
        b = Unsafe.Add(ref src, 3);
        value |= (b & 0x7F) << 21;
        if ((b & 0x80) == 0) { len = 4; return value; }
        b = Unsafe.Add(ref src, 4);
        value |= b << 28;
        len = 5;
        return value;
    }

    /// <summary>
    /// First-byte early exit (the dominant case in real schemas), then one u32 load covers the
    /// remaining 1-4 bytes exactly: 1 + 4 = the u32 maximum.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint EarlyExit1Then4(ref byte src, out int len)
    {
        uint b0 = src;
        if ((b0 & 0x80) == 0) { len = 1; return b0; }

        uint tail = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, 1));
        int tz = BitOperations.TrailingZeroCount(~tail & 0x80808080u); // 7/15/23/31 for valid data
        int tailBytes = (tz >> 3) + 1;
        len = 1 + tailBytes;
        tail = ZeroHigh32(tail, tailBytes << 3);
        uint compact = (tail & 0x7F)
            | ((tail >> 1) & (0x7Fu << 7))
            | ((tail >> 2) & (0x7Fu << 14))
            | ((tail >> 3) & (0x7Fu << 21));
        return (b0 & 0x7F) | (compact << 7);
    }

    /// <summary>
    /// Two early exits - lengths 1 and 2 cover values to 16383, which is most tags and most
    /// lengths in real streams - then the branchless 8-load for the rest.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint EarlyExit2Then8(ref byte src, out int len)
    {
        uint b0 = src;
        if ((b0 & 0x80) == 0) { len = 1; return b0; }
        uint b1 = Unsafe.Add(ref src, 1);
        if ((b1 & 0x80) == 0) { len = 2; return (b0 & 0x7F) | (b1 << 7); }
        return Load8TzcntSwar(ref src, out len);
    }

    /// <summary>u32 load first (only 5 bytes of overread guarantee needed), 5th byte as spill.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Load4Then1(ref byte src, out int len)
    {
        uint head = Unsafe.ReadUnaligned<uint>(ref src);
        uint msbs = ~head & 0x80808080u;
        uint value = (head & 0x7F)
            | ((head >> 1) & (0x7Fu << 7))
            | ((head >> 2) & (0x7Fu << 14))
            | ((head >> 3) & (0x7Fu << 21));
        if (msbs != 0)
        {
            int lenBytes = (BitOperations.TrailingZeroCount(msbs) >> 3) + 1;
            len = lenBytes;
            return ZeroHigh32(value, lenBytes * 7);
        }
        len = 5;
        return value | ((uint)Unsafe.Add(ref src, 4) << 28);
    }

    /// <summary>One u64 load; tzcnt for the length; SWAR shift-and-mask compaction. Branchless.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Load8TzcntSwar(ref byte src, out int len)
    {
        ulong v = Unsafe.ReadUnaligned<ulong>(ref src);
        int tz = BitOperations.TrailingZeroCount(~v & 0x8080808080808080ul);
        int lenBytes = (tz >> 3) + 1;
        len = lenBytes;
        v = ZeroHigh64(v, lenBytes << 3);
        ulong r = (v & 0x7F)
            | ((v >> 1) & (0x7Ful << 7))
            | ((v >> 2) & (0x7Ful << 14))
            | ((v >> 3) & (0x7Ful << 21))
            | ((v >> 4) & (0x7Ful << 28));
        return (uint)r;
    }

    /// <summary>
    /// One u64 load; pext with a constant mask extracts every payload bit in one instruction, then
    /// bzhi trims to 7*len - no pre-masking needed. HAZARD: pext is microcoded on AMD Zen1/Zen2
    /// (catastrophically slow); this can never be the unguarded default.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Load8TzcntPext(ref byte src, out int len)
    {
        ulong v = Unsafe.ReadUnaligned<ulong>(ref src);
        int tz = BitOperations.TrailingZeroCount(~v & 0x8080808080808080ul);
        int lenBytes = (tz >> 3) + 1;
        len = lenBytes;
        if (Bmi2.X64.IsSupported)
        {
            return (uint)Bmi2.X64.ZeroHighBits(
                Bmi2.X64.ParallelBitExtract(v, 0x0000_007F_7F7F_7F7Ful), (ulong)(lenBytes * 7));
        }
        // fallback so the suite runs anywhere; the benchmark column is only meaningful with BMI2
        v = ZeroHigh64(v, lenBytes << 3);
        return (uint)((v & 0x7F) | ((v >> 1) & (0x7Ful << 7)) | ((v >> 2) & (0x7Ful << 14))
            | ((v >> 3) & (0x7Ful << 21)) | ((v >> 4) & (0x7Ful << 28)));
    }

    /// <summary>One u64 load; tzcnt for the length; jump table into straight-line extractors.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Load8Switch(ref byte src, out int len)
    {
        ulong v = Unsafe.ReadUnaligned<ulong>(ref src);
        int tz = BitOperations.TrailingZeroCount(~v & 0x8080808080808080ul);
        int lenBytes = (tz >> 3) + 1;
        len = lenBytes;
        return lenBytes switch
        {
            1 => (uint)(v & 0x7F),
            2 => (uint)((v & 0x7F) | ((v >> 1) & (0x7Ful << 7))),
            3 => (uint)((v & 0x7F) | ((v >> 1) & (0x7Ful << 7)) | ((v >> 2) & (0x7Ful << 14))),
            4 => (uint)((v & 0x7F) | ((v >> 1) & (0x7Ful << 7)) | ((v >> 2) & (0x7Ful << 14))
                | ((v >> 3) & (0x7Ful << 21))),
            _ => (uint)((v & 0x7F) | ((v >> 1) & (0x7Ful << 7)) | ((v >> 2) & (0x7Ful << 14))
                | ((v >> 3) & (0x7Ful << 21)) | (((v >> 32) & 0x7Ful) << 28)),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZeroHigh32(uint value, int keepBits)
        => Bmi2.IsSupported ? Bmi2.ZeroHighBits(value, (uint)keepBits) : value & ((1u << keepBits) - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ZeroHigh64(ulong value, int keepBits)
        => Bmi2.X64.IsSupported ? Bmi2.X64.ZeroHighBits(value, (ulong)keepBits) : value & ((1ul << keepBits) - 1);

    // ---------------------------------------------------------------- benchmarks

    // each benchmark carries its own direct-call loop, deliberately: a shared delegate-taking
    // runner (as the validators use) would add dispatch cost to every column, and "it poisons
    // them all equally" is an assumption - this file exists to avoid assumptions. The decode is
    // inlined into each loop; the serial offset chain is realistic, since parsing is serial.

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public uint DecodeByteLoop()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += ByteLoop(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint DecodeByteUnrolled()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += ByteUnrolled(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint DecodeEarlyExit1Then4()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += EarlyExit1Then4(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint DecodeEarlyExit2Then8()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += EarlyExit2Then8(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint DecodeLoad4Then1()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += Load4Then1(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint DecodeLoad8TzcntSwar()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += Load8TzcntSwar(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint DecodeLoad8TzcntPext()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += Load8TzcntPext(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint DecodeLoad8Switch()
    {
        uint sum = 0;
        int offset = 0;
        ref byte root = ref MemoryMarshal.GetArrayDataReference(_data);
        for (int i = 0; i < Count; i++)
        {
            sum += Load8Switch(ref Unsafe.Add(ref root, offset), out int len);
            offset += len;
        }
        return sum;
    }
}

#endif