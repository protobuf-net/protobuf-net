using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.IO;

namespace Benchmark
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class MeasuringSerializerBenchmarks
    {
        [Params(1024, 64 * 1024, 128 *1024, 1024*1024, 64*1024*1024, 4*1024*1024*100)]
        public int PayloadSize { get; set; }

        private RuntimeTypeModel _model;
        private Stream _stream;

        [GlobalSetup]
        public void Setup()
        {
            _model = RuntimeTypeModel.Create();
            _model.Add<Stream>().SerializerType = typeof(StreamSerializer);


            var data = new byte[PayloadSize];
            new Random(42).NextBytes(data);
            _stream = new MemoryStream(data);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("MeasureAndSerialize")]
        public long MeasureAndSerialize_ISerializer()
        {
            StreamSerializer.trySkipWritingWhenMeasuring = false;

            using var measure = _model.Measure(_stream);
            using var ms = new MemoryStream();
            measure.Serialize(ms);

            return ms.Length;
        }

        [Benchmark]
        [BenchmarkCategory("MeasureAndSerialize")]
        public long MeasureAndSerialize_IMeasuringSerializer()
        {
            StreamSerializer.trySkipWritingWhenMeasuring = true;

            using var measure = _model.Measure(_stream);
            using var ms = new MemoryStream();
            measure.Serialize(ms);

            return ms.Length;
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("MeasureOnly")]
        public long MeasureOnly_ISerializer()
        {
            StreamSerializer.trySkipWritingWhenMeasuring = false;
            using var measure = _model.Measure(_stream);
            return measure.Length;
        }

        [Benchmark]
        [BenchmarkCategory("MeasureOnly")]
        public long MeasureOnly_IMeasuringSerializer()
        {
            StreamSerializer.trySkipWritingWhenMeasuring = true;
            using var measure = _model.Measure(_stream);
            return measure.Length;
        }


        public class StreamSerializer : IMeasuringSerializer<Stream>
        {
            public static bool trySkipWritingWhenMeasuring = false;

            private const int ChunkSize = 1024;

            public SerializerFeatures Features
            {
                get
                {
                    if (trySkipWritingWhenMeasuring)
                    {
                        return SerializerFeatures.CategoryMessage
                               | SerializerFeatures.WireTypeString
                               | SerializerFeatures.OptionTrySkipWritingWhenMeasuring
                               | SerializerFeatures.CategoryMessageWrappedAtRoot;
                    }
                    
                    return SerializerFeatures.CategoryMessage
                           | SerializerFeatures.WireTypeString
                           | SerializerFeatures.CategoryMessageWrappedAtRoot;
                }
            }
                
            public void Write(ref ProtoWriter.State state, Stream value)
            {
                if (value is null) return;

                value.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[ChunkSize];
                try
                {
                    int bytesRead;
                    while ((bytesRead = value.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        state.WriteFieldHeader(1, WireType.String);
                        state.WriteBytes(new ReadOnlyMemory<byte>(buffer, 0, bytesRead));
                    }
                }
                finally
                {
                    value.Seek(0, SeekOrigin.Begin);
                }
            }

            public Stream Read(ref ProtoReader.State state, Stream value)
            {
                var ms = new MemoryStream();
                int field;
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            var chunk = state.AppendBytes(default(ReadOnlyMemory<byte>));
                            if (chunk.Length > 0)
                                ms.Write(chunk.ToArray(), 0, chunk.Length);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                ms.Position = 0;
                return ms;
            }

            public int Measure(ISerializationContext context, WireType wireType, Stream value)
            {
                if (value is null || value.Length <= 0) return -1;

                int streamLength = (int)value.Length;
                int lastChunkSize = streamLength % ChunkSize;
                lastChunkSize = lastChunkSize == 0 ? ChunkSize : lastChunkSize;

                int totalLength = 1 + GetVarintLength(lastChunkSize) + lastChunkSize;

                int fullChunks = ((streamLength + ChunkSize - 1) / ChunkSize) - 1;
                if (fullChunks > 0)
                {
                    totalLength += fullChunks * (1 + GetVarintLength(ChunkSize) + ChunkSize);
                }

                return totalLength;
            }

            private static int GetVarintLength(int value)
            {
                int length = 0;
                do
                {
                    value >>= 7;
                    length++;
                } while (value != 0);
                return length;
            }
        }
    }
}
