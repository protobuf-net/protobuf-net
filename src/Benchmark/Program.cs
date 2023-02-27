// #define RUN
namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
#if (DEBUG || RUN) && NEW_API
            var obj = new Nano.EncodeIntrinsicBenchmarks();
            for (int offset = 0; offset < 8; offset++)
            {
                obj.ByteOffset = offset;
                for (int length = 1; length <= 5; length++)
                {
                    System.Console.WriteLine($"offset: {offset}; length: {length}");
                    obj.VarintLen = length;

                    System.Console.WriteLine(obj.Unoptimized());
                    System.Console.WriteLine(obj.Intrinsic());
                    System.Console.WriteLine(obj.WithZeroHighBits());
                    System.Console.WriteLine(obj.WithZeroHighBits2());
                    System.Console.WriteLine(obj.ShiftedMasks());
                }
            }
#else
            BenchmarkDotNet.Running.BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
        }
    }
}
