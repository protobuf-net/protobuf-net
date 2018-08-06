#pragma warning disable RCS1213

using BenchmarkDotNet.Running;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main()
        {
            //var obj = new LibraryComparison();
            //obj.Setup();
            //for (int i = 0; i < 10000; i++)
            //{
            //    var db = obj.ProtobufNet_Manual();
            //}
            Console.WriteLine(BenchmarkRunner.Run<LibraryComparison>());
        }
    }
}
