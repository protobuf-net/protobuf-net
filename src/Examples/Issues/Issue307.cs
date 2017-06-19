using System.IO;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
#if !NO_WCF
using ProtoBuf.ServiceModel;
#endif

namespace Examples.Issues
{
    
    public class Issue307
    {
        public enum Foo
        {
            A,
            B,
            C
        }
        [ProtoContract]
        public class FooWrapper
        {
            [ProtoMember(1)]
            public Foo Foo { get; set; }
        }
#if !NO_WCF
        [Fact]
        public void TestRoundTripWrappedEnum()
        {
            var ser = new XmlProtoSerializer(RuntimeTypeModel.Default, typeof(FooWrapper));
            var ms = new MemoryStream();
            ser.WriteObject(ms, new FooWrapper { Foo = Foo.B });
            ms.Position = 0;
            var clone = (FooWrapper)ser.ReadObject(ms);

            Assert.Equal(Foo.B, clone.Foo);
        }
        [Fact]
        public void TestRoundTripNakedEnum()
        {
            var ser = new XmlProtoSerializer(RuntimeTypeModel.Default, typeof (Foo));
            var ms = new MemoryStream();
            ser.WriteObject(ms, Foo.B);
            ms.Position = 0;
            var clone = (Foo)ser.ReadObject(ms);

            Assert.Equal(Foo.B, clone);

        }
#endif
    }
}
