using System.IO;
using NUnit.Framework;
using ProtoBuf;
using System;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue284
    {
        [Test, ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Dynamic type is not a contract-type: Int32")]
        public void Execute()
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
        }

        [ProtoContract]
        public class MyArgs
        {
            [ProtoMember(1, DynamicType = true)]
            public object Value;
        }
    }
}
