using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Google.Protobuf;
using Hyper;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.Net60), MemoryDiagnoser]
    public class BufferStrategyBenchmarks
    {
        static BufferStrategyBenchmarks()
        {
            RuntimeTypeModel.Default.Add<PooledBytes>().SetFactory(nameof(PooledBytes.Create));
        }
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

            // test deserialize
            var obj = new BytesTest();
            obj.MergeFrom(_payloadBytes);
            Assert(obj.Value.Memory, value);

            // now also deserialize with protobuf-net and prove same
            Assert((Serializer.Deserialize<SimpleBytes>(_payloadROM)).Value, value);

            using var pooled = Serializer.Deserialize<PooledBytes>(_payloadROM);
            Assert(pooled.Value, value);

            using var pooledCustom = Serializer.Deserialize<CustomPooledBytes>(_payloadROM);
            Assert(pooledCustom.Value, value);
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

        [Benchmark]
        public void ProtobufNetCustomPooledMemory()
        {
            var obj = Serializer.Deserialize<CustomPooledBytes>(_payloadROM);
            obj.Dispose();
        }

        [ProtoContract]
        public sealed class SimpleBytes
        {
            [ProtoMember(1)]
            public byte[] Value { get; set; }
        }

        [ProtoContract]
        public sealed class PooledBytes : IDisposable
        {
            [ProtoMember(1)]
            public Memory<byte> Value { get; set; }

            public void Dispose()
            {
                var tmp = Value;
                Value = default;
                // tmp.Dispose();

                s_Spare = this;
            }

            [ThreadStatic]
            private static PooledBytes s_Spare;

            public static PooledBytes Create()
            {
                var obj = s_Spare ?? new PooledBytes();
                s_Spare = null;
                return obj;
            }
        }

        [ProtoContract(Serializer = typeof(CustomPooledBytesSerializer))]
        public sealed class CustomPooledBytes : IDisposable
        {
            [ProtoMember(1)]
            public Memory<byte> Value { get; set; }

            public void Dispose()
            {
                var tmp = Value;
                Value = default;
                // tmp.Dispose();

                s_Spare = this;
            }

            [ThreadStatic]
            private static CustomPooledBytes s_Spare;

            private static CustomPooledBytes Create()
            {
                var obj = s_Spare ?? new CustomPooledBytes();
                s_Spare = null;
                return obj;
            }

            private sealed class CustomPooledBytesSerializer : ISerializer<CustomPooledBytes>
            {
                SerializerFeatures ISerializer<CustomPooledBytes>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

                CustomPooledBytes ISerializer<CustomPooledBytes>.Read(ref ProtoReader.State state, CustomPooledBytes value)
                {
                    value ??= Create();
                    int field;
                    while ((field = state.ReadFieldHeader()) > 0)
                    {
                        switch (field)
                        {
                            case 1:
                                value.Value = state.AppendBytes(value.Value);
                                break;
                            default:
                                state.SkipField();
                                break;
                        }
                    }
                    return value;
                }

                void ISerializer<CustomPooledBytes>.Write(ref ProtoWriter.State state, CustomPooledBytes value)
                {
                    if (!value.Value.IsEmpty)
                    {
                        state.WriteFieldHeader(1, WireType.String);
                        state.WriteBytes(value.Value);
                    }
                }
            }
        }
    }
}
