#pragma warning disable RCS1213

using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main()
        {
            Console.WriteLine(BenchmarkRunner.Run<SpanPerformance>());
        }
    }
}
