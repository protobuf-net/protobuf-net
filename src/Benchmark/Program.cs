using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            //var obj = new ByteHashBenchmarks();
            //obj.Length = 1024;
            //obj.Setup();
            //obj.IntrVec();

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
