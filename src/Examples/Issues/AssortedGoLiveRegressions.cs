using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using ProtoBuf;
using System.Runtime.Serialization;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class AssortedGoLiveRegressions
    {
        [Test]
        public void TestStringFromEmpty()
        {
            using (var ms = new MemoryStream())
            {
                Assert.IsNotNull(Serializer.Deserialize<Foo>(ms), "Foo");
                Assert.IsNull(Serializer.Deserialize<string>(ms), "string");
                Assert.IsNotNull(Serializer.Deserialize<DateTime>(ms), "DateTime");
                Assert.IsNull(Serializer.Deserialize<DateTime?>(ms), "DateTime?");
                Assert.IsNotNull(Serializer.Deserialize<int>(ms), "int");
                Assert.IsNull(Serializer.Deserialize<int?>(ms), "int?");
            }
        }

        [Test]
        public void TestStringArray()
        {
            var orig = new[] { "abc", "def" };
            Assert.IsTrue(Serializer.DeepClone(orig).SequenceEqual(orig));
        }

        [Test]
        public void TestInt32Array()
        {
            var orig = new[] { 1, 2 };
            Assert.IsTrue(Serializer.DeepClone(orig).SequenceEqual(orig));
        }

        [Test]
        public void TestByteArray()
        {
            // byte[] is a special case that compares most closely to 1:data
            // (rather than 1:item0 1:item1 1:item2 etc)
            var orig = new byte[] { 0, 1, 2, 4, 5 };
            var clone = Serializer.ChangeType<byte[], HasBytes>(orig).Blob;
            Assert.IsTrue(orig.SequenceEqual(clone));
        }

        [ProtoContract]
        public class HasBytes
        {
            [ProtoMember(1)]
            public byte[] Blob { get; set; }
        }

        [Test]
        public void TestStringDictionary()
        {
            var orig = new Dictionary<string,string> { {"abc","def" }};
            var clone = Serializer.DeepClone(orig).Single();
            MetaType[] types = RuntimeTypeModel.Default.GetTypes().Cast<MetaType>().ToArray();
            Assert.AreEqual(orig.Single().Key, clone.Key);
            Assert.AreEqual(orig.Single().Value, clone.Value);
        }

        [Test]
        public void TestFooList()
        {
            var orig = new List<Foo> { new Foo() { Count = 12, Name = "abc" } };

            var clone = Serializer.DeepClone(orig).Single();
            Assert.AreEqual(orig.Single().Count, clone.Count);
            Assert.AreEqual(orig.Single().Name, clone.Name);
        }



        [Test]
        public void TestEmptyStringDictionary()
        {
            var orig = new Dictionary<string, string> { };
            Assert.AreEqual(0, orig.Count);

            var clone = Serializer.DeepClone(orig);
            Assert.IsNotNull(clone);
            Assert.AreEqual(0, clone.Count);
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
