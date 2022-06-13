// #define RUN
using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
#if (DEBUG || RUN) && NEW_API
            var obj = new Nano.DecodeIntrinsicBenchmarks();
            for (int offset = 0; offset < 8; offset++)
            {
                obj.ByteOffset = offset;
                for (int length = 1; length <= 10; length++)
                {
                    Console.WriteLine($"offset: {offset}; length: {length}");
                    obj.VarintLen = length;

                    Console.WriteLine(obj.Unoptimized());
                    Console.WriteLine(obj.UnsafeAdd());
                    Console.WriteLine(obj.Intrinsics());
                    Console.WriteLine(obj.IntrinsicsSwitched());
                    Console.WriteLine(obj.IntrinsicsPreferShort());
                    Console.WriteLine(obj.IntrinsicsPreferShort2());
                    Console.WriteLine(obj.IntrinsicsPreferShort3());
                }
            }
#else

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
            }
        }
}
