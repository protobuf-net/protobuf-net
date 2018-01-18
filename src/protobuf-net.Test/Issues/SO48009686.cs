using System.Collections.Generic;
using Xunit;

namespace ProtoBuf.Issues
{
    public class SO48009686
    {
        [ProtoContract]
        public class MyObject_Default
        {
            [ProtoMember(2)]
            public Dictionary<long, decimal?> MyDictionary { get; set; }

            [ProtoMember(3)]
            public decimal Total { get; set; }
        }

        [ProtoContract]
        public class MyObject_NoMap
        {
            [ProtoMember(2), ProtoMap(DisableMap = true)]
            public Dictionary<long, decimal?> MyDictionary { get; set; }

            [ProtoMember(3)]
            public decimal Total { get; set; }
        }
        static Dictionary<long, decimal?> GetData() => new Dictionary<long, decimal?>
        {
            { 12, 34.5M },
            { 67, null  },
        };
        [Fact]
        public void CanRoundtrip_Default()
        {
            var orig = new MyObject_Default { MyDictionary = GetData() };
            var clone = Serializer.DeepClone(orig);

            Assert.Equal(2, clone.MyDictionary.Count);
            Assert.Equal(34.5M, clone.MyDictionary[12]);
            Assert.Null(clone.MyDictionary[67]);
        }

        [Fact]
        public void CanRoundtrip_NoMap()
        {
            var orig = new MyObject_NoMap { MyDictionary = GetData() };
            var clone = Serializer.DeepClone(orig);

            Assert.Equal(2, clone.MyDictionary.Count);
            Assert.Equal(34.5M, clone.MyDictionary[12]);
            Assert.Null(clone.MyDictionary[67]);
        }
    }
}