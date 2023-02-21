using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace ProtoBuf
{
    public class Usage
    {
        [Theory]
        // enabled
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.doubleProp), typeof(double?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.floatProp), typeof(float?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.int64Prop), typeof(long?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.uInt64Prop), typeof(ulong?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.int32Prop), typeof(int?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.uInt32Prop), typeof(uint?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.boolProp), typeof(bool?))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.stringProp), typeof(string))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.bytesProp), typeof(byte[]))]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.intItems), typeof(List<int>))]
        // vanilla
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.doubleProp), typeof(double?))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.floatProp), typeof(float?))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.int64Prop), typeof(long?))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.uInt64Prop), typeof(ulong?))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.int32Prop), typeof(int?))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.uInt32Prop), typeof(uint?))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.boolProp), typeof(bool?))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.stringProp), typeof(string))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.bytesProp), typeof(byte[]))]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.intItems), typeof(int[]))]
        // disabled
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.doubleProp), typeof(DoubleValue))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.floatProp), typeof(FloatValue))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.int64Prop), typeof(Int64Value))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.uInt64Prop), typeof(UInt64Value))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.int32Prop), typeof(Int32Value))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.uInt32Prop), typeof(UInt32Value))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.boolProp), typeof(BoolValue))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.stringProp), typeof(StringValue))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.bytesProp), typeof(BytesValue))]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.intItems), typeof(int[]))]
        public void CheckPropertyType(Type source, string name, Type valueType)
            => Assert.Same(valueType, source.GetProperty(name)?.PropertyType);

        [Theory]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.stringItems), false)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.stringItems), true)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.stringItems), false)]
        public void CheckSetter(Type source, string name, bool hasSetter)
            => Assert.Equal(hasSetter, source.GetProperty(name)?.CanWrite);

        [Theory]
        // enabled
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.doubleProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.floatProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.int64Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.uInt64Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.int32Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.uInt32Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.boolProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.stringProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Enabled.Foo), nameof(Enabled.Foo.bytesProp), typeof(NullWrappedValueAttribute), true)]
        // vanilla
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.doubleProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.floatProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.int64Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.uInt64Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.int32Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.uInt32Prop), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.boolProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.stringProp), typeof(NullWrappedValueAttribute), true)]
        [InlineData(typeof(Vanilla.Foo), nameof(Vanilla.Foo.bytesProp), typeof(NullWrappedValueAttribute), true)]
        // disabled
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.doubleProp), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.floatProp), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.int64Prop), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.uInt64Prop), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.int32Prop), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.uInt32Prop), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.boolProp), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.stringProp), typeof(NullWrappedValueAttribute), false)]
        [InlineData(typeof(Disabled.Foo), nameof(Disabled.Foo.bytesProp), typeof(NullWrappedValueAttribute), false)]
        public void CheckPropertyAttributeExists(Type source, string propertyName, Type expectedAttribute, bool expectsAttributetoExist)
        {
            var property = source.GetProperty(propertyName);
            if (property is null) Assert.Fail($"Expected to have a property '{propertyName}' on type {source}, but found nothing");

            var attributeInstance = property.GetCustomAttribute(expectedAttribute);
            if (expectsAttributetoExist)
            {
                Assert.NotNull(attributeInstance);
            }
            else
            {
                Assert.Null(attributeInstance);
            }
        }

        [Fact]
        public void CheckUsageVanilla()
        {
            Vanilla.Foo foo = new();
            double? x = foo.doubleProp;
            foo.doubleProp = x * 2;
        }

        [Fact]
        public void CheckUsageEnabled()
        {
            Enabled.Foo foo = new();
            double? x = foo.doubleProp;
            foo.doubleProp = x * 2;
        }

        [Fact]
        public void CheckUsageDisabled()
        {
            Disabled.Foo foo = new();
            DoubleValue x = foo.doubleProp;
            foo.doubleProp = new DoubleValue();
        }
    }
}
namespace Google.Protobuf.WellKnownTypes
{
    public class DoubleValue { } // impl not shown
    public class FloatValue { } // impl not shown
    public class Int64Value { } // impl not shown
    public class UInt64Value { } // impl not shown
    public class Int32Value { } // impl not shown
    public class UInt32Value { } // impl not shown
    public class BoolValue { } // impl not shown
    public class StringValue { } // impl not shown
    public class BytesValue { } // impl not shown

}
