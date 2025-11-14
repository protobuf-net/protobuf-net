using Xunit;

namespace ProtoBuf
{
    public class PartialStructs
    {
        [Fact]
        public void TestPartialStruct()
        {
            StructData orig = new StructData(number: 42), clone = Serializer.DeepClone(orig);
            Assert.Equal(orig.Number, clone.Number);
        }
    }

    public readonly partial struct StructData
    {
        public StructData(int number)
        {
            Number = number;
        }

        public int Number { get; }
    }

    [ProtoPartialMember(1, nameof(Number))]
    public partial struct StructData;
}
