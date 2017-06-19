using System.Net;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    [ProtoContract]
    public class WithIP
    {
        [ProtoMember(1)]
        public IPAddress Address { get; set; }
    }

    
    public class Parseable
    {
        [Fact]
        public void TestIPAddess()
        {
            var model = TypeModel.Create();
            model.AllowParseableTypes = true;
            WithIP obj = new WithIP { Address = IPAddress.Parse("100.90.80.100") },
                clone = (WithIP) model.DeepClone(obj);

            Assert.Equal(obj.Address, clone.Address);

            obj.Address = null;
            clone = (WithIP)model.DeepClone(obj);

            Assert.Null(obj.Address); //, "obj");
            Assert.Null(clone.Address); //, "clone");

        }
    }
}
