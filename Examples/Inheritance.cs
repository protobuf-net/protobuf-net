using System;
using NUnit.Framework;
using ProtoBuf;

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
            Assert.AreEqual(bar.Value, Serializer.DeepClone(bar).Value);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestBarAsFoo()
        {
            Foo foo = new Bar { Value = 1 };
            Assert.AreEqual(foo.Value, Serializer.DeepClone(foo).Value);
        }
    }
    
    [ProtoContract]
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
