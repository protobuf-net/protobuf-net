
namespace RandomNamespace.Tests.AOT
{
    using System;
    using ProtoBuf;

    public class AotTests
    {
        [Xunit.Fact]
        public void GetName()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            Xunit.Assert.NotNull(model[typeof(Foo)].CustomSerializer);
            
        }
    }

    class FakeItFoo : ISerializer<Foo>
    {
        void ISerializer<Foo>.Read(ProtoReader reader, ref Foo value)
        {
            
        }

        void ISerializer<Foo>.Write(ProtoWriter writer, ref Foo value)
        {
            
        }
    }
    [ProtoBuf.ProtoContract, ProtoBuf.CodeGen.CodeGen]
    public class Foo
    {
        [ProtoBuf.ProtoMember(1)]
        public int X {get;set;}

        public string Y { get; set; }
    }
}
