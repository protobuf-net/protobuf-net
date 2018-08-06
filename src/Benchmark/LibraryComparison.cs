using BenchmarkDotNet.Attributes;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace Benchmark
{
    [ClrJob, CoreJob, MemoryDiagnoser]
    public class LibraryComparison
    {
        [Benchmark]
        public protoc.Database Google()
        {
            return protoc.Database.Parser.ParseFrom(_data);
        }

        private byte[] _data;

        [Benchmark(Description = "protobuf-net")]
        public protogen.Database ProtobufNet()
        {
            var model = RuntimeTypeModel.Default;
            using (var reader = ProtoBuf.ProtoReader.Create(out var state, new ReadOnlyMemory<byte>(_data), model))
            {
                return (protogen.Database)model.Deserialize(ref state, reader, null, typeof(protogen.Database));
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            _data = File.ReadAllBytes("test.bin");
        }
    }
}
