using Examples.SimpleStream;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class ProtoGeneration
    {
        [Test]
        public void GetProtoTest1() {
            string proto = Serializer.GetProto<Test1>();
            Assert.AreEqual(
@"package Examples.SimpleStream;

message Test1 {
   required int32 a = 1;
}
", proto);
        }
        
    }
}
