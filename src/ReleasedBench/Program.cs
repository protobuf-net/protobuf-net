using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Google.Protobuf.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Linq;

BenchmarkSwitcher.FromAssembly(typeof(ReleasedDescriptorBenchmarks).Assembly).Run(args);

/// <summary>
/// protobuf-net as shipped on NuGet, on the same payload the v4 benchmarks use — the "what you
/// get today" column of the table in <c>docs/aot.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// The point of this project is the comparison a consumer actually cares about: not "v4 runtime
/// versus v4 generated", which understates things because the v4 runtime path already carries the
/// whole writer arc, but "the package I have installed versus what v4 offers".
/// </para>
/// <para>
/// The payload is built exactly as <c>DescriptorParseBenchmarks.BuildPayload</c> builds it — the
/// embedded <c>descriptor.proto</c>, parsed and serialized — so the two processes are measuring
/// the same bytes. The setup asserts the length is plausible for that reason; a silently
/// different payload would make the whole table meaningless.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class ReleasedDescriptorBenchmarks
{
    private byte[] _data = [];
    private FileDescriptorSet _set = null!;
    private MemoryStream _ms = null!;

    [GlobalSetup]
    public void Setup()
    {
        var set = new FileDescriptorSet();
        if (!set.Add("google/protobuf/descriptor.proto", includeInOutput: true))
        {
            throw new InvalidOperationException("could not resolve the embedded descriptor.proto");
        }
        set.Process();
        var errors = set.GetErrors();
        if (errors.Any(e => e.IsError))
        {
            throw new InvalidOperationException("parse errors: "
                + string.Join("; ", errors.Where(e => e.IsError).Select(e => e.Message)));
        }

        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, set);
            _data = ms.ToArray();
        }
        if (_data.Length < 1024)
        {
            throw new InvalidOperationException($"implausibly small payload: {_data.Length} bytes");
        }

        _set = Serializer.Deserialize<FileDescriptorSet>(new MemoryStream(_data));
        _ms = new MemoryStream(_data.Length + 1024);

        // the gate that makes the cross-process comparison meaningful: this must be the same
        // payload the v4 benchmarks measure. Reported rather than asserted against a hard-coded
        // number, since the embedded schema may legitimately change
        Console.WriteLine($"// released-package payload: {_data.Length} bytes");
    }

    [Benchmark(Description = "serialize")]
    public object Serialize()
    {
        _ms.Position = 0;
        Serializer.Serialize(_ms, _set);
        return _ms;
    }

    [Benchmark(Description = "deserialize")]
    public object Deserialize()
    {
        var source = new MemoryStream(_data);
        return Serializer.Deserialize<FileDescriptorSet>(source);
    }
}
