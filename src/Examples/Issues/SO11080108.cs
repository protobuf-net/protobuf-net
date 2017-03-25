using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11080108
    {
        [Test]
        public void Execute()
        {
            byte[] buffer = { 9, 8, 5, 26, 5, 24, 238, 98, 32, 1 };
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            using (var ms = new MemoryStream(buffer))
            {

                int len = ProtoReader.DirectReadVarintInt32(ms);
                var resp = (Response)model.Deserialize(ms, null, typeof(Response), len);

                Assert.AreEqual(5, resp.Type);
                Assert.AreEqual(1, resp.v3dDelta.Count);
                Assert.AreEqual(12654, resp.v3dDelta[0].ask);
                Assert.AreEqual(1, resp.v3dDelta[0].askSize);
            }
        }
        [ProtoContract]
        public class V3DDelta
        {
            [ProtoMember(1)]
            public int bid { get; set; }
            [ProtoMember(2)]
            public int bidSize { get; set; }
            [ProtoMember(3)]
            public int ask { get; set; }
            [ProtoMember(4)]
            public int askSize { get; set; }
        }

        [ProtoContract]
        public class Request
        {
            [ProtoMember(1)]
            public int Type { get; set; }
            [ProtoMember(2)]
            public string Rq { get; set; }
        }

        [ProtoContract]
        public class Response
        {
            [ProtoMember(1)]
            public int Type { get; set; }
            [ProtoMember(2)]
            public string Rsp { get; set; }
            [ProtoMember(3)]
            public List<V3DDelta> v3dDelta { get; set; }
            public Response()
            {
                v3dDelta = new List<V3DDelta>();
            }
        }
    }
}
