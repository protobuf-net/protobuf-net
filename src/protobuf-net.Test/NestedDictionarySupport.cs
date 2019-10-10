using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class NestedDictionarySupport
    {
        [Fact]
        public void DictionaryListList_Empty() => RoundTrip(new HazDictionaryWithLists(), "");

        [Theory]
        [InlineData(new object[] { new int[] { }, "D2-02-00" })] // field 42, length 0
        [InlineData(new object[] { new int[] { 7 }, "D2-02-02-08-07" })] // field 42, length 2; field 1 = 7
        [InlineData(new object[] { new int[] { 4, 5}, "D2-02-04-08-04-08-05" })] // field 42, length 4; field 1 = 4, field 1 = 5
        public void DictionaryListList_SingleList_KeyEmptyList(int[] items, string expectedHex)
        {
            List<int> list = new List<int>(items);
            RoundTrip(new HazDictionaryWithLists
            {
                CrazyMap = { { list, null } }
            }, expectedHex); 
        }

        private void RoundTrip(HazDictionaryWithLists value, string expectedHex)
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, value);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal(expectedHex, hex);
            ms.Position = 0;
            var clone = Serializer.Deserialize<HazDictionaryWithLists>(ms);
            Assert.NotNull(clone);
            Assert.NotSame(value, clone);
            var dict = clone.CrazyMap;
            Assert.NotNull(dict);
            Assert.NotSame(value.CrazyMap, dict);
            Assert.Equal(value.CrazyMap.Count, dict.Count);

        }

        [ProtoContract]
        public class HazDictionaryWithLists
        {
            [ProtoMember(42)]
            public Dictionary<List<int>, List<string>> CrazyMap { get; } = new Dictionary<List<int>, List<string>>();
        }
    }
}
