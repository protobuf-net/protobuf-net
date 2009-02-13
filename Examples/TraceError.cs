using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.IO;

namespace Examples
{
    [ProtoContract]
    public class TraceErrorData
    {
        [ProtoMember(1)]
        public int Foo { get; set; }

        [ProtoMember(2)]
        public string Bar { get; set; }

    }

    [TestFixture]
    public class TraceError
    {
        [Test]
        public void TestTrace()
        {
            TraceErrorData ed = new TraceErrorData {Foo = 12, Bar = "abcdefghijklmnopqrstuvwxyz"};
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, ed);
            byte[] buffer = ms.GetBuffer();

            MemoryStream ms2 = new MemoryStream();
            ms2.Write(buffer, 0, (int)ms.Length - 5);
            try
            {
                Serializer.Deserialize<TraceErrorData>(ms2);
            } catch(Exception ex)
            {
                Assert.IsTrue(ex.Data.Contains("protoSource"));
                Assert.AreEqual("TraceErrorData:2", ex.Data["protoSource"]);
            }
        }
    }
}
