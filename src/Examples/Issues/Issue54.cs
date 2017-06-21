using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class Issue54
    {
        [ProtoContract]
        class Test54
        {
            [ProtoMember(1)]
            public Dictionary<float, List<int>> Lists { get; set; }
        }


        [Fact]
        public void TestNestedLists()
        {
            Test54 obj = new Test54
            {
                Lists =
                    new Dictionary<float, List<int>> {
                {123.45F, new List<int> {1,2,3}},
                {678.90F, new List<int> {4,5,6}},
            }
            }, clone = Serializer.DeepClone(obj);
            Assert.NotSame(obj, clone);
            Assert.NotNull(clone.Lists);
            Assert.Equal(obj.Lists.Count, clone.Lists.Count);
            foreach (var key in obj.Lists.Keys)
            {
                Assert.True(clone.Lists.ContainsKey(key), key.ToString());
                var list = clone.Lists[key];
                Assert.NotNull(list); //, key.ToString());
                Assert.True(obj.Lists[key].SequenceEqual(list), key.ToString());
            }
        }

    }

}
