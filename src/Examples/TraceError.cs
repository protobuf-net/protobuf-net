using ProtoBuf;
using System.IO;
using Xunit;

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

    
    public class TraceError
    {
        [Fact]
        public void TestTrace()
        {
            TraceErrorData ed = new TraceErrorData { Foo = 12, Bar = "abcdefghijklmnopqrstuvwxyz" };
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, ed);
            byte[] buffer = ms.GetBuffer();
            Assert.Equal(30, ms.Length);
            using MemoryStream ms2 = new MemoryStream();
            ms2.Write(buffer, 0, (int)ms.Length - 5);
            ms2.Position = 0;

            var ex = Assert.Throws<EndOfStreamException>(() =>
            {
                Serializer.Deserialize<TraceErrorData>(ms2);
            });
            Assert.True(ex.Data.Contains("protoSource"), "Missing protoSource");
            Assert.Matches(@"tag=2; wire-type=String; offset=\d+; depth=0", (string)ex.Data["protoSource"]);
        }
    }
}
