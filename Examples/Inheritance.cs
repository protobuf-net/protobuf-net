using System;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class Inheritance
    {

        [ProtoContract]
        public class A { }

        public class B : A { }
        [ProtoContract]
        public class C
        {
            [ProtoMember(1)]
            public A A { get; set; }
        }
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage="Unexpected sub-type: Examples.Inheritance+B")]
        public void UnknownSubtypeMessage()
        {
            var c = new C { A = new B() };
            Serializer.DeepClone(c);
        }

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
