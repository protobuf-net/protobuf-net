using System.ComponentModel;
using Xunit;

namespace ProtoBuf.Issues
{
    public class SO58135958
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class Foo
        {
            public PricingFlags Flags {get;set;}
        }

        public enum PricingFlags : long
        {
            [Description("Aggregate")]
            Aggregate = 1L << 7,

            Something = 4296080913,
        }

        [Theory]
        [InlineData(PricingFlags.Aggregate)]
        [InlineData(PricingFlags.Something)]
        [InlineData((PricingFlags)0)]
        [InlineData((PricingFlags)10)]
        [InlineData((PricingFlags)(-10))]
        [InlineData((PricingFlags)long.MinValue)]
        [InlineData((PricingFlags)(long.MinValue + 10))]
        [InlineData((PricingFlags)(long.MaxValue - 10))]
        public void CheckLongEnumRoundTrips(PricingFlags value)
        {
            var obj = new Foo { Flags = value };
            var clone = Serializer.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.Equal(value, clone.Flags);
        }
    }
}
