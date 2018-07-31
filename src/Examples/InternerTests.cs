using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;
using System.IO;

namespace Examples
{
    
    public class InternerTests
    {
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public string Bar { get; set; }
            [ProtoMember(2)]
            public string Blap { get; set; }
        }

        static ProtoReader GetReader()
        {
            var model = TypeModel.Create();
            model.Add(typeof(Foo), true);
            model.CompileInPlace();

            var ms = new MemoryStream();
            var obj = new Foo { Bar = "abc", Blap = "abc" };
            using (var writer = ProtoWriter.Create(ms, model, null))
            {
                writer.Model.Serialize(writer, obj);
            }
            ms.Position = 0;

            return ProtoReader.Create(ms, model, null);
        }
        [Fact]
        public void ByDefaultStringsShouldBeInterned()
        {
            Foo foo;
            using (var reader = GetReader())
            {
                foo = (Foo)reader.Model.Deserialize(reader, null, typeof(Foo));
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.True(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Fact]
        public void ExplicitEnabledStringsShouldBeInterned()
        {
            Foo foo;
            using (var reader = GetReader())
            {
                reader.InternStrings = true;
                foo = (Foo)reader.Model.Deserialize(reader, null, typeof(Foo));
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.True(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Fact]
        public void ExplicitDisabledStringsShouldNotBeInterned()
        {
            Foo foo;
            using (var reader = GetReader())
            {
                reader.InternStrings = false;
                foo = (Foo)reader.Model.Deserialize(reader, null, typeof(Foo));
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.False(ReferenceEquals(foo.Bar, foo.Blap));
        }

    }
}
