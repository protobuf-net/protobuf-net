using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ProtoBuf.unittest.Serializers
{
    public class ISetTests
    {
        [Fact]
        public void ISetCollectionTest()
        {
            using (var writeStream = new System.IO.MemoryStream())
            {
                ISet<string> mySet = new HashSet<string>
                {
                    "hello world"
                };

                Serializer.Serialize(writeStream, mySet);

                using (var readStream = new System.IO.MemoryStream(writeStream.ToArray()))
                {
                    var myDeserializedSet = ProtoBuf.Serializer.Deserialize<ISet<string>>(readStream);
                    Assert.Equal(mySet.First(), myDeserializedSet.First());
                    Assert.Equal("hello world", mySet.First());
                }
            }

            using (var writeStream = new System.IO.MemoryStream())
            {
                IDictionary<DayOfWeek, ISet<string>> myMap = new Dictionary<DayOfWeek, ISet<string>>
                {
                    { DayOfWeek.Monday, new HashSet<string> { "hello world" } }
                };

                Serializer.Serialize(writeStream, myMap);

                using (var readStream = new System.IO.MemoryStream(writeStream.ToArray()))
                {
                    var myDeserializedMap = ProtoBuf.Serializer.Deserialize<IDictionary<DayOfWeek, ISet<string>>>(readStream);
                    Assert.Equal(myMap[DayOfWeek.Monday].First(), myDeserializedMap[DayOfWeek.Monday].First());
                    Assert.Equal("hello world", myMap[DayOfWeek.Monday].First());
                }
            }
        }

        [Fact]
        public void DictionaryWithISetCollectionTest()
        {
            using (var writeStream = new System.IO.MemoryStream())
            {
                IDictionary<DayOfWeek, ISet<string>> myMap = new Dictionary<DayOfWeek, ISet<string>>
                {
                    { DayOfWeek.Monday, new HashSet<string> { "hello world" } }
                };

                Serializer.Serialize(writeStream, myMap);

                using (var readStream = new System.IO.MemoryStream(writeStream.ToArray()))
                {
                    var myDeserializedMap = ProtoBuf.Serializer.Deserialize<IDictionary<DayOfWeek, ISet<string>>>(readStream);
                    Assert.Equal(myMap[DayOfWeek.Monday].First(), myDeserializedMap[DayOfWeek.Monday].First());
                    Assert.Equal("hello world", myMap[DayOfWeek.Monday].First());
                }
            }
        }
    }
}