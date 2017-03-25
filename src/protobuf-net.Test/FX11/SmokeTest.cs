using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Tests.Dnx
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".

    public class SmokeTest
    {
        [Fact]
        public void ExpectPass()
        {
            var foo = new Foo { Id = 1234567 };
            var clone = Serializer.DeepClone(foo);
            Assert.Equal(1234567, clone.Id);
        }

        [Fact]
        public void CanCompile()
        {
            var model = TypeModel.Create();
            model.Add(typeof(Foo), true);
            var compiled = model.Compile();

            var foo = new Foo { Id = 1234567 };
            var clone = (Foo)compiled.DeepClone(foo);
            Assert.Equal(1234567, clone.Id);
        }
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public int Id { get; set; }
        }
    }
}
