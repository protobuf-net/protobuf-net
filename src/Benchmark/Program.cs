using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG && NEW_API
            var obj = new Nano.NanoBenchmarks();
            obj.Setup();
#else

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
        }
    }
}
