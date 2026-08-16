using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using ReaderState = ProtoBuf.ProtoReader.State;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// Packed scalars: 64K int32 elements as "field 1: packed run of K", both encodings - the
/// strategy race Marc asked for. The interesting questions: does the fixed32 bulk arm
/// (SetCount + block copy) deliver, and is the varint terminator pre-scan (exact count from
/// counting high-bit-clear bytes, then SetCount + span fill) worth its second pass over the
/// bytes versus EnsureCapacity or a plain Add loop? descriptor.proto has no fixed-size repeated
/// integers (and our payload has no SourceCodeInfo), so this is necessarily synthetic.
///
/// Lists are pre-sized and Cleared per parse, so growth-realloc is OFF the table: the measured
/// deltas are per-element machinery only, making this the FLOOR of the bulk arms' advantage -
/// a cold list adds growth costs that SetCount/EnsureCapacity avoid entirely.
///
/// GlobalSetup is the correctness gate: every strategy must agree on (count, sum, last).
/// </summary>
[MemoryDiagnoser]
public class PackedParseBenchmarks
{
    /// <summary>Wire encoding of the packed elements.</summary>
    [Params("varint", "fixed32")]
    public string Encoding = "varint";

    /// <summary>Elements per packed run.</summary>
    [Params(4, 64)]
    public int RunSize = 4;

    private const int ElementCount = 65536;
    private byte[] _data = [];
    private List<int> _values = [];
    private (int, long, int) _expected;

    private static readonly RepeatedSerializer<List<int>, int> s_legacyList
        = RepeatedSerializer.CreateList<int>();

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(12345);
        var payload = new System.IO.MemoryStream();
        long sum = 0;
        int last = 0;
        bool fixed32 = Encoding == "fixed32";
        int runs = ElementCount / RunSize;
        var elems = new byte[RunSize * 4];
        for (int r = 0; r < runs; r++)
        {
            int len = 0;
            for (int i = 0; i < RunSize; i++)
            {
                // small values: 1-byte varints, the path/span-like case
                int v = rng.Next(0, fixed32 ? int.MaxValue : 128);
                sum += v;
                last = v;
                if (fixed32)
                {
                    elems[len++] = (byte)v;
                    elems[len++] = (byte)(v >> 8);
                    elems[len++] = (byte)(v >> 16);
                    elems[len++] = (byte)(v >> 24);
                }
                else
                {
                    elems[len++] = (byte)v; // < 0x80 by construction
                }
            }
            payload.WriteByte((1 << 3) | 2); // field 1, length-prefixed
            uint l = (uint)len;
            while (l >= 0x80)
            {
                payload.WriteByte((byte)(l | 0x80));
                l >>= 7;
            }
            payload.WriteByte((byte)l);
            payload.Write(elems, 0, len);
        }
        _data = payload.ToArray();
        _values = new List<int>(ElementCount);
        _expected = (ElementCount, sum, last);

        Check(ParseLegacyReal(), nameof(LegacyReal));
        Check(ParseNanoAddLoop(), nameof(NanoAddLoop));
        Check(ParseNanoHelper(), nameof(NanoHelper));
        Check(ParseNanoEnsureCapacity(), nameof(NanoEnsureCapacity));

        void Check((int, long, int) actual, string name)
        {
            if (actual != _expected)
            {
                throw new InvalidOperationException($"{name} disagreement: {actual} vs expected {_expected}");
            }
        }
    }

    // the real legacy stack: the same RepeatedSerializer + FillBuffer path generated/runtime
    // code uses, consuming packed and unpacked occurrences alike
    [Benchmark(Baseline = true, OperationsPerInvoke = ElementCount)]
    public (int, long, int) LegacyReal() => ParseLegacyReal();

    private (int, long, int) ParseLegacyReal()
    {
        var features = Encoding == "fixed32"
            ? SerializerFeatures.WireTypeFixed32
            : SerializerFeatures.WireTypeVarint;
        var state = ProtoReader.State.Create(new ReadOnlyMemory<byte>(_data), model: null);
        try
        {
            var values = _values;
            values.Clear();
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                if (field == 1) values = s_legacyList.ReadRepeated(ref state, features, values);
                else state.SkipField();
            }
            return Tally(values);
        }
        finally
        {
            state.Dispose();
        }
    }

    // the pre-helper emitted shape: scope push, per-element Add, scope pop
    [Benchmark(OperationsPerInvoke = ElementCount)]
    public (int, long, int) NanoAddLoop() => ParseNanoAddLoop();

    private (int, long, int) ParseNanoAddLoop()
    {
        bool fixed32 = Encoding == "fixed32";
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            var values = _values;
            values.Clear();
            uint tag;
            while ((tag = state.ReadRawTag()) != 0)
            {
                switch (tag)
                {
                    case (1 << 3) | 2:
                    {
                        var scope = state.PushLengthPrefix();
                        if (fixed32)
                        {
                            while (!state.AtScopeEnd) values.Add(unchecked((int)state.ReadRawFixed32()));
                        }
                        else
                        {
                            while (!state.AtScopeEnd) values.Add(unchecked((int)state.ReadRawVarint32()));
                        }
                        state.PopScope(scope);
                        break;
                    }
                    default:
                        state.SkipTag(tag);
                        break;
                }
            }
            return Tally(values);
        }
        finally
        {
            state.Dispose();
        }
    }

    // the library helpers: net8+ takes the bulk arms (fixed32 = SetCount + block copy; varint =
    // terminator pre-scan + SetCount + span fill); down-level takes the scope-free Add loop
    [Benchmark(OperationsPerInvoke = ElementCount)]
    public (int, long, int) NanoHelper() => ParseNanoHelper();

    private (int, long, int) ParseNanoHelper()
    {
        bool fixed32 = Encoding == "fixed32";
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            var values = _values;
            values.Clear();
            uint tag;
            while ((tag = state.ReadRawTag()) != 0)
            {
                switch (tag)
                {
                    case (1 << 3) | 2:
                        if (fixed32) state.ReadPackedFixed32(values);
                        else state.ReadPackedVarint32(values);
                        break;
                    default:
                        state.SkipTag(tag);
                        break;
                }
            }
            return Tally(values);
        }
        finally
        {
            state.Dispose();
        }
    }

    // the middle strategy: no second pass - EnsureCapacity from the byte length (exact for
    // fixed32, an upper bound for varint) then the plain Add loop. The method exists on BOTH
    // TFMs because BDN discovers benchmarks on the host TFM and generates every runtime
    // partition with the same method set - an #if-gated [Benchmark] fails the down-level
    // partition's build. Only the BODY forks: on net472 (no EnsureCapacity) this row
    // deliberately degenerates to the Add loop, and the results note it as such.
    [Benchmark(OperationsPerInvoke = ElementCount)]
    public (int, long, int) NanoEnsureCapacity() => ParseNanoEnsureCapacity();

    private (int, long, int) ParseNanoEnsureCapacity()
    {
        bool fixed32 = Encoding == "fixed32";
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            var values = _values;
            values.Clear();
            uint tag;
            while ((tag = state.ReadRawTag()) != 0)
            {
                switch (tag)
                {
                    case (1 << 3) | 2:
                    {
                        var scope = state.PushLengthPrefix();
                        // Position-derived remaining bytes: len is inside the scope machinery,
                        // so approximate via capacity growth per run - the benchmark-side
                        // stand-in for a library arm that would know len directly
                        if (fixed32)
                        {
#if NET6_0_OR_GREATER
                            values.EnsureCapacity(values.Count + RunSize);
#endif
                            while (!state.AtScopeEnd) values.Add(unchecked((int)state.ReadRawFixed32()));
                        }
                        else
                        {
#if NET6_0_OR_GREATER
                            values.EnsureCapacity(values.Count + RunSize);
#endif
                            while (!state.AtScopeEnd) values.Add(unchecked((int)state.ReadRawVarint32()));
                        }
                        state.PopScope(scope);
                        break;
                    }
                    default:
                        state.SkipTag(tag);
                        break;
                }
            }
            return Tally(values);
        }
        finally
        {
            state.Dispose();
        }
    }

    private static (int, long, int) Tally(List<int> values)
    {
        long sum = 0;
        int last = 0;
        foreach (var v in values)
        {
            sum += v;
            last = v;
        }
        return (values.Count, sum, last);
    }
}
