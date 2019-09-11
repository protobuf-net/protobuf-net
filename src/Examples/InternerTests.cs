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

        private static ProtoReader GetReader(out ProtoReader.State state)
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Foo), true);
            model.CompileInPlace();

            var ms = new MemoryStream();
            var obj = new Foo { Bar = "abc", Blap = "abc" };
            using (var writer = ProtoWriter.Create(out var s, ms, model, null))
            {
                writer.Model.Serialize(writer, ref s, obj);
                writer.Close(ref s);
            }
            ms.Position = 0;

            return ProtoReader.Create(out state, ms, model, null);
        }
        [Fact]
        public void ByDefaultStringsShouldNotBeInterned()
        {
            Foo foo;
            using (var reader = GetReader(out var state))
            {
                foo = (Foo)reader.Model.Deserialize(reader, ref state, null, typeof(Foo));
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.False(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Fact]
        public void ExplicitEnabledStringsShouldBeInterned()
        {
            Foo foo;
            using (var reader = GetReader(out var state))
            {
                reader.InternStrings = true;
                foo = (Foo)reader.Model.Deserialize(reader, ref state, null, typeof(Foo));
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.True(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Fact]
        public void ExplicitDisabledStringsShouldNotBeInterned()
        {
            Foo foo;
            using (var reader = GetReader(out var state))
            {
                reader.InternStrings = false;
                foo = (Foo)reader.Model.Deserialize(reader, ref state, null, typeof(Foo));
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.False(ReferenceEquals(foo.Bar, foo.Blap));
        }
    }
}
