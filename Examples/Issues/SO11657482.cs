using System.IO;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11657482
    {
        [ProtoContract]
        [ProtoInclude(1, typeof(Derived))]
        public abstract class Base { }

        [ProtoContract]
        public class Derived : Base
        {
            [ProtoMember(1)]
            public int SomeProperty { get; set; }
        }

        [ProtoContract]
        public class Aggregate
        {
            [ProtoMember(1, AsReference = true)]
            public Base Base { get; set; }
        }

        [Test]
        public void TestMethod1()
        {
            var value = new Aggregate { Base = new Derived() };
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, value);
                stream.Position = 0;

                var obj = Serializer.Deserialize<Aggregate>(stream);
                Assert.AreEqual(typeof(Derived), obj.Base.GetType());
            }
        }
    }
}
