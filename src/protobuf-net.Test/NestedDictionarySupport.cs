using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf
{
    public class NestedDictionarySupport
    {
        [Fact]
        public void DictionaryListList_Empty() => RoundTripCheck(new HazDictionaryWithLists(), "");

        [Theory]
        [InlineData(new object[] { new int[] { }, "D2-02-02-0A-00" })] // field 42, length 0, field 1 = (empty)
        [InlineData(new object[] { new int[] { 7 }, "D2-02-02-08-07" })] // field 42, length 2; field 1 = 7
        [InlineData(new object[] { new int[] { 4, 5}, "D2-02-04-0A-02-04-05" })] // field 42, length 4; field 1 = 4, field 1 = 5
        // note: v2 was "D2-02-04-08-04-08-05", but this is equivalent as 'packed'
        public void DictionaryListList_SingleList_KeyEmptyList(int[] items, string expectedHex)
        {
            List<int> list = new List<int>(items);
            RoundTripCheck(new HazDictionaryWithLists
            {
                CrazyMap = { { list, null } }
            }, expectedHex); 
        }

        [Theory]
        [InlineData(new object[] { null, "D2-02-02-08-01" })]
        [InlineData(new object[] { new string[] { }, "D2-02-02-08-01" })]
        [InlineData(new object[] { new string[] { "abc" }, "D2-02-07-08-01-12-03-61-62-63" })]
        [InlineData(new object[] { new string[] { "def", "ghi"}, "D2-02-0C-08-01-12-03-64-65-66-12-03-67-68-69" })]
        public void DictionaryListList_SingleList_KeyDataList(string[] items, string expectedHex)
        {
            List<string> list = items == null ? null : new List<string>(items);
            RoundTripCheck(new HazDictionaryWithLists
            {
                CrazyMap = { { new List<int> { 1 }, list } }
            }, expectedHex);
        }
        
        private void RoundTripCheck(HazDictionaryWithLists value, string expectedHex)
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

            if (value.CrazyMap.Count == 1)
            {
                var pair = dict.Single();
                Assert.NotNull(pair.Key); // Dictionary key cannot be null
                
                var oldPair = value.CrazyMap.Single();
                Assert.True(oldPair.Key.SequenceEqual(pair.Key));

                if (oldPair.Value == null || !oldPair.Value.Any())
                {
	                Assert.Null(pair.Value);
                } 
                else 
                {
	                Assert.True(oldPair.Value.SequenceEqual(pair.Value));
                }
            }
        }

        [ProtoContract]
        public class HazDictionaryWithLists
        {
            [ProtoMember(42)]
            public Dictionary<List<int>, List<string>> CrazyMap { get; } = new Dictionary<List<int>, List<string>>();
        }
    }
}
