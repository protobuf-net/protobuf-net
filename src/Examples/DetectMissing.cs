using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Xunit;
using ProtoBuf;
using System.Runtime.Serialization;
using ProtoBuf.Meta;

namespace Examples
{
    [ProtoContract]
    public class DetectMissing
    {
        private int? foo;
        
        [ProtoMember(1)]
        public int Foo
        {
            get { return foo ?? 5; }
            set { foo = value;}
        }
        [XmlIgnore, Browsable(false)]
        public bool FooSpecified
        {
            get { return foo != null; }
            set { if(value == (foo == null)) foo = value ? Foo : (int?)null;}
        }

        private bool ShouldSerializeFoo() {return FooSpecified; }
        private void ResetFoo() { FooSpecified = false; }

        private string bar;
        
        [ProtoMember(2)]
        public string Bar
        {
            get { return bar ?? "abc"; }
            set { bar = value;}
        }
        [XmlIgnore, Browsable(false)]
        public bool BarSpecified
        {
            get { return bar != null; }
            set { if (value == (bar == null)) bar = value ? Bar : (string)null; }
        }
        private bool ShouldSerializeBar() { return BarSpecified; }
        private void ResetBar() { BarSpecified = false; }

    }

    
    public class TestDetectMissing
    {
        [Fact]
        public void TestDefaults()
        {
            DetectMissing dm = new DetectMissing();
            Assert.Equal(5, dm.Foo);
            Assert.Equal("abc", dm.Bar);
            Assert.False(dm.FooSpecified, "FooSpecified");
            Assert.False(dm.BarSpecified, "BarSpecified");
        }

        [Fact]
        public void TestSetValuesToDefaults()
        {
            DetectMissing dm = new DetectMissing();
            dm.Foo = 5;
            dm.Bar = "abc";
            Assert.Equal(5, dm.Foo);
            Assert.Equal("abc", dm.Bar);
            Assert.True(dm.FooSpecified, "FooSpecified");
            Assert.True(dm.BarSpecified, "BarSpecified");
        }

        [Fact]
        public void TestSetValuesToNewValues()
        {
            DetectMissing dm = new DetectMissing();
            dm.Foo = 7;
            dm.Bar = "def";
            Assert.Equal(7, dm.Foo);
            Assert.Equal("def", dm.Bar);
            Assert.True(dm.FooSpecified, "FooSpecified");
            Assert.True(dm.BarSpecified, "BarSpecified");
        }

        [Fact]
        public void TestSetSpecified()
        {
            DetectMissing dm = new DetectMissing();
            dm.FooSpecified = true;
            dm.BarSpecified = true;
            Assert.Equal(5, dm.Foo);
            Assert.Equal("abc", dm.Bar);
            Assert.True(dm.FooSpecified, "FooSpecified");
            Assert.True(dm.BarSpecified, "BarSpecified");
        }

        [Fact]
        public void TestResetSpecified()
        {
            DetectMissing dm = new DetectMissing();
            dm.Foo = 27;
            dm.Bar = "ghi";
            dm.FooSpecified = false;
            dm.BarSpecified = false;
            Assert.Equal(5, dm.Foo);
            Assert.Equal("abc", dm.Bar);
            Assert.False(dm.FooSpecified, "FooSpecified");
            Assert.False(dm.BarSpecified, "BarSpecified");
        }

        [Fact]
        public void TestViaXmlSerializerNotSet()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer ser = new XmlSerializer(typeof(DetectMissing));
                ser.Serialize(ms, new DetectMissing());
                ms.Position = 0;
                DetectMissing dm = (DetectMissing) ser.Deserialize(ms);
                Assert.False(dm.FooSpecified, "FooSpecified");
                Assert.False(dm.BarSpecified, "BarSpecified");
            }
        }
        [Fact]
        public void TestViaXmlSerializerSet() {
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer ser = new XmlSerializer(typeof(DetectMissing));
                ser.Serialize(ms, new DetectMissing { FooSpecified = true, BarSpecified = true});
                ms.Position = 0;
                DetectMissing dm = (DetectMissing)ser.Deserialize(ms);
                Assert.True(dm.FooSpecified, "FooSpecified");
                Assert.True(dm.BarSpecified, "BarSpecified");
            }
        }

        [Fact]
        public void TestViaXmlProtoNotSet()
        {
            var model = TypeModel.Create();
            model.Add(typeof(DetectMissing), true);
            DetectMissing dm1 = (DetectMissing)model.DeepClone(new DetectMissing());
            Assert.False(dm1.FooSpecified, "FooSpecified:Runtime");
            Assert.False(dm1.BarSpecified, "BarSpecified:Runtime");

            model.CompileInPlace();
            DetectMissing dm2 = (DetectMissing)model.DeepClone(new DetectMissing());
            Assert.False(dm2.FooSpecified, "FooSpecified:CompileInPlace");
            Assert.False(dm2.BarSpecified, "BarSpecified:CompileInPlace");

            DetectMissing dm3 = (DetectMissing)model.Compile().DeepClone(new DetectMissing());
            Assert.False(dm3.FooSpecified, "FooSpecified:Compile");
            Assert.False(dm3.BarSpecified, "BarSpecified:Compile");

            model.Compile("TestViaXmlProtoNotSet", "TestViaXmlProtoNotSet.dll");
            PEVerify.AssertValid("TestViaXmlProtoNotSet.dll");
        }
        [Fact]
        public void TestViaXmlProtoSet()
        {
            var model = TypeModel.Create();
            model.Add(typeof(DetectMissing), true);
            DetectMissing dm1 = (DetectMissing)model.DeepClone(new DetectMissing { FooSpecified = true, BarSpecified = true });
            Assert.True(dm1.FooSpecified, "FooSpecified:Runtime");
            Assert.True(dm1.BarSpecified, "BarSpecified:Runtime");

            model.CompileInPlace();
            DetectMissing dm2 = (DetectMissing)model.DeepClone(new DetectMissing { FooSpecified = true, BarSpecified = true });
            Assert.True(dm2.FooSpecified, "FooSpecified:CompileInPlace");
            Assert.True(dm2.BarSpecified, "BarSpecified:CompileInPlace");

            DetectMissing dm3 = (DetectMissing)model.Compile().DeepClone(new DetectMissing { FooSpecified = true, BarSpecified = true });
            Assert.True(dm3.FooSpecified, "FooSpecified:Compile");
            Assert.True(dm3.BarSpecified, "BarSpecified:Compile");

            model.Compile("TestViaXmlProtoSet", "TestViaXmlProtoSet.dll");
            PEVerify.AssertValid("TestViaXmlProtoSet.dll");
        }
#if !COREFX
        [Fact]
        public void TestComponentModelNotSet()
        {
            DetectMissing dm = new DetectMissing();
            var props = TypeDescriptor.GetProperties(dm);
            Assert.False(props["Foo"].ShouldSerializeValue(dm), "Foo");
            Assert.False(props["Bar"].ShouldSerializeValue(dm), "Bar");
        }

        [Fact]
        public void TestComponentModelSet()
        {
            DetectMissing dm = new DetectMissing {FooSpecified = true, BarSpecified = true};
            var props = TypeDescriptor.GetProperties(dm);
            Assert.True(props["Foo"].ShouldSerializeValue(dm), "Foo");
            Assert.True(props["Bar"].ShouldSerializeValue(dm), "Bar");
        }

        [Fact]
        public void TestComponentModelReset()
        {
            DetectMissing dm = new DetectMissing { Foo = 37, Bar = "fgjh" };
            var props = TypeDescriptor.GetProperties(dm);
            Assert.True(props["Foo"].CanResetValue(dm), "Foo");
            Assert.True(props["Bar"].CanResetValue(dm), "Bar");
            props["Foo"].ResetValue(dm);
            props["Bar"].ResetValue(dm);
            Assert.False(dm.FooSpecified, "Foo");
            Assert.False(dm.BarSpecified, "Bar");
        }
#endif
    }
}
