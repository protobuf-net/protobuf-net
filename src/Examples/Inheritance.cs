using System;
using Xunit;
using ProtoBuf;

namespace Examples
{
    
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
        [Fact]
        public void UnknownSubtypeMessage()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var c = new C { A = new B() };
                Serializer.DeepClone(c);
            }, "Unexpected sub-type: Examples.Inheritance+B");
        }

        [Fact]
        public void TestFooAsFoo()
        {
            Foo foo = new Foo { Value = 1 };
            Assert.Equal(foo.Value, Serializer.DeepClone(foo).Value);
        }
        [Fact]
        public void TestBarAsBar()
        {
            Bar bar = new Bar { Value = 1 };
            Assert.Equal(bar.Value, Serializer.DeepClone<Foo>(bar).Value);
        }

        [Fact]
        public void TestBarAsFoo()
        {
            Foo foo = new Bar { Value = 1 };
            Foo clone = Serializer.DeepClone(foo);
            Assert.Equal(foo.Value, clone.Value);
            Assert.IsType<Bar>(clone);
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
