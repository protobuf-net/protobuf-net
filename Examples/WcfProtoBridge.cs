using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.ServiceModel;
using System.IO;

namespace Examples
{
    [TestFixture]
    public class WcfProtoBridge
    {
        [Test]
        public void TestBasicType()
        {
            var obj = new Foo {Bar = 123};
            var clone = (Foo)RoundTrip(obj);
            Assert.AreEqual(123, clone.Bar);
        }
        [Test]
        public void TestBasicTypeWithDefaultValues()
        {
            var obj = new Foo { Bar = 0 };
            var clone = (Foo)RoundTrip(obj);
            Assert.AreEqual(0, clone.Bar);
        }
        [Test]
        public void TestListType()
        {
            var obj = new List<Foo>{new Foo { Bar = 123 }};
            var clone = (List<Foo>)RoundTrip(obj);
            Assert.AreEqual(1, clone.Count);
            Assert.AreEqual(123, clone[0].Bar);
        }
        static object RoundTrip(object obj)
        {
            Assert.IsNotNull(obj, "obj");
            var model = RuntimeTypeModel.Create();
            var ser = XmlProtoSerializer.TryCreate(model, obj.GetType());
            Assert.IsNotNull(ser, "ser");
            using(var ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                ms.Position = 0;
                var clone = ser.ReadObject(ms);
                Assert.IsNotNull(clone, "clone");
                Assert.AreNotSame(obj, clone);
                return clone;
            }
        }

        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public int Bar { get; set; }
        }
    }
}
