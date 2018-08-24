using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;
using ProtoBuf;

namespace Examples.Issues
{
    public class Issue453
    {
        [ProtoContract]
        public class TestIDictionary
        {
            [ProtoMember(1)]
            public IDictionary<long, string> Data { get; set; }


            public TestIDictionary()
            {
                Data = new Dictionary<long, string>();
            }
        }

        [ProtoContract]
        public class TestDictionary
        {
            [ProtoMember(1)]
            public Dictionary<long, string> Data { get; set; }


            public TestDictionary()
            {
                Data = new Dictionary<long, string>();
            }
        }

        [Fact]
        public void IsMapIsTrueForBoth()
        {
            Assert.True(RuntimeTypeModel.Default[typeof(TestDictionary)][1].IsMap);
            Assert.True(RuntimeTypeModel.Default[typeof(TestIDictionary)][1].IsMap);
        }

        [Fact]
        public void RoundtripWireformatSame()
        {
            var data = new TestDictionary()
            {
                Data =
                {
                    {1, "abc" }, {0, "" }, {2, "def"}
                }
            };
            var clone = Serializer.DeepClone(data);
            Assert.Equal(3, clone.Data.Count);
            Assert.Equal("", clone.Data[0]);
            Assert.Equal("abc", clone.Data[1]);
            Assert.Equal("def", clone.Data[2]);


            using (var ms = new MemoryStream())
            {

                Serializer.Serialize(ms, new TestIDictionary()
                {
                    Data =
                    {
                        {1, "abc" }, {0, "" }, {2, "def"}
                    }
                });
                var expectedHex = BitConverter.ToString(ms.ToArray());

                ms.Position = 0;
                ms.SetLength(0);
                Serializer.Serialize(ms, data);
                var actualHex = BitConverter.ToString(ms.ToArray());

                Assert.Equal(expectedHex, actualHex);

            }

        }

    }
}
