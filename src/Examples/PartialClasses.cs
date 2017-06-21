using System;
using Xunit;

namespace ProtoBuf
{
    
    public class PartialClasses
    {


        [Fact]
        public void TestPartial()
        {
            PartialData orig = new PartialData {
                Name = "abcdefghijklmnopqrstuvwxyz",
                Number = 1234,
                When = new DateTime(2008,1,1),
                HowMuchNotSerialized = 123.456M
            },  clone = Serializer.DeepClone(orig);

            Assert.NotNull(orig); //, "original");
            Assert.NotNull(clone); //, "clone");
            Assert.Equal(orig.Name, clone.Name); //, "name");
            Assert.Equal(orig.Number, clone.Number); //, "number");
            Assert.Equal(orig.When, clone.When); //, "when");
            Assert.Equal(0.0M, clone.HowMuchNotSerialized); //, "how much");
        }

        [Fact]
        public void TestSubClass()
        {
            SubClassData orig = new SubClassData
            {
                Name = "abcdefghijklmnopqrstuvwxyz",
                Number = 1234,
                When = new DateTime(2008, 1, 1),
                HowMuchNotSerialized = 123.456M
            }, clone = (SubClassData)Serializer.DeepClone<PartialData>(orig);

            Assert.NotNull(orig); //, "original");
            Assert.NotNull(clone); //, "clone");
            Assert.Equal(orig.Name, clone.Name); //, "name");
            Assert.Equal(orig.Number, clone.Number); //, "number");
            Assert.Equal(orig.When, clone.When); //, "when");
            Assert.Equal(0.0M, clone.HowMuchNotSerialized); //, "how much");
        }
    }

    public partial class PartialData
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public DateTime When { get; set; }
        public decimal HowMuchNotSerialized { get; set; }
    }


    [ProtoPartialMember(1, "Number")]
    [ProtoPartialMember(2, "Name")]
    [ProtoPartialMember(3, "When")]
    [ProtoContract]
    [ProtoInclude(4, typeof(SubClassData))]
    public partial class PartialData
    {
    }

    [ProtoContract]
    public class SubClassData : PartialData
    {
    }

}
