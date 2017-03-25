using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System.IO;

namespace Examples
{
    [TestFixture]
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
            using (var writer = new ProtoWriter(ms, model, null))
            {
                writer.Model.Serialize(writer, obj);
            }
            ms.Position = 0;

            return new ProtoReader(ms, model, null);
        }
        [Test]
        public void ByDefaultStringsShouldBeInterned()
        {
            Foo foo;
            using (var reader = GetReader())
            {
                foo = (Foo)reader.Model.Deserialize(reader, null, typeof(Foo));
            }
            Assert.AreEqual("abc", foo.Bar, "Bar");
            Assert.AreEqual("abc", foo.Blap, "Blap");

            Assert.IsTrue(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Test]
        public void ExplicitEnabledStringsShouldBeInterned()
        {
            Foo foo;
            using (var reader = GetReader())
            {
                reader.InternStrings = true;
                foo = (Foo)reader.Model.Deserialize(reader, null, typeof(Foo));
            }
            Assert.AreEqual("abc", foo.Bar, "Bar");
            Assert.AreEqual("abc", foo.Blap, "Blap");

            Assert.IsTrue(ReferenceEquals(foo.Bar, foo.Blap));
        }
        [Test]
        public void ExplicitDisabledStringsShouldNotBeInterned()
        {
            Foo foo;
            using (var reader = GetReader())
            {
                reader.InternStrings = false;
                foo = (Foo)reader.Model.Deserialize(reader, null, typeof(Foo));
            }
            Assert.AreEqual("abc", foo.Bar, "Bar");
            Assert.AreEqual("abc", foo.Blap, "Blap");

            Assert.IsFalse(ReferenceEquals(foo.Bar, foo.Blap));
        }

    }
}
