using Examples.SimpleStream;
using NUnit.Framework;
using ProtoBuf;
using System.ComponentModel;
using ProtoBuf.Meta;
using System.Runtime.Serialization;

namespace Examples
{
    [TestFixture]
    public class ProtoGeneration
    {
        [Test]
        public void GetProtoTest1()
        {
            var model = TypeModel.Create();
            model.UseImplicitZeroDefaults = false;
            string proto = model.GetSchema(typeof(Test1));
            Assert.AreEqual(
@"package Examples.SimpleStream;

message Test1 {
   required int32 a = 1;
}
", proto);
        }

        [Test]
        public void GetProtoTest2()
        {
            var model = TypeModel.Create();
            model.UseImplicitZeroDefaults = false;
            string proto = model.GetSchema(typeof(Test2));
            Assert.AreEqual(
@"package Examples;

message abc {
   required uint32 ghi = 2;
   required bytes def = 3;
}
", proto);
        }

        [DataContract(Name="abc")]
        public class Test2
        {
            [DataMember(Name = "def", IsRequired = true, Order = 3)]
            public byte[] X { get; set; }

            [DataMember(Name = "ghi", IsRequired = true, Order = 2)]
            public char Y { get; set; }
        }

        [Test]
        public void TestProtoGenerationWithDefaultString()
        {
#pragma warning disable 0618
            string proto = Serializer.GetProto<MyClass>();
#pragma warning restore 0618
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

