
using ProtoBuf;
namespace SO11895998
{
    [ProtoContract]
    [ProtoInclude(2, typeof(Bar))]
    public abstract class Foo
    {
        [ProtoMember(1)]
        public int Value { get; set; }
    }

    [ProtoContract]
    public class Bar : Foo
    {
        [ProtoMember(2)]
        public string Name { get; set; }
    }
}
