using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(ProtoBuf.Nano.Bench.FieldParseBenchmarks).Assembly).Run(args);
