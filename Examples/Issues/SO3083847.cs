using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO3083847
    {
        [Test]
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
            Assert.AreNotSame(data,clone);
            Assert.IsNotNull(clone.Bar, "Bar");
            Assert.IsNotNull(clone.YYY, "YYY");
            Assert.AreEqual(2, clone.Bar.Count, "Bar");
            Assert.AreEqual(3, clone.YYY.Count, "YYY");
            var bar = clone.Bar[0];
            Assert.IsTrue(bar.ID == 123 && bar.Name == "abc" && bar.Value == 234);
            bar = clone.Bar[1];
            Assert.IsTrue(bar.ID == 345 && bar.Name == "bcd" && bar.Value == 456);
            var yyy = clone.YYY[0];
            Assert.IsTrue(yyy.ID == 567 && yyy.Name == "cde" && yyy.Value == "def");
            yyy = clone.YYY[1];
            Assert.IsTrue(yyy.ID == 678 && yyy.Name == "efg" && yyy.Value == "fgh");
            yyy = clone.YYY[2];
            Assert.IsTrue(yyy.ID == 789 && yyy.Name == "ghi" && yyy.Value == "hij");
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
