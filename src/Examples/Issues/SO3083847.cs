using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class SO3083847
    {
        [Fact]
        public void Roundtrip()
        {
            CacheData data = new CacheData
            {
                Bar = new List<CacheLoadItem<int>>
                        {
                            new CacheLoadItem<int>() { ID = 123, Name = "abc", Value = 234},
                            new CacheLoadItem<int>() { ID = 345, Name = "bcd", Value = 456},
                        },
                YYY = new List<CacheLoadItem<string>>
                        {
                            new CacheLoadItem<string>() { ID = 567, Name = "cde", Value = "def"},
                            new CacheLoadItem<string>() { ID = 678, Name = "efg", Value = "fgh"},
                            new CacheLoadItem<string>() { ID = 789, Name = "ghi", Value = "hij"},
                        }
            };
            var clone = Serializer.DeepClone(data);
            Assert.NotSame(data,clone);
            Assert.NotNull(clone.Bar); //, "Bar");
            Assert.NotNull(clone.YYY); //, "YYY");
            Assert.Equal(2, clone.Bar.Count); //, "Bar");
            Assert.Equal(3, clone.YYY.Count); //, "YYY");
            var bar = clone.Bar[0];
            Assert.True(bar.ID == 123 && bar.Name == "abc" && bar.Value == 234);
            bar = clone.Bar[1];
            Assert.True(bar.ID == 345 && bar.Name == "bcd" && bar.Value == 456);
            var yyy = clone.YYY[0];
            Assert.True(yyy.ID == 567 && yyy.Name == "cde" && yyy.Value == "def");
            yyy = clone.YYY[1];
            Assert.True(yyy.ID == 678 && yyy.Name == "efg" && yyy.Value == "fgh");
            yyy = clone.YYY[2];
            Assert.True(yyy.ID == 789 && yyy.Name == "ghi" && yyy.Value == "hij");
        }

        [DataContract]
        public class CacheData
        {
            [DataMember(Order = 1)]
            public List<CacheLoadItem<int>> Foo;

            [DataMember(Order = 2)]
            public List<CacheLoadItem<int>> Bar;

            [DataMember(Order = 3)]
            public List<CacheLoadItem<int>> XXX;

            [DataMember(Order = 4)]
            public List<CacheLoadItem<string>> YYY;

            [DataMember(Order = 5)]
            public List<CacheLoadItem<int>> Other;

            [DataMember(Order = 6)]
            public List<CacheLoadItem<int>> Other2;

            [DataMember(Order = 7)]
            public List<CacheLoadItem<int>> Other3;

            [DataMember(Order = 8)]
            public List<CacheLoadItem<string>> EvenMore;

            [DataMember(Order = 9)]
            public List<CacheLoadItem<string>> AlmostThere;
        }

        [DataContract]
        public class CacheLoadItem<V>
        {
            [DataMember(Order = 1)]
            public int ID;

            [DataMember(Order = 2)]
            public string Name;

            [DataMember(Order = 3)]
            public V Value;
        }
    }
}
