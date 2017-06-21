using System.Runtime.Serialization;
using Xunit;
using ProtoBuf;

namespace Examples.SimpleStream
{
    
    public class NullTypes
    {
        [DataContract]
        class TypeWithNulls
        {
            [DataMember(Order = 1)]
            public int? Foo { get; set; }
        }
        [Fact]
        public void TestNull()
        {
            TypeWithNulls twn = new TypeWithNulls { Foo = null },
                clone = Serializer.DeepClone(twn);
            Assert.Null(twn.Foo);
            Assert.True(Program.CheckBytes(twn, new byte[0]));
        }
        [Fact]
        public void TestNotNull()
        {
            TypeWithNulls twn = new TypeWithNulls { Foo = 150 },
                clone = Serializer.DeepClone(twn);
            Assert.NotNull(twn.Foo);
            Assert.True(Program.CheckBytes(twn, 0x08, 0x96, 0x01));
        }
    }
}
