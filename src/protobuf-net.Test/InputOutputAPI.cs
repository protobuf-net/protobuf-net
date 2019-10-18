using ProtoBuf.Meta;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class InputOutputAPI
    {
        [Fact]
        public void IsStreamInput() => Assert.True(RuntimeTypeModel.Default is IProtoInput<Stream>);
        [Fact]
        public void IsStreamOutput() => Assert.True(RuntimeTypeModel.Default is IProtoOutput<Stream>);

        [ProtoContract]
        public class SomeModel
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }
        [Fact]
        public void CanSerializeViaInputOutputAPI()
        {
            using (var ms = new MemoryStream())
            {
                IProtoOutput<Stream> output = RuntimeTypeModel.Default;
                var orig = new SomeModel { Id = 42 };
                output.Serialize(ms, orig);

                ms.Position = 0;

                IProtoInput<Stream> input = RuntimeTypeModel.Default;
                var clone = input.Deserialize<SomeModel>(ms);

                Assert.NotSame(orig, clone);
                Assert.Equal(42, clone.Id);
            }
        }
    }
}
