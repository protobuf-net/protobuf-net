using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Nano;
using System;
using System.Text;
using ReaderState = ProtoBuf.ProtoReader.State;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// Strings: 64K records of "field 1: string" (a duplicated string field REPLACES - confirmed
/// against both Google.Protobuf and legacy protobuf-net, see docs/nano-core.md). Memory is
/// diagnosed: the materialized string is the unavoidable allocation, and the interesting question
/// is whether either side allocates anything else.
///
/// GlobalSetup is the correctness gate: all three parsers must agree on (count, total chars,
/// last string) against expected before any measurement.
/// </summary>
[MemoryDiagnoser]
public class StringParseBenchmarks
{
    /// <summary>short = 0-16 chars (the common case); long = 0-200 chars.</summary>
    [Params("short", "long")]
    public string Length = "short";

    /// <summary>ascii = 1 byte/char; unicode = mixed multi-byte (the UTF-8 slow path).</summary>
    [Params("ascii", "unicode")]
    public string Charset = "ascii";

    private const int Count = 65536;
    private byte[] _data = [];
    private int _expectedCount;
    private long _expectedChars;
    private string _expectedLast = "";

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(12345);
        var payload = new System.IO.MemoryStream();
        _expectedCount = Count;
        _expectedChars = 0;
        var sb = new StringBuilder();
        for (int i = 0; i < Count; i++)
        {
            int chars = rng.Next(0, Length == "short" ? 17 : 201);
            sb.Clear();
            for (int c = 0; c < chars; c++)
            {
                sb.Append(Charset == "ascii"
                    ? (char)rng.Next(0x20, 0x7F)
                    : (char)rng.Next(0x20, 0x2FF)); // spans 1- and 2-byte UTF-8
            }
            var s = sb.ToString();
            _expectedChars += s.Length;
            _expectedLast = s;

            var utf8 = Encoding.UTF8.GetBytes(s);
            payload.WriteByte(0x0A); // field 1, length-prefixed
            uint len = (uint)utf8.Length;
            while (len >= 0x80)
            {
                payload.WriteByte((byte)(len | 0x80));
                len >>= 7;
            }
            payload.WriteByte((byte)len);
            payload.Write(utf8, 0, utf8.Length);
        }
        _data = payload.ToArray();

        var legacy = ParseLegacyReal();
        var shim = ParseNanoViaLegacyApi();
        var raw = ParseNanoRaw();
        if (legacy != shim || shim != raw
            || legacy != (_expectedCount, _expectedChars, _expectedLast))
        {
            throw new InvalidOperationException(
                $"disagreement: legacy {legacy}, shim {shim}, raw {raw}, expected ({_expectedCount}, {_expectedChars}, {_expectedLast})");
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public (int, long, string) LegacyReal() => ParseLegacyReal();

    private (int, long, string) ParseLegacyReal()
    {
        var state = ProtoReader.State.Create(new ReadOnlyMemory<byte>(_data), model: null);
        try
        {
            int count = 0;
            long chars = 0;
            string last = "";
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        last = state.ReadString();
                        chars += last.Length;
                        count++;
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return (count, chars, last);
        }
        finally
        {
            state.Dispose();
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public (int, long, string) NanoViaLegacyApi() => ParseNanoViaLegacyApi();

    private (int, long, string) ParseNanoViaLegacyApi()
    {
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            int count = 0;
            long chars = 0;
            string last = "";
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        last = state.ReadString();
                        chars += last.Length;
                        count++;
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return (count, chars, last);
        }
        finally
        {
            state.Dispose();
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public (int, long, string) NanoRaw() => ParseNanoRaw();

    private (int, long, string) ParseNanoRaw()
    {
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            int count = 0;
            long chars = 0;
            string last = "";
            uint tag;
            while ((tag = state.ReadRawTag()) != 0)
            {
                switch (tag)
                {
                    case (1 << 3) | 2:
                    {
                        var tmp = state.ReadRawString();
                        if (tmp != null) last = tmp; // the emitted null-guard shape
                        chars += last.Length;
                        count++;
                        break;
                    }
                    default:
                        state.SkipTag(tag);
                        break;
                }
            }
            return (count, chars, last);
        }
        finally
        {
            state.Dispose();
        }
    }
}
