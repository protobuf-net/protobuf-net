using Google.Protobuf.WellKnownTypes;
using System;
using Xunit;

namespace ProtoBuf
{
    public class Usage
    {
        [Theory]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.I), typeof(double?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.I), typeof(double?))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.I), typeof(DoubleValue))]
        public void CheckPropertyType(Type source, string name, Type valueType)
            => Assert.Same(valueType, source.GetProperty(name)?.PropertyType);

        [Theory]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.S), false)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.S), true)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.S), false)]
        public void CheckSetter(Type source, string name, bool hasSetter)
            => Assert.Equal(hasSetter, source.GetProperty(name)?.CanWrite);

        [Fact]
        public void CheckUsageVanilla()
        {
            Vanilla.Foo foo = new();
            double? x = foo.I;
            foo.I = x * 2;
        }

        [Fact]
        public void CheckUsageEnabled()
        {
            Enabled.Foo foo = new();
            double? x = foo.I;
            foo.I = x * 2;
        }

        [Fact]
        public void CheckUsageDisabled()
        {
            Disabled.Foo foo = new();
            DoubleValue x = foo.I;
            foo.I = new DoubleValue();
        }
    }
}
namespace Google.Protobuf.WellKnownTypes
{
    public class DoubleValue { } // impl not shown
}
