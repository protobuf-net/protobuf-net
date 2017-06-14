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
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazMap {
   map<int32,string> Lookup = 3;
}
", schema);
        }

        [Fact]
        public void GetMapWithDataFormatSchema()
        {
            var schema = Serializer.GetProto<HazMapWithDataFormat>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazMapWithDataFormat {
   map<sint32,sint64> Lookup = 3;
}
", schema);
        }

        [Fact]
        public void TimeSchemaTypes()
        {
            var schema = Serializer.GetProto<HazTime>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

message HazTime {
   optional .bcl.DateTime a = 1;
   optional .google.protobuf.Timestamp b = 2;
   optional .bcl.TimeSpan c = 3;
   optional .google.protobuf.Duration d = 4;
}
", schema);
        }

        [ProtoContract]
        public class HazTime
        {
            [ProtoMember(1)]
            public DateTime a { get; set; }
            [ProtoMember(2, DataFormat = DataFormat.WellKnown)]
            public DateTime b { get; set; }
            [ProtoMember(3)]
            public TimeSpan c { get; set; }
            [ProtoMember(4, DataFormat = DataFormat.WellKnown)]
            public TimeSpan d { get; set; }
        }
        [Fact]
        public void TestPackNonPackedSchemas_Proto2()
        {
            var schema = Serializer.GetProto<TestPackNonPackedSchemas>(ProtoSyntax.Proto2);
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message TestPackNonPackedSchemas {
   repeated int32 Packed = 1 [packed = true];
   repeated float NonPacked = 2;
   optional bool Boolean = 3 [default = false];
}
", schema);
        }

        [Fact]
        public void TestPackNonPackedSchemas_Proto3()
        {
            var schema = Serializer.GetProto<TestPackNonPackedSchemas>(ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Serializers;

message TestPackNonPackedSchemas {
   repeated int32 Packed = 1;
   repeated float NonPacked = 2 [packed = false];
   bool Boolean = 3;
}
", schema);
        }

        [Fact]
        public void TestEnumProto_Proto2()
        {
            var schema = Serializer.GetProto<HazEnum>(ProtoSyntax.Proto2);
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazEnum {
   optional SomeEnum X = 1 [default = B];
}
enum SomeEnum {
   B = 0;
   A = 1;
   C = 2;
}
", schema);
        }

        [Fact]
        public void TestEnumProto_Proto3()
        {
            var schema = Serializer.GetProto<HazEnum>(ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Serializers;

message HazEnum {
   SomeEnum X = 1;
}
enum SomeEnum {
   B = 0;
   A = 1;
   C = 2;
}
", schema);
        }

        [ProtoContract]
        public class HazEnum
        {
            [ProtoMember(1)]
            public SomeEnum X { get; set; }
            
            public enum SomeEnum : short
            {
                A = 1, B = 0, C = 2
            }
        }

        [ProtoContract]
        public class TestPackNonPackedSchemas
        {
            [ProtoMember(1, IsPacked = true)]
            public int[] Packed { get; set; }

            [ProtoMember(2, IsPacked = false)]
            public float[] NonPacked { get; set; }

            [ProtoMember(3)]
            public bool Boolean { get; set; }
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
        [Fact]
        public void GetMapSchema_HazImplicitMap()
        {
            var schema = Serializer.GetProto<HazImplicitMap>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazImplicitMap {
   map<int32,string> Lookup = 3;
}
", schema);
        }

        [ProtoContract]
        public class HazImplicitMap
        {
            [ProtoMember(3)]
            public Dictionary<int, string> Lookup { get; } = new Dictionary<int, string>();
        }

        [Fact]
        public void GetMapSchema_HazDisabledMap()
        {
            var schema = Serializer.GetProto<HazDisabledMap>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazDisabledMap {
   repeated KeyValuePair_Int32_String Lookup = 3;
}
message KeyValuePair_Int32_String {
   optional int32 Key = 1;
   optional string Value = 2;
}
", schema);
        }
        [ProtoContract]
        public class HazDisabledMap
        {
            [ProtoMember(3), ProtoMap(DisableMap = true)]
            public Dictionary<int, string> Lookup { get; } = new Dictionary<int, string>();
        }
        [Fact]
        public void GetMapSchema_HazInvalidKeyTypeMap()
        {
            var schema = Serializer.GetProto<HazInvalidKeyTypeMap>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazInvalidKeyTypeMap {
   repeated KeyValuePair_Double_String Lookup = 3;
}
message KeyValuePair_Double_String {
   optional double Key = 1;
   optional string Value = 2;
}
", schema);
        }
        [ProtoContract]
        public class HazInvalidKeyTypeMap
        {
            [ProtoMember(3), ProtoMap(DisableMap = true)]
            public Dictionary<double, string> Lookup { get; } = new Dictionary<double, string>();
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
