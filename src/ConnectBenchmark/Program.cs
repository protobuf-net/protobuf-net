using ProtoBuf;

if (Environment.GetEnvironmentVariable("DUMP_PROTO") == "1")
{
    Console.WriteLine(Serializer.GetProto<ProtoBuf.ConnectBenchmark.Payload>());
    return;
}

if (Environment.GetEnvironmentVariable("SIZES") == "1")
{
    var bench = new ProtoBuf.ConnectBenchmark.CodecBenchmarks();
    bench.Setup();
    Console.WriteLine(bench.Sizes);
    Console.WriteLine($"runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}, "
        + $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription}, "
        + $"{Environment.ProcessorCount} logical cores");
    return;
}

BenchmarkDotNet.Running.BenchmarkSwitcher
    .FromAssembly(typeof(ProtoBuf.ConnectBenchmark.Payload).Assembly)
    .Run(args);
