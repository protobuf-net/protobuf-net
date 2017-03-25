using System.Net;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

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
            var model = TypeModel.Create();
            model.AllowParseableTypes = true;
            WithIP obj = new WithIP { Address = IPAddress.Parse("100.90.80.100") },
                clone = (WithIP) model.DeepClone(obj);

            Assert.AreEqual(obj.Address, clone.Address);

            obj.Address = null;
            clone = (WithIP)model.DeepClone(obj);

            Assert.IsNull(obj.Address, "obj");
            Assert.IsNull(clone.Address, "clone");

        }
    }
}
