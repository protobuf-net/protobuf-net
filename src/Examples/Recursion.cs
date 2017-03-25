using System.IO;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [ProtoContract]
    public class RecursiveObject
    {
        [ProtoMember(1)]
        public RecursiveObject Yeuch { get; set; }
    }
    [TestFixture]
    public class Recursion
    {
        [Test]
        public void BlowUp()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                RecursiveObject obj = new RecursiveObject();
                obj.Yeuch = obj;
                Serializer.Serialize(Stream.Null, obj);
            });
        }
    }
}
