using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ProtoBuf.Nano;
using System;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// The next generality case: sub-messages, in both termination modes - length-prefixed
/// ("field 1: len, [field 1: varint]") and group-encoded ("start-group, [field 1: varint],
/// end-group") - with deliberate MERGE semantics: the same child instance is reused across all
/// 64K records (a repeated message field merges into the existing value), so the loop measures
/// parsing rather than allocation.
///
/// Two-way only, recorded honestly: the veneer row cannot participate yet, because legacy
/// consumer code frames sub-messages via StartSubItem/EndSubItem returning SubItemToken - whose
/// constructor is internal to Core, so the spike cannot mint one. That evaporates when nano lands
/// inside Core; noted in the results file as an integration point for the swap plan.
///
/// GlobalSetup is the correctness gate: both parsers must agree on (count, sum, last) against
/// expected before any measurement.
/// </summary>
public class SubMessageParseBenchmarks
{
    public sealed class Child
    {
        public int Value;
    }

    [Params("prefixed", "group")]
    public string Encoding = "prefixed";

    /// <summary>small = 1-byte values only; mixed = 1-5 byte spread.</summary>
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
        var inner = new System.IO.MemoryStream();
        _expectedCount = Count;
        for (int i = 0; i < Count; i++)
        {
            uint v = Distribution == "small"
                ? (uint)rng.Next(0, 128)
                : (uint)rng.NextInt64(0, uint.MaxValue);
            unchecked { _expectedSum += v; }
            _expectedLast = unchecked((int)v);

            inner.SetLength(0);
            inner.WriteByte(0x08); // child field 1, varint
            while (v >= 0x80)
            {
                inner.WriteByte((byte)(v | 0x80));
                v >>= 7;
            }
            inner.WriteByte((byte)v);

            if (Encoding == "prefixed")
            {
                payload.WriteByte(0x0A); // parent field 1, length-prefixed
                payload.WriteByte(checked((byte)inner.Length)); // always < 128 here
                inner.WriteTo(payload);
            }
            else
            {
                payload.WriteByte(0x0B); // parent field 1, start-group
                inner.WriteTo(payload);
                payload.WriteByte(0x0C); // end-group
            }
        }
        _data = payload.ToArray();

        var legacy = ParseLegacyReal();
        var raw = ParseNanoRaw();
        if (legacy != raw || legacy != (_expectedCount, _expectedSum, _expectedLast))
        {
            throw new InvalidOperationException(
                $"disagreement: legacy {legacy}, raw {raw}, expected ({_expectedCount}, {_expectedSum}, {_expectedLast})");
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public (int, uint, int) LegacyReal() => ParseLegacyReal();

    private (int, uint, int) ParseLegacyReal()
    {
        var state = ProtoReader.State.Create(new ReadOnlyMemory<byte>(_data), model: null);
        try
        {
            var child = new Child();
            int count = 0;
            uint sum = 0;
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1: // both wire forms arrive here; StartSubItem handles either framing
                        var token = state.StartSubItem();
                        int innerField;
                        while ((innerField = state.ReadFieldHeader()) > 0)
                        {
                            switch (innerField)
                            {
                                case 1:
                                    child.Value = state.ReadInt32();
                                    break;
                                default:
                                    state.SkipField();
                                    break;
                            }
                        }
                        state.EndSubItem(token);
                        unchecked { sum += (uint)child.Value; }
                        count++;
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return (count, sum, child.Value);
        }
        finally
        {
            state.Dispose();
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public (int, uint, int) NanoRaw() => ParseNanoRaw();

    private (int, uint, int) ParseNanoRaw()
    {
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            var child = new Child();
            int count = 0;
            uint sum = 0;
            uint tag;
            while ((tag = state.ReadRawTag()) != 0)
            {
                switch (tag)
                {
                    case (1 << 3) | 2: // length-prefixed child
                    {
                        var scope = state.PushLengthPrefix();
                        child = NanoReadChild(ref state, child);
                        state.PopScope(scope);
                        unchecked { sum += (uint)child.Value; }
                        count++;
                        break;
                    }
                    case (1 << 3) | 3: // group child; the end tag is a compile-time constant here
                    {
                        var scope = state.PushGroup((1 << 3) | 4);
                        child = NanoReadChild(ref state, child);
                        state.PopScope(scope);
                        unchecked { sum += (uint)child.Value; }
                        count++;
                        break;
                    }
                    default:
                        state.SkipTag(tag);
                        break;
                }
            }
            return (count, sum, child.Value);
        }
        finally
        {
            state.Dispose();
        }
    }

    // the exact shape the generator's nano pass emits (see NanoPass.output.cs): value in, value
    // returned, ??= construction inside, merge by mutation, sentinel checked only in the default
    // case - and the call site assigns back, as generated code would
    private static Child NanoReadChild(ref ReaderState state, Child value)
    {
        value ??= new Child();
        uint tag;
        while ((tag = state.ReadRawTag()) != 0)
        {
            switch (tag)
            {
                case (1 << 3) | 0:
                    value.Value = unchecked((int)state.ReadRawVarint32());
                    break;
                default:
                    if (state.IsScopeEnd(tag)) return value;
                    state.SkipTag(tag);
                    break;
            }
        }
        return value;
    }
}
