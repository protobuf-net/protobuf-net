using BenchmarkDotNet.Running;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
//#if NEW_API
//            var obj = new DeserializeBenchmarks();
//            obj.Setup();
//            obj.MemoryStream_New_C_Pooled();
//#endif
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
