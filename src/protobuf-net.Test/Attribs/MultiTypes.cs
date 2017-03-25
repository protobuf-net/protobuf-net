using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.IO;
using ProtoBuf.unittest.Serializers;

namespace ProtoBuf.unittest
{
    
    public class MultiTypeLookupTests
    {
        [Fact]
        public void TestInt32RoundTrip()
        {
            var orig = PropertyValue.Create("abc", 123);
            var intClone = Serializer.DeepClone(orig);
            Assert.Equal("abc", intClone.Name);
            Assert.Equal(123, intClone.Value);
        }
        [Fact]
        public void TestStringSerialize()
        {
            var prop = PropertyValue.Create("abc", "def");
            string hex;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, prop);
                hex = Util.GetHex(ms.ToArray());
            }
            
            Assert.Equal(
                "32" // field 6, string
              + "05" // 5 bytes
                + "0A" // field 1, string
                + "03" // 3 bytes
                  + "646566" // "def"
              + "0A" // field 1, string
              + "03" // 3 bytes
                + "616263" // "abc"
                ,hex);
        }
        [Fact]
        public void TestStringRoundTrip()
        {
            var stringClone = Serializer.DeepClone(
                PropertyValue.Create("abc", "def"));
            Assert.Equal("abc", stringClone.Name);
            Assert.Equal("def", stringClone.Value);

            Serializer.PrepareSerializer<PropertyValue>();

            stringClone = Serializer.DeepClone(
                PropertyValue.Create("abc", "def"));
            Assert.Equal("abc", stringClone.Name);
            Assert.Equal("def", stringClone.Value);
        }


        [ProtoContract]
        [ProtoInclude(5, typeof(PropertyValue<int>))]
        [ProtoInclude(6, typeof(PropertyValue<string>))]
        /* etc known types */
        public abstract class PropertyValue
        {
            public static PropertyValue<T> Create<T>(string name, T value)
            {
                return new PropertyValue<T> { Name = name, Value = value };
            }

            [ProtoMember(1)]
            public string Name { get; set; }

            public abstract object UntypedValue { get; set; }
        }
        [ProtoContract]
        public sealed class PropertyValue<T> : PropertyValue
        {
            [ProtoMember(1)]
            public T Value { get; set; }

            public override object UntypedValue
            {
                get { return this.Value; }
                set { this.Value = (T)value; }
            }
        }
    }
    
}
