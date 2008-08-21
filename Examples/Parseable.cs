using System.Net;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [ProtoContract]
    public class WithIP
    {
        [ProtoMember(1)]
        public IPAddress Address { get; set; }
    }

    [TestFixture]
    public class Parseable
    {
        [Test]
        public void TestIPAddess()
        {
            WithIP obj = new WithIP { Address = IPAddress.Parse("100.90.80.100") },
                clone = Serializer.DeepClone(obj);

            Assert.AreEqual(obj.Address, clone.Address);

            obj.Address = null;
            clone = Serializer.DeepClone(obj);

            Assert.IsNull(obj.Address, "obj");
            Assert.IsNull(clone.Address, "clone");

        }
    }
}
