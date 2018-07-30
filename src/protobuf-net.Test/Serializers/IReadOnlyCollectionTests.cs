using System.Collections.Generic;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.unittest.Serializers
{
    public class IReadOnlyCollectionTests
    {
        [Fact]
        public void BasicIReadOnlyCollectionTest()
        {
            var orig = new TypeWithIReadOnlyCollection { Items = new List<string>{"abc", "def"} };
            var model = TypeModel.Create();
            var clone = (TypeWithIReadOnlyCollection)model.DeepClone(orig);
            Assert.Equal(orig.Items, clone.Items); //, "Runtime");

            model.CompileInPlace();
            clone = (TypeWithIReadOnlyCollection)model.DeepClone(orig);
            Assert.Equal(orig.Items, clone.Items); //, "CompileInPlace");

            clone = (TypeWithIReadOnlyCollection)model.Compile().DeepClone(orig);
            Assert.Equal(orig.Items, clone.Items); //, "Compile");
        }

        [ProtoContract]
        public class TypeWithIReadOnlyCollection
        {
            [ProtoMember(1)]
            public IReadOnlyCollection<string> Items { get; set; }
        }
    }
}