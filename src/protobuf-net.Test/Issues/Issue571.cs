#if PLAT_ARRAY_BUFFER_WRITER
using ProtoBuf.Meta;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue571
    {
        [ProtoContract]
        class TestContract
        {
            [ProtoMember(1)]
            public long Value { get; set; }
        }

        static void Parse(ref ReadOnlySequence<byte> buffer)
        {
            using var reader = ProtoReader.State.Create(
                buffer.Slice(0, 10), RuntimeTypeModel.Default);

            var value = reader.DeserializeRoot<TestContract>();

            buffer = buffer.Slice(10);
        }

        [Fact]
        static async Task Execute()
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

            Parse(ref buffer);
            Parse(ref buffer);
        }
    }
}
#endif