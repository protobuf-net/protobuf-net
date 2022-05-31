#define RUN
using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
#if (DEBUG || RUN) && NEW_API
            var obj = new Nano.NanoBenchmarks();
            obj.Setup();
            for (int i = 0 ; i < 50000 ; i++)
            {
                if ((i % 1000) == 0) Console.Write(".");
                obj.DeserializeRequestNanoSlab();
            }
#else

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
        }
    }
}
