using BenchmarkDotNet.Running;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
//#if INTRINSICS
//            var obj = new VarintDecodeBenchmarks();
//            obj.Setup();
//#endif
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
