using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;

#if NEW_API
namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser]
    public class SpanPerformance
    {
        private MemoryStream _ms;
        private ReadOnlySequence<byte> _ros;
        public ProtoReader.State ReadMS()
            => ProtoReader.State.Create(_ms, Model);

        public ProtoReader.State ReadROS()
            => ProtoReader.State.Create(_ros, Model);

        public ProtoReader.State ReadROM()
        {
            if (!_ros.IsSingleSegment) throw new InvalidOperationException("Expected single segment");
            return ProtoReader.State.Create(_ros.First, Model);
        }

        public TypeModel Model => RuntimeTypeModel.Default;

        [GlobalSetup]
        public void Setup()
        {
            var data = File.ReadAllBytes("nwind.proto.bin");
            _ms = new MemoryStream(data);
            _ros = new ReadOnlySequence<byte>(data);
        }

        [Benchmark(Baseline = true)]
        public void MemoryStream()
        {
            _ms.Position = 0;
            using var reader = ReadMS();
            var dal = reader.DeserializeRoot<protogen.Database>();
            GC.KeepAlive(dal);
        }

        [Benchmark]
        public void ReadOnlySequence()
        {
            using var reader = ReadROS();
            var dal = reader.DeserializeRoot<protogen.Database>();
            GC.KeepAlive(dal);
        }
        [Benchmark]
        public void ReadOnlyMemory()
        {
            using var reader = ReadROM();
            var dal = reader.DeserializeRoot<protogen.Database>();
            GC.KeepAlive(dal);
        }
    }
}
#endif