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

        private static ProtoReader.State GetReader()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Foo), true);
            model.CompileInPlace();

            var ms = new MemoryStream();
            var obj = new Foo { Bar = "abc", Blap = "abc" };
            var s = ProtoWriter.State.Create(ms, model, null);
            try
            {
                s.SerializeRoot(obj);
                s.Close();
            }
            finally
            {
                s.Dispose();
            }
            ms.Position = 0;

            return ProtoReader.State.Create(ms, model, null);
        }
        [Fact]
        public void ByDefaultStringsShouldNotBeInterned()
        {
            Foo foo;
            using (var state = GetReader())
            {
                foo = (Foo)state.DeserializeRoot<Foo>(null);
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.False(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Fact]
        public void ExplicitEnabledStringsShouldBeInterned()
        {
            Foo foo;
            var state = GetReader();
            try
            {
                state.InternStrings = true;
                foo = (Foo)state.DeserializeRoot<Foo>(null);
            }
            finally
            {
                state.Dispose();
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.True(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Fact]
        public void ExplicitDisabledStringsShouldNotBeInterned()
        {
            Foo foo;
            var state = GetReader();
            try
            {
                state.InternStrings = false;
                foo = (Foo)state.DeserializeRoot<Foo>(null);
            }
            finally
            {
                state.Dispose();
            }
            Assert.Equal("abc", foo.Bar); //, "Bar");
            Assert.Equal("abc", foo.Blap); //, "Blap");

            Assert.False(ReferenceEquals(foo.Bar, foo.Blap));
        }
    }
}
