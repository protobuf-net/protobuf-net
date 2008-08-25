using System;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.NetExtensions;

namespace Examples
{
    [TestFixture]
    public class Inheritance
    {
        [Test]
        public void TestFooAsFoo()
        {
            Foo foo = new Foo { Value = 1 };
            Assert.AreEqual(foo.Value, Serializer.DeepClone(foo).Value);
        }
        [Test]
        public void TestBarAsBar()
        {
            Bar bar = new Bar { Value = 1 };
            Assert.AreEqual(bar.Value, Serializer.DeepClone<Foo>(bar).Value);
        }

        [Test]
        public void TestBarAsFoo()
        {
            Foo foo = new Bar { Value = 1 };
            Foo clone = Serializer.DeepClone(foo);
            Assert.AreEqual(foo.Value, clone.Value);
            Assert.IsInstanceOfType(typeof(Bar), clone);
        }
    }
    
    [ProtoContract]
    [ProtoInclude(2, typeof(Bar))]
    class Foo
    {
        [ProtoMember(1)]
        public int Value { get; set; }
    }

    [ProtoContract]
    class Bar : Foo
    {

    }
}
