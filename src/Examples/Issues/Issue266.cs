using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.IO;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue266
    {
        [Test]
        public void TestNakedNullableInt32Deserialize()
        {
            int? i = Serializer.Deserialize<int?>(Stream.Null);
            Assert.IsNull(i);
        }
        [Test]
        public void TestWrappedNullableEnumDeserialize()
        {
            Bar bar = Serializer.Deserialize<Bar>(Stream.Null);
            Assert.IsNull(bar.Foo);
        }
        [Test]
        public void TestNakedNullableEnumDeserialize()
        {
            Foo? foo = Serializer.Deserialize<Foo?>(Stream.Null);
            Assert.IsNull(foo);
        }
        [Test]
        public void TestNakedDirectFoo()
        {
            Foo orig = Foo.B, result;
            using(var ms = new MemoryStream())
            {
                RuntimeTypeModel.Default.Serialize(ms, Foo.B);
                ms.Position = 0;
                result = (Foo) RuntimeTypeModel.Default.Deserialize(ms, null, typeof (Foo));
            }
            Assert.AreEqual(orig, result);
        }

        public enum Foo
        {
            A = 2, B = 3
        }
        [ProtoContract]
        public class Bar
        {
            [ProtoMember(1)]
            public Foo? Foo { get; set; }
        }
    }
}
