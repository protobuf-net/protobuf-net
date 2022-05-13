using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Google.Protobuf;
using Hyper;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser]
    public class BufferStrategyBenchmarks
    {
        private readonly byte[] _payloadBytes;
        private readonly ReadOnlyMemory<byte> _payloadROM;
        public BufferStrategyBenchmarks()
        {
            // generate a reliable known
            var rand = new Random(12345);
            var value = new byte[1024];
            rand.NextBytes(value);
            var template = new BytesTest
            {
                Value = ByteString.CopyFrom(value)
            };

            // serialize
            _payloadBytes = new byte[template.CalculateSize()];
            template.WriteTo(_payloadBytes);
            _payloadROM = _payloadBytes;

            var obj = new BytesTest();
            obj.MergeFrom(_payloadBytes);
            Assert(obj.Value.Memory, value);

            // now also deserialize with protobuf-net and prove same
            Assert((Serializer.Deserialize<SimpleBytes>(_payloadROM)).Value, value);

            using var pooled = Serializer.Deserialize<PooledBytes>(_payloadROM);
            Assert(pooled.Value, value);
        }

        static void Assert(ReadOnlyMemory<byte> actual, ReadOnlyMemory<byte> expected)
        {
            if (!actual.Span.SequenceEqual(expected.Span))
                throw new InvalidOperationException("Data mismatch");
        }

        [Benchmark(Baseline = true)]
        public void GoogleProtobuf()
        {
            var obj = new BytesTest();
            obj.MergeFrom(_payloadBytes);
        }

        [Benchmark]
        public void ProtobufNetByteArray()
        {
            Serializer.Deserialize<SimpleBytes>(_payloadROM);
        }

        [Benchmark]
        public void ProtobufNetPooledMemory()
        {
            var obj = Serializer.Deserialize<PooledBytes>(_payloadROM);
            obj.Dispose();
        }

        [ProtoContract]
        public class SimpleBytes
        {
            [ProtoMember(1)]
            public byte[] Value { get; set; }
        }

        [ProtoContract]
        public class PooledBytes : IDisposable
        {
            [ProtoMember(1)]
            public PooledMemory<byte> Value { get; set; }

            public void Dispose()
            {
                var tmp = Value;
                Value = default;
                tmp.Dispose();
            }
        }
    }
}
