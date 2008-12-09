using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class DataMemberOffset
    {
        [Test]
        public void TestOffset()
        { 
            DMO_First first = new DMO_First {Foo = 12};
            DMO_Second second = Serializer.ChangeType<DMO_First, DMO_Second>(first);

            Assert.AreEqual(first.Foo, second.Bar);
        }

    }

    [DataContract]
    class DMO_First
    {
        [DataMember(Order = 5)]
        public int Foo { get; set; }
    }
    [DataContract]
    [ProtoContract(DataMemberOffset = 2)]
    class DMO_Second
    {
        [DataMember(Order = 3)]
        public int Bar { get; set; }
    }
}
