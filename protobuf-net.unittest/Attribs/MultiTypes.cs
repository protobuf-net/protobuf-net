using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ProtoBuf.unittest
{
    [TestFixture]
    public class MultiTypeLookupTests
    {
        [Test]
        public void TestInt32RoundTrip()
        {
            var orig = PropertyValue.Create("abc", 123);
            var intClone = Serializer.DeepClone(orig);
            Assert.AreEqual("abc", intClone.Name);
            Assert.AreEqual(123, intClone.Value);
        }
        [Test]
        public void TestStringRoundTrip()
        {
            var stringClone = Serializer.DeepClone(
                PropertyValue.Create("abc", "def"));
            Assert.AreEqual("abc", stringClone.Name);
            Assert.AreEqual("def", stringClone.Value);
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
