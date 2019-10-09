using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using System.IO;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class Issue266
    {
        [Fact]
        public void TestNakedNullableInt32Deserialize()
        {
            int? i = Serializer.Deserialize<int?>(Stream.Null);
            Assert.Null(i);
        }
        [Fact]
        public void TestWrappedNullableEnumDeserialize()
        {
            Bar bar = Serializer.Deserialize<Bar>(Stream.Null);
            Assert.Null(bar.Foo);
        }
        [Fact]
        public void TestNakedNullableEnumDeserialize()
        {
            Foo? foo = Serializer.Deserialize<Foo?>(Stream.Null);
            Assert.Null(foo);
        }
        [Fact]
        public void TestNakedDirectFoo()
        {
            Foo orig = Foo.B, result;
            using(var ms = new MemoryStream())
            {
                RuntimeTypeModel.Default.Serialize(ms, Foo.B);
                ms.Position = 0;
#pragma warning disable CS0618
                result = (Foo) RuntimeTypeModel.Default.Deserialize(ms, null, typeof (Foo));
#pragma warning restore CS0618
            }
            Assert.Equal(orig, result);
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
