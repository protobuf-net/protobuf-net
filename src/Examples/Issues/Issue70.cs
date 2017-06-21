using Xunit;
using ProtoBuf;
using System.IO;

namespace Examples.Issues
{
    
    public class Issue70 {

        [ProtoContract]
        public class Strange // test entity
        {
            [ProtoMember(1)]
            public string Foo { get; set; } // test prop
            [ProtoMember(2)]
            public int Bar { get; set; } // test prop
        }

        [Fact(Skip = "not sure we want to support this")]
        public void SerializeWithLengthPrefixShouldWorkWithBase128()
        {
            var original = new Strange { Foo = "abc", Bar = 123 };
            // serialize and deserialize with base-128
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(ms, original, PrefixStyle.Base128, 1);
                ms.Position = 0;
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(ms,
                    PrefixStyle.Base128, i => typeof(Strange), out object obj);
                var clone = (Strange)obj;
                Assert.NotSame(original, clone);
                Assert.NotNull(clone);
                Assert.Equal(original.Foo, clone.Foo); //, "Foo");
                Assert.Equal(original.Bar, clone.Bar); //, "Bar");
            }
        }
        [Fact(Skip = "not sure we want to support this")]
        public void SerializeWithLengthPrefixShouldWorkWithFixed32()
        {
            var original = new Strange { Foo = "abc", Bar = 123 };
            // serialize and deserialize with fixed-32
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(ms, original, PrefixStyle.Fixed32, 1);
                ms.Position = 0;
                // BOOM here; oh how embarrassing
                Serializer.NonGeneric.TryDeserializeWithLengthPrefix(ms,
                    PrefixStyle.Fixed32, i => typeof(Strange), out object obj);
                var clone = (Strange)obj;
                Assert.NotSame(original, clone);
                Assert.NotNull(clone);
                Assert.Equal(original.Foo, clone.Foo); //, "Foo");
                Assert.Equal(original.Bar, clone.Bar); //, "Bar");
            }
        }
    
    }
}