using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.IO;
using ProtoBuf;
using System.Runtime.Serialization;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class AssortedGoLiveRegressions
    {
        [Fact]
        public void TestStringFromEmpty()
        {
            using (var ms = new MemoryStream())
            {
                Assert.NotNull(Serializer.Deserialize<Foo>(ms)); //, "Foo");
                Assert.Null(Serializer.Deserialize<string>(ms)); //, "string");
                Assert.Null(Serializer.Deserialize<DateTime?>(ms)); //, "DateTime?");
                Assert.Null(Serializer.Deserialize<int?>(ms)); //, "int?");
            }
        }

        [Fact]
        public void TestStringArray()
        {
            var orig = new[] { "abc", "def" };
            Assert.True(Serializer.DeepClone(orig).SequenceEqual(orig));
        }

        [Fact]
        public void TestInt32Array()
        {
            var orig = new[] { 1, 2 };
            Assert.True(Serializer.DeepClone(orig).SequenceEqual(orig));
        }

        [Fact]
        public void TestByteArray()
        {
            // byte[] is a special case that compares most closely to 1:data
            // (rather than 1:item0 1:item1 1:item2 etc)
            var orig = new byte[] { 0, 1, 2, 4, 5 };
            var clone = Serializer.ChangeType<byte[], HasBytes>(orig).Blob;
            Assert.True(orig.SequenceEqual(clone));
        }

        [ProtoContract]
        public class HasBytes
        {
            [ProtoMember(1)]
            public byte[] Blob { get; set; }
        }

        [Fact]
        public void TestStringDictionary()
        {
            var orig = new Dictionary<string,string> { {"abc","def" }};
            var clone = Serializer.DeepClone(orig).Single();
            MetaType[] types = RuntimeTypeModel.Default.GetTypes().Cast<MetaType>().ToArray();
            Assert.Equal(orig.Single().Key, clone.Key);
            Assert.Equal(orig.Single().Value, clone.Value);
        }

        [Fact]
        public void TestFooList()
        {
            var orig = new List<Foo> { new Foo() { Count = 12, Name = "abc" } };

            var clone = Serializer.DeepClone(orig).Single();
            Assert.Equal(orig.Single().Count, clone.Count);
            Assert.Equal(orig.Single().Name, clone.Name);
        }



        [Fact]
        public void TestEmptyStringDictionary()
        {
            var orig = new Dictionary<string, string> { };
            Assert.Empty(orig);

            var clone = Serializer.DeepClone(orig);
            Assert.NotNull(clone);
            Assert.Empty(clone);
        }

        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public string Name { get; set; }
            [ProtoMember(2)]
            public int Count { get; set; }
        }
    }
}
