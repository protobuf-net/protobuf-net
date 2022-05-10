using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class OverwriteEnumerable
    {
        [Fact]
        public void Execute()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            ExecuteImpl(model);
            model.CompileInPlace();
            ExecuteImpl(model);
            ExecuteImpl(model.CompileAndVerify());
        }
        private void ExecuteImpl(TypeModel model)
        {
            var obj = new TypeWithEnumerableProperty { Values = new List<string> { "val1", "val2", "val3" } };
            var clone = model.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal("val1,val2,val3", string.Join(",", clone.Values));
        }
    }
    [ProtoContract]
    public partial class TypeWithEnumerableProperty
    {
        [ProtoMember(1, OverwriteList = true)]
        public IEnumerable<string> Values { get; set; } = Enumerable.Empty<string>();
    }
}
