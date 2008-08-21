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
        [Test, ExpectedException(typeof(ProtoException))]
        public void BlowUp()
        {
            RecursiveObject obj = new RecursiveObject();
            obj.Yeuch = obj;
            Serializer.Serialize(Stream.Null, obj);
        }
    }
}
