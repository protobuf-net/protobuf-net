using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
//#if INTRINSICS
//            new VarintDecodeBenchmarks().Setup();
//#endif
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
