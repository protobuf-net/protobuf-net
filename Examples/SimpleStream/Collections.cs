using System.Collections.Generic;
using ProtoBuf;
using System.IO;
using System;
using NUnit.Framework;

namespace Examples.SimpleStream
{
    [TestFixture]
    public class Collections
    {
        [ProtoContract]
        public class Foo
        {
            public Foo() { Bars = new List<Bar>(); }
            [ProtoMember(1)]
            public List<Bar> Bars { get; private set; }
        }
        [ProtoContract]
        public class Bar
        {
            [ProtoMember(1)]
            public int Value { get; set; }
        }
        [Test]
        public void RunCollectionTests()
        {
            Foo foo = new Foo(), clone;
            for (int i = Int16.MinValue; i <= Int16.MaxValue; i++)
            {
                foo.Bars.Add(new Bar { Value = i });
            }
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                ms.Position = 0;
                clone = Serializer.Deserialize<Foo>(ms);
            }
            Assert.AreEqual(foo.Bars.Count, clone.Bars.Count, "Item count");
            
            int count = clone.Bars.Count;
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(foo.Bars[i].Value, clone.Bars[i].Value, "Value mismatch");
            }
        }
    }
}
