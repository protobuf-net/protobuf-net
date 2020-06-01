#if PLAT_ARRAY_BUFFER_WRITER
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Issues
{
    public class Issue571
    {
        [ProtoContract]
        public class TestContract
        {
            [ProtoMember(1)]
            public long Value { get; set; }
        }

        public Issue571(ITestOutputHelper log)
            => _log = log;
        private readonly ITestOutputHelper _log;

        private void Log(string message) => _log?.WriteLine(message);

        void Parse(ref ReadOnlySequence<byte> buffer)
        {
            Log($"Parsing 10 bytes of {buffer.Length} bytes available...");
            using var reader = ProtoReader.State.Create(
                buffer.Slice(0, 10), RuntimeTypeModel.Default);

            Log("Deserializing...");
            var value = reader.DeserializeRoot<TestContract>();
            Log($"Deserialized: Value = {value.Value}");
            Assert.Equal(long.MaxValue, value.Value);
            
            buffer = buffer.Slice(10);
            Log($"Sliced; buffer now {buffer.Length} bytes");
        }

        [Fact]
        public void ExpectedBytes()
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, new TestContract { Value = long.MaxValue });
            Assert.Equal("08-FF-FF-FF-FF-FF-FF-FF-FF-7F", BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length));
            // 9 * 11111111 + 01111111 = 63x1 (after stop-bits), so yes; FF-FF-FF-FF-FF-FF-FF-FF-7F
        }

        [Fact]
        public async Task Execute()
        {
            var data = new ArrayBufferWriter<byte>();
            Serializer.Serialize(data, new TestContract { Value = long.MaxValue });

            var options = new PipeOptions(minimumSegmentSize: 1);
            var pipe = new Pipe(options);
            var writer = pipe.Writer;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < data.WrittenCount; j++)
                {
                    writer.GetSpan(1)[0] = data.WrittenSpan[j];
                    writer.Advance(1);
                    await writer.FlushAsync();
                }
            }

            var result = await pipe.Reader.ReadAsync();
            var buffer = result.Buffer;

            Assert.Equal("08-FF-FF-FF-FF-FF-FF-FF-FF-7F-08-FF-FF-FF-FF-FF-FF-FF-FF-7F", BitConverter.ToString(buffer.ToArray()));
            Parse(ref buffer);
            Assert.Equal("08-FF-FF-FF-FF-FF-FF-FF-FF-7F", BitConverter.ToString(buffer.ToArray()));
            Parse(ref buffer);
        }
    }
}
#endif