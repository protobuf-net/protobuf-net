using NUnit.Framework;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{
    [TestFixture, Ignore("not sure we want to support this")]
    public class Issue70 {

        [ProtoContract]
        public class Strange // test entity
        {
            [ProtoMember(1)]
            public string Foo { get; set; } // test prop
            [ProtoMember(2)]
            public int Bar { get; set; } // test prop
        }

        [Test]
        public void SerializeWithLengthPrefixShouldWorkWithBase128()
        {
            var original = new Strange { Foo = "abc", Bar = 123 };
            // serialize and deserialize with base-128
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(ms, original, PrefixStyle.Base128, 1);
                ms.Position = 0;
                object obj;
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(ms,
                    PrefixStyle.Base128, i => typeof(Strange), out obj);
                var clone = (Strange)obj;
                Assert.AreNotSame(original, clone);
                Assert.IsNotNull(clone);
                Assert.AreEqual(original.Foo, clone.Foo, "Foo");
                Assert.AreEqual(original.Bar, clone.Bar, "Bar");
            }
        }
        [Test]
        public void SerializeWithLengthPrefixShouldWorkWithFixed32()
        {
            var original = new Strange { Foo = "abc", Bar = 123 };
            // serialize and deserialize with fixed-32
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(ms, original, PrefixStyle.Fixed32, 1);
                ms.Position = 0;
                object obj;
                // BOOM here; oh how embarrassing
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(ms,
                    PrefixStyle.Fixed32, i => typeof(Strange), out obj);
                var clone = (Strange)obj;
                Assert.AreNotSame(original, clone);
                Assert.IsNotNull(clone);
                Assert.AreEqual(original.Foo, clone.Foo, "Foo");
                Assert.AreEqual(original.Bar, clone.Bar, "Bar");
            }
        }
    
    }
}