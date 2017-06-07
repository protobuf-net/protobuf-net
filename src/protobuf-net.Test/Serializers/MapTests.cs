using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace ProtoBuf.Serializers
{
    [Trait("kind", "maps")]
    public class MapTests
    {
        [Fact]
        public void RoundTripBasic()
        {
            var data = new HazMap
            {
                Lookup =
                {
                    {1, "abc" }, {0, "" }, {2, "def"}
                }
            };
            var clone = Serializer.DeepClone(data);
            Assert.Equal(3, clone.Lookup.Count);
            Assert.Equal("", clone.Lookup[0]);
            Assert.Equal("abc", clone.Lookup[1]);
            Assert.Equal("def", clone.Lookup[2]);


            using (var ms = new MemoryStream())
            {
                
                Serializer.Serialize(ms, new HazMapEquiv
                {
                    Lookup =
                    {
                        new HazMapEquiv.Entry { Key = 1, Value = "abc" },
                        new HazMapEquiv.Entry { },
                        new HazMapEquiv.Entry { Key = 2, Value = "def" },
                    }
                });
                var expectedHex = BitConverter.ToString(ms.ToArray());

                ms.Position = 0;
                ms.SetLength(0);
                Serializer.Serialize(ms, data);
                var actualHex = BitConverter.ToString(ms.ToArray());

                Assert.Equal(expectedHex, actualHex);

            }

            Assert.True(RuntimeTypeModel.Default[typeof(HazMap)][3].IsMap);
            Assert.Equal(DataFormat.Default, RuntimeTypeModel.Default[typeof(HazMap)][3].MapKeyFormat);
            Assert.Equal(DataFormat.Default, RuntimeTypeModel.Default[typeof(HazMap)][3].MapValueFormat);
        }

        [Fact]
        public void GetMapSchema()
        {
            var schema = Serializer.GetProto<HazMap>();
            Assert.Equal(@"package ProtoBuf.Serializers;

message HazMap {
   map<int32,string> Lookup = 3;
}
", schema);
        }

        [Fact]
        public void GetMapWithDataFormatSchema()
        {
            var schema = Serializer.GetProto<HazMapWithDataFormat>();
            Assert.Equal(@"package ProtoBuf.Serializers;

message HazMapWithDataFormat {
   map<sint32,sint64> Lookup = 3;
}
", schema);
        }

        [Fact]
        public void RoundTripWithDataFormat()
        {
            var data = new HazMapWithDataFormat
            {
                Lookup =
                {
                    {1, 42 }, {0, 0 }, {2, -8}
                }
            };
            var clone = Serializer.DeepClone(data);
            Assert.Equal(3, clone.Lookup.Count);
            Assert.Equal(0, clone.Lookup[0]);
            Assert.Equal(42, clone.Lookup[1]);
            Assert.Equal(-8, clone.Lookup[2]);


            using (var ms = new MemoryStream())
            {

                Serializer.Serialize(ms, new HazMapWithDataFormatEquiv
                {
                    Lookup =
                    {
                        new HazMapWithDataFormatEquiv.Entry { Key = 1, Value = 42 },
                        new HazMapWithDataFormatEquiv.Entry { },
                        new HazMapWithDataFormatEquiv.Entry { Key = 2, Value = -8 },
                    }
                });
                var expectedHex = BitConverter.ToString(ms.ToArray());

                ms.Position = 0;
                ms.SetLength(0);
                Serializer.Serialize(ms, data);
                var actualHex = BitConverter.ToString(ms.ToArray());

                Assert.Equal(expectedHex, actualHex);

            }

            Assert.True(RuntimeTypeModel.Default[typeof(HazMapWithDataFormat)][3].IsMap);
            Assert.Equal(DataFormat.ZigZag, RuntimeTypeModel.Default[typeof(HazMapWithDataFormat)][3].MapKeyFormat);
            Assert.Equal(DataFormat.ZigZag, RuntimeTypeModel.Default[typeof(HazMapWithDataFormat)][3].MapValueFormat);
        }
        [ProtoContract]
        public class HazMap
        {
            [ProtoMember(3), ProtoMap]
            public Dictionary<int, string> Lookup { get; } = new Dictionary<int, string>();
        }

        [ProtoContract]
        public class HazMapEquiv
        {
            [ProtoMember(3)]
            public List<Entry> Lookup = new List<Entry>();
            [ProtoContract]
            public class Entry
            {
                [ProtoMember(1)]
                public int? Key { get; set; }
                [ProtoMember(2)]
                public string Value { get; set; }
            }
        }

        [ProtoContract]
        public class HazMapWithDataFormat
        {
            [ProtoMember(3), ProtoMap(KeyFormat = DataFormat.ZigZag, ValueFormat = DataFormat.ZigZag)]
            public Dictionary<int, long> Lookup { get; } = new Dictionary<int, long>();
        }

        [ProtoContract]
        public class HazMapWithDataFormatEquiv
        {
            [ProtoMember(3)]
            public List<Entry> Lookup = new List<Entry>();
            [ProtoContract]
            public class Entry
            {
                [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
                public int? Key { get; set; }
                [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
                public long? Value { get; set; }
            }
        }
    }
}
