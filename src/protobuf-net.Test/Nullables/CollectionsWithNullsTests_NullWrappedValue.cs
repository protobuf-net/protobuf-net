using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_NullWrappedValue : NullablesTestsBase
    {
        // 4: FooWithAttributes2 [NullWrappedValue], * not touching* SupportNull at all
        //  4a.schema has new wrapper layer, "message Foo { repeated NullWrappedBar Items = 1; }" // naming is hard, with "Bar value = 1" **valid** syntax
        //  4b.payload has the extra layer with "length prefix"
        //  4c. null works correctly! 

        public CollectionsWithNullsTests_NullWrappedValue(ITestOutputHelper log) 
            : base(log)
        {
        }

        [Fact]
        public void ProtoSchema_NullWrappedValueListModel() 
            => AssertSchemaSections<NullWrappedValueListModel>(
@"syntax = ""proto3"";

message Bar {
   int32 Id = 1;
}
message WrappedBar {
   optional Bar value = 1;
}
message Wrappedint32 {
   optional int32 value = 1;
}
message Wrappedstring {
   optional string value = 1;
}
message Wrappeduint32 {
   optional uint32 value = 1;
}
message Wrappedbool {
   optional bool value = 1;
}
message Wrappeddouble {
   optional double value = 1;
}
message Wrappedint64 {
   optional int64 value = 1;
}
message Wrappeduint64 {
   optional uint64 value = 1;
}
message NullWrappedValueListModel {
   repeated WrappedBar ClassItems = 1;
   repeated Wrappedint32 NullableIntItems = 2;
   repeated Wrappedstring StringItems = 3;
   repeated Wrappedint32 IntItems = 4 [packed = false];
   repeated Wrappeduint32 CharItems = 5 [packed = false];
   repeated Wrappedbool BoolItems = 6 [packed = false];
   repeated Wrappeddouble DoubleItems = 7 [packed = false];
   repeated Wrappeduint32 ByteItems = 8 [packed = false];
   repeated Wrappedint64 LongItems = 9 [packed = false];
   repeated Wrappedint32 ShortItems = 10 [packed = false];
   repeated Wrappeduint32 UShortItems = 11 [packed = false];
   repeated Wrappeduint32 UIntItems = 12 [packed = false];
   repeated Wrappeduint64 ULongItems = 13 [packed = false];
}");

        [Fact]
        public void NullWrappedValueListModel_ByteOutput()
        {
            var model = NullWrappedValueListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal(
                "0A-04-0A-02-08-01-0A-04-0A-02-08-02-12-02-08-01-12-02-08-02-1A-05-0A-03-71-77-65-1A-05-0A-03-72-74-79-22-02-08-01-22-02-08-02-2A-02-08-31-2A-02-08-32-32-02-08-01-32-02-08-00-3A-09-09-00-00-00-00-00-00-F0-3F-3A-09-09-CD-CC-CC-CC-CC-CC-00-40-42-02-08-01-42-02-08-02-4A-02-08-01-4A-02-08-02-52-02-08-01-52-02-08-02-5A-02-08-01-5A-02-08-02-62-02-08-01-62-02-08-02-6A-02-08-01-6A-02-08-02", 
                hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_NullWrappedValueListModel_Success()
        {
            var origin = NullWrappedValueListModel.BuildWithNull();
            var result = DeepClone(origin);

            AssertCollectionEquality(origin.ClassItems, result.ClassItems);
            AssertCollectionEquality(origin.NullableIntItems, result.NullableIntItems);
            AssertCollectionEquality(origin.StringItems, result.StringItems);
            AssertCollectionEquality(origin.IntItems, result.IntItems);
            AssertCollectionEquality(origin.CharItems, result.CharItems);
            AssertCollectionEquality(origin.BoolItems, result.BoolItems);
            AssertCollectionEquality(origin.DoubleItems, result.DoubleItems);
            AssertCollectionEquality(origin.ByteItems, result.ByteItems);
            AssertCollectionEquality(origin.LongItems, result.LongItems);
            AssertCollectionEquality(origin.ShortItems, result.ShortItems);
            AssertCollectionEquality(origin.UShortItems, result.UShortItems);
            AssertCollectionEquality(origin.UIntItems, result.UIntItems);
            AssertCollectionEquality(origin.ULongItems, result.ULongItems);
        }

        [ProtoContract]
        public class NullWrappedValueListModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Bar> ClassItems { get; set; } = new();

            [ProtoMember(2), NullWrappedValue]
            public List<int?> NullableIntItems { get; set; } = new();

            [ProtoMember(3), NullWrappedValue]
            public List<string> StringItems { get; set; } = new();

            [ProtoMember(4), NullWrappedValue]
            public List<int> IntItems { get; set; } = new();

            [ProtoMember(5), NullWrappedValue]
            public List<char> CharItems { get; set; } = new();

            [ProtoMember(6), NullWrappedValue]
            public List<bool> BoolItems { get; set; } = new();

            [ProtoMember(7), NullWrappedValue]
            public List<double> DoubleItems { get; set; } = new();

            [ProtoMember(8), NullWrappedValue]
            public List<byte> ByteItems { get; set; } = new();

            [ProtoMember(9), NullWrappedValue]
            public List<long> LongItems { get; set; } = new();

            [ProtoMember(10), NullWrappedValue]
            public List<short> ShortItems { get; set; } = new();

            [ProtoMember(11), NullWrappedValue]
            public List<ushort> UShortItems { get; set; } = new();

            [ProtoMember(12), NullWrappedValue]
            public List<uint> UIntItems { get; set; } = new();

            [ProtoMember(13), NullWrappedValue]
            public List<ulong> ULongItems { get; set; } = new();

            public static NullWrappedValueListModel Build() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, 2 },
                StringItems = new() { "qwe", "rty" },
                IntItems = new() { 1, 2 },
                CharItems = new() { '1', '2' },
                BoolItems = new() { true, false },
                DoubleItems = new() { 1.0, 2.1 },
                ByteItems = new() { 1, 2 },
                LongItems = new() { 1, 2 },
                ShortItems = new() { 1, 2 },
                UShortItems = new() { 1, 2 },
                UIntItems = new() { 1, 2 },
                ULongItems = new() { 1, 2 }
            };

            public static NullWrappedValueListModel BuildWithNull() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, null, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, null, 2 },
                StringItems = new() { "qwe", null, "rty" },
                IntItems = new() { 1, 2 },
                CharItems = new() { '1', '2' },
                BoolItems = new() { true, false },
                DoubleItems = new() { 1.0, 2.1 },
                ByteItems = new() { 1, 2 },
                LongItems = new() { 1, 2 },
                ShortItems = new() { 1, 2 },
                UShortItems = new() { 1, 2 },
                UIntItems = new() { 1, 2 },
                ULongItems = new() { 1, 2 }
            };
        }
    }
}
