using System;
using NUnit.Framework;

namespace ProtoBuf
{
    [TestFixture]
    public class PartialClasses
    {


        [Test]
        public void TestPartial()
        {
            PartialData orig = new PartialData {
                Name = "abcdefghijklmnopqrstuvwxyz",
                Number = 1234,
                When = new DateTime(2008,1,1),
                HowMuchNotSerialized = 123.456M
            },  clone = Serializer.DeepClone(orig);

            Assert.IsNotNull(orig, "original");
            Assert.IsNotNull(clone, "clone");
            Assert.AreEqual(orig.Name, clone.Name, "name");
            Assert.AreEqual(orig.Number, clone.Number, "number");
            Assert.AreEqual(orig.When, clone.When, "when");
            Assert.AreEqual(0.0M, clone.HowMuchNotSerialized, "how much");
        }

        [Test]
        public void TestSubClass()
        {
            SubClassData orig = new SubClassData
            {
                Name = "abcdefghijklmnopqrstuvwxyz",
                Number = 1234,
                When = new DateTime(2008, 1, 1),
                HowMuchNotSerialized = 123.456M
            }, clone = Serializer.DeepClone(orig);

            Assert.IsNotNull(orig, "original");
            Assert.IsNotNull(clone, "clone");
            Assert.AreEqual(orig.Name, clone.Name, "name");
            Assert.AreEqual(orig.Number, clone.Number, "number");
            Assert.AreEqual(orig.When, clone.When, "when");
            Assert.AreEqual(0.0M, clone.HowMuchNotSerialized, "how much");
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
    public partial class PartialData
    {
    }

    [ProtoPartialMember(1, "Number")]
    [ProtoPartialMember(2, "Name")]
    [ProtoPartialMember(3, "When")]
    [ProtoContract]
    public class SubClassData : PartialData
    {
    }

}
