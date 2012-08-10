using System.IO;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.ServiceModel;

namespace Examples.Issues
{
    [TestFixture]
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
        [Test]
        public void TestRoundTripWrappedEnum()
        {
            var ser = new XmlProtoSerializer(RuntimeTypeModel.Default, typeof(FooWrapper));
            var ms = new MemoryStream();
            ser.WriteObject(ms, new FooWrapper { Foo = Foo.B });
            ms.Position = 0;
            var clone = (FooWrapper)ser.ReadObject(ms);

            Assert.AreEqual(Foo.B, clone.Foo);
        }
        [Test]
        public void TestRoundTripNakedEnum()
        {
            var ser = new XmlProtoSerializer(RuntimeTypeModel.Default, typeof (Foo));
            var ms = new MemoryStream();
            ser.WriteObject(ms, Foo.B);
            ms.Position = 0;
            var clone = (Foo)ser.ReadObject(ms);

            Assert.AreEqual(Foo.B, clone);

        }
    }
}
