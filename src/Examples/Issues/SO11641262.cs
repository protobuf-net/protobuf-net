using System;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO11641262
    {
        [Fact]
        public void Execute()
        {
            var model = TypeModel.Create();
            model.Add(typeof (FooData), true)
                .AddSubType(1, typeof (FooData<string>))
                .AddSubType(2, typeof (FooData<int>))
                .AddSubType(3, typeof (FooData<SomeOtherType>));

            var val = FooData.Create("abc");
            var clone = (FooData)model.DeepClone(val);
            Assert.Equal("abc", clone.ValueUntyped);
            Assert.Equal(typeof(string), clone.ItemType);

        }

        [ProtoContract]
        public abstract class FooData
        {
            public static FooData<T> Create<T>(T value)
            {
                return new FooData<T> {Value = value};
            }
            public abstract Type ItemType { get; }
            public abstract object ValueUntyped { get; set; }
        }
        [ProtoContract]
        public class FooData<T> : FooData
        {
            [ProtoMember(1)]
            public T Value { get; set; }
            
            public override Type ItemType
            {
                get { return typeof (T); }
            }
            public override object ValueUntyped
            {
                get { return Value; }
                set { Value = (T) value; }
            }
        }
        [ProtoContract]
        public class SomeOtherType {}
    }
}
