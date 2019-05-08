#pragma warning disable RCS1213

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace BenchmarkBaseline
{
    public static class Program
    {
        private static void Main()
        {
            Console.WriteLine(BenchmarkRunner.Run<Benchmarks>());
        }
    }

    [ClrJob, CoreJob, MemoryDiagnoser]
    public class Benchmarks
    {
        private MemoryStream _ms;

        public ProtoReader ReadMS()
            => ProtoReader.Create(_ms, Model);

        public TypeModel Model => RuntimeTypeModel.Default;

        [GlobalSetup]
        public void Setup()
        {
            var data = File.ReadAllBytes("test.bin");
            _ms = new MemoryStream(data);
        }

        [Benchmark(Baseline = true)]
        public void MemoryStream()
        {
            _ms.Position = 0;
            using (var reader = ReadMS())
            {
                var dal = Model.Deserialize(reader, null, typeof(protogen.Database));
                GC.KeepAlive(dal);
            }
        }
    }
}
