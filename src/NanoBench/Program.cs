using BenchmarkDotNet.Running;

// `--probe` runs the allocation diagnosis for gaps.md B24 instead of a benchmark; everything else
// is BenchmarkDotNet as before. A probe rather than a benchmark because the question is "where do
// 2272 bytes come from", which GC.GetAllocatedBytesForCurrentThread answers directly and a mean
// time does not.
if (args.Length > 0 && args[0] == "--probe")
{
    ProtoBuf.Nano.Bench.ObjectDispatchProbe.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(ProtoBuf.Nano.Bench.FieldParseBenchmarks).Assembly).Run(args);
