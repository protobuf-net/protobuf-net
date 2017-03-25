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

    [DataContract, ProtoContract]
    class TypeWithProtosAndDataContract_UseAny
    {
        [ProtoMember(1)]
        public int Foo { get; set; }
        [DataMember(Order=2)]
        public int Bar { get; set; }
    }
    [DataContract, ProtoContract(UseProtoMembersOnly=true)]
    class TypeWithProtosAndDataContract_UseProtoOnly
    {
        [ProtoMember(1)]
        public int Foo { get; set; }
        [DataMember(Order = 2)]
        public int Bar { get; set; }
    }
    [TestFixture]
    public class TestWeCanTurnOffNonProtoMarkers
    {
        [Test]
        public void TypeWithProtosAndDataContract_UseAny_ShouldSerializeBoth()
        {
            var orig = new TypeWithProtosAndDataContract_UseAny { Foo = 123, Bar = 456 };
            var clone = Serializer.DeepClone(orig);
            Assert.AreEqual(123, clone.Foo);
            Assert.AreEqual(456, clone.Bar);
        }
        [Test]
        public void TypeWithProtosAndDataContract_UseProtoOnly_ShouldSerializeFooOnly()
        {
            var orig = new TypeWithProtosAndDataContract_UseProtoOnly { Foo = 123, Bar = 456 };
            var clone = Serializer.DeepClone(orig);
            Assert.AreEqual(123, clone.Foo);
            Assert.AreEqual(0, clone.Bar);
        }
    }
}
