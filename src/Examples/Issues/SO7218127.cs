using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{

    [TestFixture]
    public class SO7218127
    {
        [Test]
        public void Test()
        {
            var orig = new SomeWrapper {Value = new SubType { Foo = 123, Bar = "abc"}};
            var clone = Serializer.DeepClone(orig);
            Assert.AreEqual(123, orig.Value.Foo);
            Assert.AreEqual("abc", ((SubType) clone.Value).Bar);
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
