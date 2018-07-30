#if !NO_WCF
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

//#if FEAT_SERVICEMODEL && PLAT_XMLSERIALIZER
namespace Examples
{
    using ProtoBuf.ServiceModel;
    
    public class WcfProtoBridge
    {
        [Fact]
        public void TestBasicType()
        {
            var obj = new Foo {Bar = 123};
            var clone = (Foo)RoundTrip(obj);
            Assert.Equal(123, clone.Bar);
        }
        [Fact]
        public void TestBasicTypeWithDefaultValues()
        {
            var obj = new Foo { Bar = 0 };
            var clone = (Foo)RoundTrip(obj);
            Assert.Equal(0, clone.Bar);
        }
        [Fact]
        public void TestListType()
        {
            var obj = new List<Foo>{new Foo { Bar = 123 }};
            var clone = (List<Foo>)RoundTrip(obj);
            Assert.Equal(1, clone.Count);
            Assert.Equal(123, clone[0].Bar);
        }

        [Fact]
        public void TestEmptyListType()
        {
            var obj = new List<Foo> {  };
            var clone = (List<Foo>)RoundTrip(obj);
            Assert.Equal(0, clone.Count);
        }
        static object RoundTrip(object obj)
        {
            Assert.NotNull(obj); //, "obj");
            var model = RuntimeTypeModel.Create();
            var ser = XmlProtoSerializer.TryCreate(model, obj.GetType());
            Assert.NotNull(ser, "ser");
            using(var ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer(),0,(int)ms.Length));
                ms.Position = 0;
                object clone;
                using (var reader = XmlReader.Create(ms))
                {
                    Assert.True(ser.IsStartObject(reader));
                    clone = ser.ReadObject(reader);
                }
                Assert.NotNull(clone); //, "clone");
                Assert.NotSame(obj, clone);
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
//#endif
#endif