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
        public class FooContainer
        {
            [ProtoMember(1)]
            public Foo Foo { get; set; }
        }
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public List<Bar> Bars { get; set; }
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
            FooContainer fooContainer = new FooContainer(), cloneContainer;
            Foo foo = fooContainer.Foo = new Foo(), clone;
            foo.Bars = new List<Bar>();
            for (int i = Int16.MinValue; i <= Int16.MaxValue; i++)
            {
                foo.Bars.Add(new Bar { Value = i });
            }
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, fooContainer);
                ms.Position = 0;
                cloneContainer = Serializer.Deserialize<FooContainer>(ms);
                Assert.IsNotNull(cloneContainer);
                clone = cloneContainer.Foo;
                Assert.IsNotNull(clone);
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
