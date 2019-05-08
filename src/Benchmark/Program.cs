#pragma warning disable RCS1213

using BenchmarkDotNet.Running;
using ProtoBuf;
using System;

namespace Benchmark
{
    public static class Program
    {
        private static void Main()
        {
            var obj = new LibraryComparison();
            obj.Setup();
            var dal = obj.MemoryStream_AUTO();
            using (var ms = new System.IO.MemoryStream())
            {
                using (var writer = ProtoWriter.Create(out var state, ms, obj.Auto))
                {
                    obj.Auto.Serialize(writer, ref state, dal);
                    writer.Flush(ref state);
                }
                obj.Verify(ms.GetBuffer(), (int)ms.Length);
            }
            //for (int i = 0; i < 10000; i++)
            //{
            //    var db = obj.ROM_Manual();
            //    GC.KeepAlive(db);
            //}
            Console.WriteLine(BenchmarkRunner.Run<LibraryComparison>());
        }
    }
}
