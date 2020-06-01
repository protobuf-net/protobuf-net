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
            var m = RuntimeTypeModel.Create();

            Assert.False(m.IsDefined(type));

            _ = m.Add(type, true);

            Assert.True(m.IsDefined(type));
        }

        [Fact]
        public void IsDefinedWorksWhenUsingIndexer()
        {
            var type = typeof(MyClass);
            var m = RuntimeTypeModel.Create();

            Assert.False(m.IsDefined(type));
            var mt = m[type];
            Assert.NotNull(mt);
            Assert.True(m.IsDefined(type));
        }

        class MyClass
        {
            public string MyProp { get; set; }
        }

        [Fact]
        public void IsDefinedWorksWhenAddingSubTypes()
        {
            var baseType = typeof(MyBaseClass);
            var subType = typeof(MyDerivedClass);
            var m = RuntimeTypeModel.Create();

            Assert.False(m.IsDefined(baseType));
            Assert.False(m.IsDefined(subType));

            var protoType = m.Add(baseType, true);
            Assert.True(m.IsDefined(baseType));
            Assert.False(m.IsDefined(subType));

            protoType.AddSubType(100, subType);
            Assert.True(m.IsDefined(baseType));
            Assert.True(m.IsDefined(subType));
        }

        class MyBaseClass { }
        class MyDerivedClass : MyBaseClass { }
    }
}
