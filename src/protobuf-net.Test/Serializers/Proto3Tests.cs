
using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace ProtoBuf.Serializers
{
    [Trait("kind", "proto3")]
    public class Proto3Tests
    {

        [Fact]
        public void HazBasicEnum_Schema()
        {
            var schema = Serializer.GetProto<HazBasicEnum>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazBasicEnum {
   optional RegularEnum Value = 1 [default = A];
}
enum RegularEnum {
   A = 0;
   B = 1;
   C = 2;
}
", schema);
        }
        [Fact]
        public void HazBasicEnum_WorksForKnownAndUnknownValues()
        {
            var obj = Serializer.ChangeType<HazInteger, HazBasicEnum>(new HazInteger { Value = 0 });
            Assert.Equal(RegularEnum.A, obj.Value);

            obj = Serializer.ChangeType<HazInteger, HazBasicEnum>(new HazInteger { Value = 1 });
            Assert.Equal(RegularEnum.B, obj.Value);

            obj = Serializer.ChangeType<HazInteger, HazBasicEnum>(new HazInteger { Value = 5 });
            Assert.Equal((RegularEnum)5, obj.Value);
        }

        [ProtoContract]
        public class HazInteger
        {
            [ProtoMember(1, IsRequired = true)]
            public int Value { get; set; }
        }

        [ProtoContract]
        public class HazBasicEnum
        {
            [ProtoMember(1)]
            public RegularEnum Value { get; set; }
        }
        public enum RegularEnum
        {
            A, B, C
        }


        [Fact]
        public void HazStrictEnum_Schema()
        {
            var schema = Serializer.GetProto<HazStrictEnum>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazStrictEnum {
   optional StrictEnum Value = 1 [default = A];
}
enum StrictEnum {
   A = 0;
   B = 1;
   C = 2;
}
", schema);
        }
        [Fact]
        public void HazStrictEnum_WorksForKnownAndUnknownValues()
        {
            var obj = Serializer.ChangeType<HazInteger, HazStrictEnum>(new HazInteger { Value = 0 });
            Assert.Equal(StrictEnum.A, obj.Value);

            obj = Serializer.ChangeType<HazInteger, HazStrictEnum>(new HazInteger { Value = 1 });
            Assert.Equal(StrictEnum.B, obj.Value);

            var ex = Assert.Throws<ProtoException>(() =>
            {
                obj = Serializer.ChangeType<HazInteger, HazStrictEnum>(new HazInteger { Value = 5 });
            });
            Assert.Equal("No ProtoBuf.Serializers.Proto3Tests+StrictEnum enum is mapped to the wire-value 5", ex.Message);
        }

        [ProtoContract]
        public class HazStrictEnum
        {
            [ProtoMember(1)]
            public StrictEnum Value { get; set; }
        }
        [ProtoContract(EnumPassthru = false)]
        public enum StrictEnum
        {
            A, B, C
        }


        [Fact]
        public void HazCustomMappedEnum_Schema()
        {
            var schema = Serializer.GetProto<HazCustomMappedEnum>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazCustomMappedEnum {
   optional MappedEnum Value = 1 [default = A];
}
enum MappedEnum {
   B = 0;
   A = 1;
   C = 2;
}
", schema);
        }
        [Fact]
        public void HazCustomMappedEnum_WorksForKnownAndUnknownValues()
        {
            var obj = Serializer.ChangeType<HazInteger, HazCustomMappedEnum>(new HazInteger { Value = 0 });
            Assert.Equal(MappedEnum.B, obj.Value);

            obj = Serializer.ChangeType<HazInteger, HazCustomMappedEnum>(new HazInteger { Value = 1 });
            Assert.Equal(MappedEnum.A, obj.Value);

            var ex = Assert.Throws<ProtoException>(() =>
            {
                obj = Serializer.ChangeType<HazInteger, HazCustomMappedEnum>(new HazInteger { Value = 5 });
            });
            Assert.Equal("No ProtoBuf.Serializers.Proto3Tests+MappedEnum enum is mapped to the wire-value 5", ex.Message);
        }

        [ProtoContract]
        public class HazCustomMappedEnum
        {
            [ProtoMember(1)]
            public MappedEnum Value { get; set; }
        }

        public enum MappedEnum
        {
            [ProtoEnum(Value = 1)]
            A,
            [ProtoEnum(Value = 0)]
            B,
            C
        }

        [Fact]
        public void HazAliasedEnum_Schema()
        {
            var schema = Serializer.GetProto<HazAliasedEnum>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

enum AliasedEnum {
   option allow_alias = true;
   A = 0;
   B = 1;
   C = 1;
}
message HazAliasedEnum {
   optional AliasedEnum Value = 1 [default = A];
}
", schema);
        }
        [Fact]
        public void HazAliasedEnum_WorksForKnownAndUnknownValues()
        {
            var obj = Serializer.ChangeType<HazInteger, HazAliasedEnum>(new HazInteger { Value = 0 });
            Assert.Equal(AliasedEnum.A, obj.Value);

            obj = Serializer.ChangeType<HazInteger, HazAliasedEnum>(new HazInteger { Value = 1 });
            Assert.Equal(AliasedEnum.C, obj.Value); // coube be B - identical

            obj = Serializer.ChangeType<HazInteger, HazAliasedEnum>(new HazInteger { Value = 5 });
            Assert.Equal((AliasedEnum)5, obj.Value);
        }

        [ProtoContract]
        public class HazAliasedEnum
        {
            [ProtoMember(1)]
            public AliasedEnum Value { get; set; }
        }

        public enum AliasedEnum
        {
            A = 0,
            B = 1,
            C = 1
        }

        [Fact]
        public void CompileHazMap() => Compile<HazMap>();

        [Fact]
        public void CompileHazImplicitMap() => Compile<HazImplicitMap>();

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

        [ProtoContract]
        class HasEvilDictionary_Array
        {
            [ProtoMember(1)]
            public Dictionary<int, int[]> SourceOfProblem { get; set; }
        }

        [ProtoContract]
        class HasEvilDictionary_List
        {
            [ProtoMember(1)]
            public Dictionary<int, List<int>> SourceOfProblem { get; set; }
        }

        [Fact]
        public void ComplexMapShouldNotBreak_Array()
        {
            var obj = new HasEvilDictionary_Array();
            var model = RuntimeTypeModel.Create();
            model.DeepClone(obj);

            var arr = new int[] { 1, 2, 3 };
            var arr2 = (int[])model.DeepClone(arr);
            Assert.Equal("1,2,3", string.Join(",", arr2));
        }

        [Fact]
        public void ComplexMapShouldNotBreak_List()
        {
            var obj = new HasEvilDictionary_List();
            var model = RuntimeTypeModel.Create();
            model.DeepClone(obj);

            var list = new List<int> { 1, 2, 3 };
            var list2 = (List<int>) model.DeepClone(list);
            Assert.Equal("1,2,3", string.Join(",", list2));
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
        public void GetDynamicAsRefSchema()
        {
            var schema = Serializer.GetProto<HasRefDynamic>();
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""protobuf-net/protogen.proto""; // custom protobuf-net options

message HasRefDynamic {
   optional .bcl.NetObjectProxy Obj = 1 [(.protobuf_net.fieldopt).asRef = true, (.protobuf_net.fieldopt).dynamicType = true];
}
", schema);
        }
        [ProtoContract]
        public class HasRefDynamic
        {
            [ProtoMember(1, AsReference = true, DynamicType = true)]
            public HasRefDynamic Obj { get; set; }
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
        public void TestEnumProto_Proto2_RuntimeRenamed()
        {
            var model = TypeModel.Create();
            model[typeof(HazEnum.SomeEnum)][1].Name = "zzz";
            var schema = model.GetSchema(typeof(HazEnum), ProtoSyntax.Proto2);
            Assert.Equal(@"syntax = ""proto2"";
package ProtoBuf.Serializers;

message HazEnum {
   optional SomeEnum X = 1 [default = B];
}
enum SomeEnum {
   B = 0;
   zzz = 1;
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
            public Dictionary<int, string> Lookup { get; set; } = new Dictionary<int, string>();
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
            [ProtoMember(3)] // leave this as {get;} = ... for the backing field test
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

        [Fact]
        public void ImplicitMapLastMapWins()
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, new ListKVP
            {
                Items = {
                    new KeyValuePair<int, string>(1, "abc"),
                    new KeyValuePair<int, string>(2, "def"),
                    new KeyValuePair<int, string>(3, "ghi"),
                    new KeyValuePair<int, string>(2, "jkl"),
                    new KeyValuePair<int, string>(1, "mno"),
                }
            });
            ms.Position = 0;
            var obj = Serializer.Deserialize<ImplicitMap>(ms);
            Assert.Equal(3, obj.Items.Count);
            Assert.Equal("mno", obj.Items[1]);
            Assert.Equal("jkl", obj.Items[2]);
            Assert.Equal("ghi", obj.Items[3]);
        }

        [Fact]
        public void CompileImplicitMap() => Compile<ImplicitMap>();

        private static void Compile<T>([CallerMemberName] string name = null, bool deleteOnSuccess = true)
        {
            var model = TypeModel.Create();
            model.Add(typeof(T), true);
            var path = Path.ChangeExtension(name, "dll");
            if (File.Exists(path)) File.Delete(path);
            model.Compile(name, path);
            PEVerify.Verify(path, 0, deleteOnSuccess);
        }

        [Fact]
        public void ExplicitMapLastMapWins()
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, new ListKVP
            {
                Items = {
                    new KeyValuePair<int, string>(1, "abc"),
                    new KeyValuePair<int, string>(2, "def"),
                    new KeyValuePair<int, string>(3, "ghi"),
                    new KeyValuePair<int, string>(2, "jkl"),
                    new KeyValuePair<int, string>(1, "mno"),
                }
            });
            ms.Position = 0;
            var obj = Serializer.Deserialize<ExplicitMap>(ms);
            Assert.Equal(3, obj.Items.Count);
            Assert.Equal("mno", obj.Items[1]);
            Assert.Equal("jkl", obj.Items[2]);
            Assert.Equal("ghi", obj.Items[3]);
        }

        [Fact]
        public void DisabledMapFails()
        {
            var ms = new MemoryStream();
            Serializer.Serialize(ms, new ListKVP
            {
                Items = {
                    new KeyValuePair<int, string>(1, "abc"),
                    new KeyValuePair<int, string>(2, "def"),
                    new KeyValuePair<int, string>(3, "ghi"),
                    new KeyValuePair<int, string>(2, "jkl"),
                    new KeyValuePair<int, string>(1, "mno"),
                }
            });
            ms.Position = 0;
            var ex = Assert.Throws<ArgumentException>(() =>
                Serializer.Deserialize<DisabledMap>(ms)
            );
            Assert.StartsWith("An item with the same key has already been added.", ex.Message);
        }

        [ProtoContract]
        public class ListKVP
        {
            [ProtoMember(1)]
            public List<KeyValuePair<int, string>> Items { get; }
                = new List<KeyValuePair<int, string>>();
        }

        [ProtoContract]
        public class ImplicitMap
        {
            [ProtoMember(1, OverwriteList = true), ProtoMap]
            public Dictionary<int, string> Items { get; set; }
                = new Dictionary<int, string>();
        }
        [ProtoContract]
        public class ExplicitMap
        {
            [ProtoMember(1), ProtoMap]
            public Dictionary<int, string> Items { get; }
                = new Dictionary<int, string>();
        }
        [ProtoContract]
        public class DisabledMap
        {
            [ProtoMember(1)]
            [ProtoMap(DisableMap = true)]
            public Dictionary<int, string> Items { get; }
                = new Dictionary<int, string>();
        }
    }
}


