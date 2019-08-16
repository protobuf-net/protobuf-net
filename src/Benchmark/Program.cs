#pragma warning disable RCS1213

using BenchmarkDotNet.Running;
using ProtoBuf;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
