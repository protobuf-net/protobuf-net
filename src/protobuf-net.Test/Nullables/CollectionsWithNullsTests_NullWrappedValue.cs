using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_NullWrappedValue : CollectionsWithNullsTestsBase
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
        {
            AssertSchemaSections<NullWrappedValueListModel>(
                "message Bar { int32 Id = 1; }",
                "message WrappedBar { optional Bar value = 1; }",
                "message Wrappedint32 { optional int32 value = 1; }",
                "message Wrappedstring { optional string value = 1; }",
                @"message NullWrappedValueListModel 
                { 
                    repeated WrappedBar ClassItems = 1;
                    repeated Wrappedint32 NullableIntItems = 2;
                    repeated Wrappedstring StringItems = 3;
                    repeated Wrappedint32 IntItems = 4 [packed = false];
                }"
            );
        }

        [Fact]
        public void NullWrappedValueListModel_ByteOutput()
        {
            var model = NullWrappedValueListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0A-04-0A-02-08-01-0A-04-0A-02-08-02-12-02-08-01-12-02-08-02-1A-05-0A-03-71-77-65-1A-05-0A-03-72-74-79-22-02-08-01-22-02-08-02", hex);
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
        }

        [ProtoContract]
        public class NullWrappedValueListModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Bar?> ClassItems { get; set; } = new();

            [ProtoMember(2), NullWrappedValue]
            public List<int?> NullableIntItems { get; set; } = new();

            [ProtoMember(3), NullWrappedValue]
            public List<string> StringItems { get; set; } = new();

            [ProtoMember(4), NullWrappedValue]
            public List<int> IntItems { get; set; } = new();

            public static NullWrappedValueListModel Build() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, 2 },
                StringItems = new() { "qwe", "rty" },
                IntItems = new() { 1, 2 }
            };

            public static NullWrappedValueListModel BuildWithNull() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, null, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, null, 2 },
                StringItems = new() { "qwe", null, "rty" },
                IntItems = new() { 1, 2 }
            };
        }
    }
}
