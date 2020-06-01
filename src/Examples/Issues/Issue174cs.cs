#if FEAT_DYNAMIC_REF
using ProtoBuf;
using System;
using System.IO;
using Xunit;

namespace Examples.Issues
{
    public class Issue174cs
    {
        [Fact]
        public void TestDynamic()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var myVal = new TestProto { Value = true };
                byte[] serialized;
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, myVal);
                    serialized = ms.ToArray();
                }
                Assert.NotNull(serialized);
            }, "Dynamic type is not a contract-type: Boolean");
        }

        [ProtoContract]
        public class TestProto
        {
            [ProtoMember(1, DynamicType = true)]
            public object Value { get; internal set; }
        }
    }
}
#endif