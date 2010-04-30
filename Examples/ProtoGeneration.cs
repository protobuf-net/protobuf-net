using Examples.SimpleStream;
using NUnit.Framework;
using ProtoBuf;
using System.ComponentModel;

namespace Examples
{
    [TestFixture]
    public class ProtoGeneration
    {
        [Test, Ignore("GetProto not implemented yet")]
        public void GetProtoTest1() {
            string proto = Serializer.GetProto<Test1>();
            Assert.AreEqual(
@"package Examples.SimpleStream;

message Test1 {
   required int32 a = 1;
}
", proto);
        }

        [Test, Ignore("GetProto not implemented yet")]
        public void TestProtoGenerationWithDefaultString()
        {
            string proto = Serializer.GetProto<MyClass>();
            Assert.AreEqual(@"
message MyClass {
   optional string TestString = 1 [default = ""Test Test TEst""];
}
", proto);
        }
    }
}
[ProtoContract]
class MyClass
{
    [ProtoMember(1), DefaultValue("Test Test TEst")]
    public string TestString { get; set; }
}

