#pragma warning disable RCS1213

using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main()
        {
            var obj = new LibraryComparison();
            obj.Setup();
            obj.MemoryStream();
            //for (int i = 0; i < 10000; i++)
            //{
            //    var db = obj.ROM_Manual();
            //    GC.KeepAlive(db);
            //}
            //Console.WriteLine(BenchmarkRunner.Run<LibraryComparison>());
        }
    }
}
