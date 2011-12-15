using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

#if (FEAT_SERVICEMODEL && PLAT_XMLSERIALIZER) || (SILVERLIGHT && !PHONE7)
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

        [Test]
        public void TestEmptyListType()
        {
            var obj = new List<Foo> {  };
            var clone = (List<Foo>)RoundTrip(obj);
            Assert.AreEqual(0, clone.Count);
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
                Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer(),0,(int)ms.Length));
                ms.Position = 0;
                object clone;
                using (var reader = XmlReader.Create(ms))
                {
                    Assert.IsTrue(ser.IsStartObject(reader));
                    clone = ser.ReadObject(reader);
                }
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
#endif