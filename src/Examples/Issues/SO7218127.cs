#if FEAT_DYNAMIC_REF

using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    public class SO7218127
    {
        [Fact]
        public void Test()
        {
            var orig = new SomeWrapper {Value = new SubType { Foo = 123, Bar = "abc"}};
            var clone = Serializer.DeepClone(orig);
            Assert.Equal(123, orig.Value.Foo);
            Assert.Equal("abc", ((SubType) clone.Value).Bar);
        }
        [ProtoContract]
        public class SomeWrapper
        {
            [ProtoMember(1, DynamicType = true)]
            public BaseType Value { get; set; }
        }
        [ProtoContract]
        public class BaseType
        {
            [ProtoMember(1)]
            public int Foo { get; set; }
        }
        [ProtoContract]
        public class SubType : BaseType
        {
            [ProtoMember(2)]
            public string Bar { get; set; }
        }
    }
}

#endif