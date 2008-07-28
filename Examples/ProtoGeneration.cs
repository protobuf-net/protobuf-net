using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using Examples.SimpleStream;

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
