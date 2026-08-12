using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Nano;
using System;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// The first end-to-end three-way: a large payload of repeated "field 1, varint value" records
/// (a duplicated field means overwrite - last value wins), parsed by
///   (a) the genuine legacy reader (ProtoReader.State over the real protobuf-net.Core),
///   (b) the nano reader via the legacy veneer API (ReadFieldHeader/ReadInt32 shims), and
///   (c) the nano reader via the raw static API (ReadRawTag constants + ReadRawVarint32),
/// which is exactly the landing-strategy side-by-side: (a) vs (b) isolates the reader internals
/// under an identical API, (b) vs (c) isolates what the raw surface buys on top.
///
/// GlobalSetup is the correctness gate: all three must agree on (count, sum, last) before any
/// measurement. Values are non-repeating within the 64K-value stream, which also blunts the
/// branch-predictor memorization caveat from VarintU32DecodeResults.md.
/// </summary>
public class FieldParseBenchmarks
{
    /// <summary>small = 1-byte values only (the common case); mixed = 1-5 byte spread.</summary>
    [Params("small", "mixed")]
    public string Distribution = "small";

    private const int Count = 65536;
    private byte[] _data = [];
    private int _expectedCount;
    private uint _expectedSum;
    private int _expectedLast;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(12345);
        var payload = new System.IO.MemoryStream();
        _expectedCount = Count;
        for (int i = 0; i < Count; i++)
        {
            uint v = Distribution == "small"
                ? (uint)rng.Next(0, 128)
                : (uint)(rng.NextDouble() * uint.MaxValue);
            unchecked { _expectedSum += v; }
            _expectedLast = unchecked((int)v);
            payload.WriteByte(0x08); // field 1, varint
            while (v >= 0x80)
            {
                payload.WriteByte((byte)(v | 0x80));
                v >>= 7;
            }
            payload.WriteByte((byte)v);
        }
        _data = payload.ToArray();

        var legacy = ParseLegacyReal();
        var shim = ParseNanoViaLegacyApi();
        var raw = ParseNanoRaw();
        if (legacy != shim || shim != raw
            || legacy != (_expectedCount, _expectedSum, _expectedLast))
        {
            throw new InvalidOperationException(
                $"disagreement: legacy {legacy}, shim {shim}, raw {raw}, expected ({_expectedCount}, {_expectedSum}, {_expectedLast})");
        }
    }

    // (a) the genuine article: protobuf-net.Core's ProtoReader.State
    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public (int, uint, int) LegacyReal() => ParseLegacyReal();

    private (int, uint, int) ParseLegacyReal()
    {
        var state = ProtoReader.State.Create(new ReadOnlyMemory<byte>(_data), model: null);
        try
        {
            int count = 0, last = 0;
            uint sum = 0;
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        last = state.ReadInt32();
                        unchecked { sum += (uint)last; }
                        count++;
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return (count, sum, last);
        }
        finally
        {
            state.Dispose();
        }
    }

    // (b) the nano internals behind the legacy API shape - same consumer code as (a)
    [Benchmark(OperationsPerInvoke = Count)]
    public (int, uint, int) NanoViaLegacyApi() => ParseNanoViaLegacyApi();

    private (int, uint, int) ParseNanoViaLegacyApi()
    {
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            int count = 0, last = 0;
            uint sum = 0;
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        last = state.ReadInt32();
                        unchecked { sum += (uint)last; }
                        count++;
                        break;
                    default:
                        state.SkipTag((uint)((field << 3) | (int)state.WireType));
                        break;
                }
            }
            return (count, sum, last);
        }
        finally
        {
            state.Dispose();
        }
    }

    // (c) the raw surface: what the generator's nano pass emits (see NanoPass.output.cs)
    [Benchmark(OperationsPerInvoke = Count)]
    public (int, uint, int) NanoRaw() => ParseNanoRaw();

    private (int, uint, int) ParseNanoRaw()
    {
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            int count = 0, last = 0;
            uint sum = 0;
            uint tag;
            while ((tag = state.ReadRawTag()) != 0)
            {
                switch (tag)
                {
                    case (1 << 3) | 0:
                        last = unchecked((int)state.ReadRawVarint32());
                        unchecked { sum += (uint)last; }
                        count++;
                        break;
                    default:
                        state.SkipTag(tag);
                        break;
                }
            }
            return (count, sum, last);
        }
        finally
        {
            state.Dispose();
        }
    }
}
