using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
//#if INTRINSICS
//            var obj = new SerializeBenchmarks();
//            obj.Setup();
//            obj.BufferWriter_CIP();
//#endif
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
