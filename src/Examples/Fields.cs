using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.Runtime.Serialization;
using System.IO;
using ProtoBuf;

namespace Examples
{
    
    public class Fields
    {
        [Fact]
        public void TestWcfSerializesDataContractAsExpected()
        {
            WcfWithFields obj = new WcfWithFields { Foo = 123, Bar = "abc" }, clone;
            DataContractSerializer dcs = new DataContractSerializer(typeof(WcfWithFields));
            using (MemoryStream ms = new MemoryStream())
            {
                dcs.WriteObject(ms, obj);
                ms.Position = 0;
                clone = (WcfWithFields)dcs.ReadObject(ms);
            }
            Assert.Equal(obj.Foo, clone.Foo); //, "Foo");
            Assert.Equal(obj.Bar, clone.Bar); //, "Bar");
        }

        [Fact]
        public void TestProtoSerializesDataContractAsExpected()
        {
            WcfWithFields obj = new WcfWithFields { Foo = 123, Bar = "abc" },
                clone = Serializer.DeepClone(obj);
            Assert.Equal(obj.Foo, clone.Foo); //, "Foo");
            Assert.Equal(obj.Bar, clone.Bar); //, "Bar");
        }

        [Fact]
        public void TestProtoSerializesProtoContractAsExpected()
        {
            ProtoWithFields obj = new ProtoWithFields { Foo = 123, Bar = "abc" },
                clone = Serializer.DeepClone(obj);
            Assert.Equal(obj.Foo, clone.Foo); //, "Foo");
            Assert.Equal(obj.Bar, clone.Bar); //, "Bar");
        }
    }

    [DataContract]
    public class WcfWithFields
    {
        [DataMember(Order = 1)]
        private int foo;
        public int Foo
        {
            get { return foo; }
            set { foo = value; }
        }

        [DataMember(Order = 2)]
        private string bar;
        public string Bar
        {
            get { return bar; }
            set { bar = value; }
        }
    }

    [ProtoContract]
    public class ProtoWithFields
    {
        [ProtoMember(1)]
        private int foo;
        public int Foo
        {
            get { return foo; }
            set { foo = value; }
        }

        [ProtoMember(2)]
        private string bar;
        public string Bar
        {
            get { return bar; }
            set { bar = value; }
        }
    }
}
