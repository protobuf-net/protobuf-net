using ProtoBuf.Meta;
using System;
using Xunit;

namespace ProtoBuf.Issues
{
    public partial class Issue401
    {
        [Fact]
        public void IsDefinedWorksWhenAddingTypes()
        {
            var type = typeof(MyClass);
            var m = TypeModel.Create();

            Assert.False(m.IsDefined(type));

            var protoType = m.Add(type, true);

            Assert.True(m.IsDefined(type));
        }

        class MyClass
        {
            public string MyProp { get; set; }
        }
    }
}
