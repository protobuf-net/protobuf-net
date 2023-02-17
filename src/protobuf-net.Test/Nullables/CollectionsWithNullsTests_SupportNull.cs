using ProtoBuf.Meta;
using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_SupportNull : NullablesTestsBase
    {
        // 2. if model is tweaked with SupportNull:
        //  2a. schema has new wrapper layer, "message Foo { repeated NullWrappedBar Items = 1; }" 
        //      with "group Bar value = 1" **invalid** syntax
        //  2b. payload has the extra layer with "group"
        //  2c. null works correctly!

        public CollectionsWithNullsTests_SupportNull(ITestOutputHelper log) 
            : base(log)
        {
        }

        protected override void SetupRuntimeTypeModel(RuntimeTypeModel runtimeTypeModel)
        {
            MarkTypeFieldsAsSupportNull<SupportsNullListModel>();
        }

        [Fact]
        public void ProtoSchema_SupportsNullModelClass() 
            => AssertSchemaSections<SupportsNullListModel>(
@"syntax = ""proto3"";

message Bar {
   int32 Id = 1;
}
message WrappedAsSupportNullBar {
   optional Bar value = 1;
}
message WrappedAsSupportNullint32 {
   optional int32 value = 1;
}
message WrappedAsSupportNullstring {
   optional string value = 1;
}
message SupportsNullListModel {
   repeated group WrappedAsSupportNullBar ClassItems = 1;
   repeated group WrappedAsSupportNullint32 NullableIntItems = 2;
   repeated group WrappedAsSupportNullstring StringItems = 3;
   repeated group WrappedAsSupportNullint32 IntItems = 4 [packed = false];
}");

        [Fact]
        public void SupportsNullModel_ByteOutput()
        {
            var model = SupportsNullListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0B-0A-02-08-01-0C-0B-0A-02-08-02-0C-13-08-01-14-13-08-02-14-1B-0A-03-71-77-65-1C-1B-0A-03-72-74-79-1C-23-08-01-24-23-08-02-24", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_SupportsNullModel_Success()
        {
            var origin = SupportsNullListModel.BuildWithNull();
            var result = DeepClone(origin);

            AssertCollectionEquality(origin.ClassItems, result.ClassItems);
            AssertCollectionEquality(origin.NullableIntItems, result.NullableIntItems);
            AssertCollectionEquality(origin.StringItems, result.StringItems);
            AssertCollectionEquality(origin.IntItems, result.IntItems);
        }

        [ProtoContract]
        class SupportsNullListModel
        {
            [ProtoMember(1)]
            public List<Bar> ClassItems { get; set; } = new();

            [ProtoMember(2)]
            public List<int?> NullableIntItems { get; set; } = new();

            [ProtoMember(3)]
            public List<string> StringItems { get; set; } = new();

            [ProtoMember(4)]
            public List<int> IntItems { get; set; } = new();

            public static SupportsNullListModel Build() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, 2 },
                StringItems = new() { "qwe", "rty" },
                IntItems = new() { 1, 2 }
            };

            public static SupportsNullListModel BuildWithNull() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, null, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, null, 2 },
                StringItems = new() { "qwe", null, "rty" },
                IntItems = new() { 1, 2 }
            };
        }
    }
}
