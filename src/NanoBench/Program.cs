using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(ProtoBuf.Nano.Bench.VarintU32DecodeBenchmarks).Assembly).Run(args);
