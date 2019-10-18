using Pipelines.Sockets.Unofficial.Buffers;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class InputOutputAPI
    {
        private static object ModelObj => RuntimeTypeModel.Default;
        [Fact]
        public void IsStreamInput() => Assert.True(ModelObj is IProtoInput<Stream>);
        [Fact]
        public void IsArrayInput() => Assert.True(ModelObj is IProtoInput<byte[]>);
        [Fact]
        public void IsArraySegmentInput() => Assert.True(ModelObj is IProtoInput<ArraySegment<byte>>);
        [Fact]
        public void IsStreamOutput() => Assert.True(ModelObj is IProtoOutput<Stream>);
        [Fact]
        public void IsArrayOutput() => Assert.False(ModelObj is IProtoOutput<byte[]>);
        [Fact]
        public void IsArraySegmentOutput() => Assert.False(ModelObj is IProtoOutput<ArraySegment<byte>>);

        [ProtoContract]
        public class SomeModel
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }
        [Fact]
        public void CanSerializeViaInputOutputAPI()
        {
            using var ms = new MemoryStream();
            IProtoOutput<Stream> output = RuntimeTypeModel.Default;
            var orig = new SomeModel { Id = 42 };
            output.Serialize(ms, orig);

            ms.Position = 0;

            IProtoInput<Stream> input = RuntimeTypeModel.Default;
            var clone = input.Deserialize<SomeModel>(ms);

            Assert.NotSame(orig, clone);
            Assert.Equal(42, clone.Id);

            IProtoInput<byte[]> arrayInput = RuntimeTypeModel.Default;
            clone = arrayInput.Deserialize<SomeModel>(ms.ToArray());

            Assert.NotSame(orig, clone);
            Assert.Equal(42, clone.Id);

            var segment = new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
            IProtoInput<ArraySegment<byte>> segmentInput = RuntimeTypeModel.Default;
            clone = segmentInput.Deserialize<SomeModel>(segment);

            Assert.NotSame(orig, clone);
            Assert.Equal(42, clone.Id);
        }

        [Fact]
        public void CanSerializeViaInputOutputAPI_Buffers()
        {
            using var buffer = BufferWriter<byte>.Create();
            IProtoOutput<IBufferWriter<byte>> output = RuntimeTypeModel.Default;

            var orig = new SomeModel { Id = 42 };
            output.Serialize(buffer, orig);

            using var payload = buffer.Flush();
            IProtoInput<ReadOnlySequence<byte>> input = RuntimeTypeModel.Default;
            var clone = input.Deserialize<SomeModel>(payload.Value);

            Assert.NotSame(orig, clone);
            Assert.Equal(42, clone.Id);
        }
    }
}
