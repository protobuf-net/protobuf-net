using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue402
    {
        [Fact]
        public void ZeroWithoutScaleShouldRoundtrip() => CheckZero(0m);

        [Fact]
        public void MinusZeroWithoutScaleShouldRoundtrip() => CheckZero(new decimal(0,0,0,true,0));

        [Fact]
        public void ZeroWithScaleShouldRoundtrip() => CheckZero(0.0000000m);

        static RuntimeTypeModel Serializer;
        static Issue402()
        {
            Serializer = RuntimeTypeModel.Create();
            Serializer.UseImplicitZeroDefaults = false;
        }
        private void CheckZero(decimal value)
        {
            var clone = ((Foo)Serializer.DeepClone(new Foo { Value = value })).Value;
            
            var origBits = decimal.GetBits(value);
            var cloneBits = decimal.GetBits(clone);

            Assert.Equal(string.Join(",", origBits), string.Join(",", cloneBits));
        }
        [ProtoContract]
        class Foo
        {
            [ProtoMember(1)]
            public decimal Value { get; set; }
        }
    }
}
