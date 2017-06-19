using System.IO;
using Xunit;
using ProtoBuf;

namespace Examples
{
    [ProtoContract]
    public class RecursiveObject
    {
        [ProtoMember(1)]
        public RecursiveObject Yeuch { get; set; }
    }
    
    public class Recursion
    {
        [Fact]
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
