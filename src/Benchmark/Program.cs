using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            //var obj = new SerializeBenchmarks();
            //obj.Setup();

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }

//        static void Main()
//        {
//#if NEW_API
//            var obj = new SerializeBenchmarks();
//            obj.Setup();
//            for(int i = 0; i < 50; i++)
//            {
//                Console.WriteLine(i);
//                obj.FakeBufferWriter_C();
//            }
//#endif
//        }
    }
}
