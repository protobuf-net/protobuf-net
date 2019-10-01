#if FEAT_DYNAMIC_REF
using System.IO;
using Xunit;
using ProtoBuf;
using System;

namespace Examples.Issues
{
    
    public class Issue284
    {
        [Fact]
        public void Execute()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                MyArgs test = new MyArgs
                {
                    Value = 12,
                };

                byte[] buffer = new byte[256];
                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    Serializer.Serialize(ms, test);
                }

                using (MemoryStream ms = new MemoryStream(buffer))
                {
                    Serializer.Deserialize<MyArgs>(ms);
                }
            }, "Dynamic type is not a contract-type: Int32");
        }

        [ProtoContract]
        public class MyArgs
        {
            [ProtoMember(1, DynamicType = true)]
            public object Value;
        }
    }
}
#endif