extern alias gpb;

using BenchmarkDotNet.Attributes;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using Model = ProtoBuf.Nano.Bench.DescriptorModel;
using ReaderState = ProtoBuf.ProtoReader.State;
using Pbn = Google.Protobuf.Reflection;          // protobuf-net.Reflection's DTOs (legacy row)
using Gpb = gpb::Google.Protobuf.Reflection;     // the Google.Protobuf package (home-turf row)
using GpbExt = gpb::Google.Protobuf.MessageExtensions;

namespace ProtoBuf.Nano.Bench;

/// <summary>
/// The serialize half of the north-star composite (notes/nano-writer.md): the same
/// self-describing descriptor payload, written five ways. LegacyReal is the incumbent
/// (RuntimeTypeModel over protobuf-net.Reflection's DTOs); GeneratedProtogen is the SAME
/// object graph through the generated measure-first model that now ships inside
/// protobuf-net.Reflection - the cleanest engine-swap comparison available, since both rows
/// traverse identical objects with identical guards; NanoGenerated is the generated model
/// over this project's attributed DTOs; NanoGeneratedMeasure is its Measure_ alone, which
/// prices the measure pass for the recompute-vs-lengthCache race; GoogleProtobuf is home
/// turf. All rows write into one reused MemoryStream, so the transport cost is identical.
///
/// GlobalSetup gates: the two Reflection-DTO rows must be BYTE-IDENTICAL to the original
/// payload; the bench-DTO and Google rows must census-match through a legacy reparse (their
/// DTO shapes may guard differently without being wrong); and Measure_ must equal the
/// generated write's length exactly - the measure-write agreement the whole writer arc
/// rests on, asserted here on a real composite document.
/// </summary>
[MemoryDiagnoser]
public class DescriptorSerializeBenchmarks
{
    private byte[] _data = [];
    private Pbn.FileDescriptorSet _pbnSet = null!;
    private Gpb.FileDescriptorSet _gpbSet = null!;
    private Model.FileDescriptorSet _nanoSet = null!;
    private MemoryStream _ms = null!;
    private Meta.TypeModel _protogenModel = null!;

    private delegate long GeneratedMeasure(Model.FileDescriptorSet value, int depth,
        System.Collections.Generic.Dictionary<object, long> lengths);
    private static readonly GeneratedMeasure s_generatedMeasure = ResolveMeasure();

    // reference identity, both TFMs (the BCL's ReferenceEqualityComparer is net5+ only)
    private sealed class RefComparer : System.Collections.Generic.IEqualityComparer<object>
    {
        internal static readonly RefComparer Instance = new();
        bool System.Collections.Generic.IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
        int System.Collections.Generic.IEqualityComparer<object>.GetHashCode(object obj)
            => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    private readonly System.Collections.Generic.Dictionary<object, long> _measureScratch = new(RefComparer.Instance);

    [GlobalSetup]
    public void Setup()
    {
        _data = DescriptorParseBenchmarks.BuildPayload();
        _ms = new MemoryStream(_data.Length + 1024);

        _pbnSet = Serializer.Deserialize<Pbn.FileDescriptorSet>(new ReadOnlyMemory<byte>(_data));
        _gpbSet = Gpb.FileDescriptorSet.Parser.ParseFrom(_data);
        var state = new ReaderState(_data, 0, _data.Length);
        try
        {
            _nanoSet = DescriptorParseBenchmarks.s_generatedRead(ref state, null);
        }
        finally
        {
            state.Dispose();
        }

        // the generated model protobuf-net.Reflection ships (CustomProtogenSerializer) is
        // internal to that assembly; the TypeModel base is public, so one reflective hop at
        // setup gives a direct virtual call on the measured path
        var protogenType = typeof(Pbn.FileDescriptorSet).Assembly
            .GetType("ProtoBuf.Reflection.Internal.CustomProtogenSerializer")
            ?? throw new InvalidOperationException("CustomProtogenSerializer not found");
        _protogenModel = (Meta.TypeModel)(protogenType
            .GetProperty("Instance")?.GetValue(null)
            ?? throw new InvalidOperationException("CustomProtogenSerializer.Instance not found"));

        // the UTF-8 floor's inputs: every string in the graph, collected once
        _strings = CollectStrings(_nanoSet);
        int widest = 0;
        long floorBytes = 0;
        foreach (var s in _strings)
        {
            var n = ProtoWriter.UTF8.GetByteCount(s);
            if (n > widest) widest = n;
            floorBytes += n;
        }
        _utf8Scratch = new byte[widest + 16];
        Console.WriteLine($"// utf8 floor: {_strings.Length} strings, {floorBytes} bytes "
            + $"({(100.0 * floorBytes / _data.Length):0.0}% of the payload)");

        // gates, before any measurement
        var legacy = Run(LegacyReal);
        if (!legacy.SequenceEqual(_data))
        {
            throw new InvalidOperationException("legacy serialize does not round-trip its own payload");
        }
        var protogen = Run(GeneratedProtogen);
        if (!protogen.SequenceEqual(_data))
        {
            throw new InvalidOperationException(
                $"generated protogen model output differs from legacy ({protogen.Length} vs {_data.Length} bytes)");
        }
        var nano = Run(NanoGenerated);
        CensusGate(nano, "nano-generated");
        Console.WriteLine($"// nano-generated output byte-identical to legacy: {nano.SequenceEqual(_data)}");
        var google = Run(GoogleProtobuf);
        CensusGate(google, "google");

        var measured = NanoGeneratedMeasure();
        if (measured != nano.Length)
        {
            throw new InvalidOperationException(
                $"Measure_ disagreement: measured {measured}, wrote {nano.Length} bytes");
        }
        Console.WriteLine($"// payload {_data.Length} bytes; measure agrees at {measured}");

        byte[] Run(Func<object> row)
        {
            row();
            _ms.SetLength(_ms.Position); // a shorter row must not inherit a longer row's tail
            return _ms.ToArray();
        }
    }

    private void CensusGate(byte[] payload, string name)
    {
        var reparsed = Serializer.Deserialize<Pbn.FileDescriptorSet>(new ReadOnlyMemory<byte>(payload));
        var census = DescriptorParseBenchmarks.CensusLegacy(reparsed);
        var expected = DescriptorParseBenchmarks.CensusLegacy(_pbnSet);
        if (census != expected)
        {
            throw new InvalidOperationException($"census disagreement ({name}):\n{census}\nvs\n{expected}");
        }
    }

    // each row rewinds the shared stream and returns a value so nothing is elided; the
    // payloads are the same length every time, so the stream never regrows

    [Benchmark(Baseline = true)]
    public object LegacyReal()
    {
        _ms.Position = 0;
        Serializer.Serialize(_ms, _pbnSet);
        return _ms;
    }

    [Benchmark]
    public object GeneratedProtogen()
    {
        _ms.Position = 0;
        _protogenModel.Serialize(_ms, _pbnSet);
        return _ms;
    }

    [Benchmark]
    public object NanoGenerated()
    {
        _ms.Position = 0;
        Model.NanoDescriptorModel.Instance.Serialize(_ms, _nanoSet);
        return _ms;
    }

    [Benchmark]
    public long NanoGeneratedMeasure()
    {
        // cleared per invoke: the row prices a cold measure INCLUDING cache population,
        // which is what a root serialize actually pays
        _measureScratch.Clear();
        return s_generatedMeasure(_nanoSet, 512, _measureScratch);
    }

    /// <summary>
    /// The runtime model asked for a length first, i.e. the gRPC shape on the incumbent engine.
    /// </summary>
    /// <remarks>
    /// The pair (this and <see cref="NanoGeneratedMeasured"/>) is what a gRPC-style transport
    /// actually costs: the frame header carries the length, so the payload has to be priced before
    /// a byte is written. Comparing them against the two plain-serialize rows shows what that
    /// requirement costs on each engine, which is not the same answer.
    /// </remarks>
    [Benchmark]
    public object LegacyRealMeasured()
    {
        _ms.Position = 0;
        var output = (IMeasuredProtoOutput<Stream>)Meta.RuntimeTypeModel.Default;
        using var measured = output.Measure(_pbnSet);
        output.Serialize(measured, _ms);
        return _ms;
    }

    /// <summary>The measured path, i.e. the one where the presized lease fires; see the
    /// buffer-writer sibling for the reasoning.</summary>
    [Benchmark]
    public object NanoGeneratedMeasured()
    {
        _ms.Position = 0;
        var output = (IMeasuredProtoOutput<Stream>)Model.NanoDescriptorModel.Instance;
        using var measured = output.Measure(_nanoSet);
        output.Serialize(measured, _ms);
        return _ms;
    }

    [Benchmark]
    public object GoogleProtobuf()
    {
        _ms.Position = 0;
        GpbExt.WriteTo(_gpbSet, _ms);
        return _ms;
    }

    /// <summary>
    /// The floor: UTF-8 encoding every string in the graph, and NOTHING else - no tags, no
    /// lengths, no traversal, no destination.
    /// </summary>
    /// <remarks>
    /// The payload census (DescriptorPayloadCensus.md) says 71.5% of this document's bytes are
    /// string payload, so the obvious question is whether the write rows are really just
    /// measuring <see cref="System.Text.Encoding.GetBytes(string, int, int, byte[], int)"/>.
    /// This row answers it: it is the irreducible part of any serializer's job for this
    /// document, and every write row above is bounded below by it.
    /// <para>
    /// Both halves are here on purpose - GetByteCount is what the MEASURE pass pays and GetBytes
    /// is what the WRITE pass pays, and a measure-first serializer pays both.
    /// </para>
    /// </remarks>
    [Benchmark]
    public long Utf8Floor()
    {
        long total = 0;
        var strings = _strings;
        var scratch = _utf8Scratch;
        for (int i = 0; i < strings.Length; i++)
        {
            var s = strings[i];
            total += ProtoWriter.UTF8.GetByteCount(s);
            total += ProtoWriter.UTF8.GetBytes(s, 0, s.Length, scratch, 0);
        }
        return total;
    }

    private string[] _strings = [];
    private byte[] _utf8Scratch = [];

    /// <summary>
    /// Every string reachable from the graph, in traversal order, collected ONCE in setup -
    /// reflection here costs nothing, since it never runs inside a benchmark.
    /// </summary>
    private static string[] CollectStrings(object root)
    {
        var found = new System.Collections.Generic.List<string>();
        var seen = new System.Collections.Generic.HashSet<object>(RefComparer.Instance);
        void Walk(object obj)
        {
            if (obj is null || !seen.Add(obj)) return;
            foreach (var member in obj.GetType().GetProperties(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (member.GetIndexParameters().Length != 0) continue;
                object value;
                try { value = member.GetValue(obj); }
                catch { continue; }
                switch (value)
                {
                    case null:
                        break;
                    case string s:
                        if (s.Length != 0) found.Add(s);
                        break;
                    case System.Collections.IEnumerable list when value is not string:
                        foreach (var item in list)
                        {
                            if (item is string es) { if (es.Length != 0) found.Add(es); }
                            else if (item is not null && !item.GetType().IsPrimitive) Walk(item);
                        }
                        break;
                    default:
                        if (!value.GetType().IsPrimitive && !value.GetType().IsEnum) Walk(value);
                        break;
                }
            }
        }
        Walk(root);
        return found.ToArray();
    }

    private static GeneratedMeasure ResolveMeasure()
    {
        foreach (var nested in typeof(Model.NanoDescriptorModel).GetNestedTypes(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        {
            foreach (var method in nested.GetMethods(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static))
            {
                if (method.Name.StartsWith("Measure_", StringComparison.Ordinal)
                    && method.GetParameters() is { Length: 3 } p
                    && p[0].ParameterType == typeof(Model.FileDescriptorSet))
                {
                    return (GeneratedMeasure)method.CreateDelegate(typeof(GeneratedMeasure));
                }
            }
        }
        throw new InvalidOperationException(
            "generated Measure_ for FileDescriptorSet not found - did the write pass emit?");
    }
}
