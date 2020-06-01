using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using System.IO;
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
        public void VerifyMyObject_NoMap()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(MyObject_NoMap));
            model.Compile("VerifyMyObject_NoMap", "VerifyMyObject_NoMap.dll");
        }
        [Fact]
        public void CanRoundtrip_Default()
        {
            var orig = new MyObject_Default { MyDictionary = GetData() };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, orig);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("12-09-08-0C-12-05-08-D9-02-18-02-12-02-08-43", hex);
            /*
             12 = field 2, type String
             09 = length 9
               08 = field 1, type Variant
               0C = 12 (raw) or 6 (zigzag)
               12 = field 2, type String
               05 = length 5
                 08 = field 1, type Variant
                 D9-02 = 345 (raw) or -173 (zigzag)
                 18 = field 3, type Variant <== scale
                 02 = 2 (raw) or 1 (zigzag)
             12 = field 2, type String
             02 = length 2
               08 = field 1, type Variant
               43 = 67 (raw) or -34 (zigzag)
            */


            ms.Position = 0;
            var clone = Serializer.Deserialize<MyObject_Default>(ms);

            Assert.NotNull(clone.MyDictionary);
            Assert.Equal(2, clone.MyDictionary.Count);
            Assert.Equal(34.5M, clone.MyDictionary[12]);
            Assert.Null(clone.MyDictionary[67]);
        }

        [Fact]
        public void CanRoundtrip_NoMap()
        {
            var orig = new MyObject_NoMap { MyDictionary = GetData() };
            var clone = Serializer.DeepClone(orig);

            Assert.NotNull(clone.MyDictionary);
            Assert.Equal(2, clone.MyDictionary.Count);
            Assert.Equal(34.5M, clone.MyDictionary[12]);
            Assert.Null(clone.MyDictionary[67]);
        }
    }
}