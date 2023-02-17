using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_NullWrappedValueGroup : NullablesTestsBase
    {
        // 3: FooWithAttributes [NullWrappedValue(AsGroup = true)],
        // * not touching* SupportNull at all ** works exactly like [SupportNull]**

        public CollectionsWithNullsTests_NullWrappedValueGroup(ITestOutputHelper log) 
            : base(log)
        {
        }

        [Fact]
        public void ProtoSchema_NullWrappedValueGroupListModel() 
            => AssertSchemaSections<NullWrappedValueGroupListModel>(
@"syntax = ""proto3"";

message Bar {
   int32 Id = 1;
}
message WrappedAsGroupBar {
   optional Bar value = 1;
}
message WrappedAsGroupint32 {
   optional int32 value = 1;
}
message WrappedAsGroupstring {
   optional string value = 1;
}
message NullWrappedValueGroupListModel {
   repeated group WrappedAsGroupBar ClassItems = 1;
   repeated group WrappedAsGroupint32 NullableIntItems = 2;
   repeated group WrappedAsGroupstring StringItems = 3;
   repeated group WrappedAsGroupint32 IntItems = 4 [packed = false];
}");

        [Fact]
        public void NullWrappedValueGroupListModel_ByteOutput()
        {
            var model = NullWrappedValueGroupListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0B-0A-02-08-01-0C-0B-0A-02-08-02-0C-13-08-01-14-13-08-02-14-1B-0A-03-71-77-65-1C-1B-0A-03-72-74-79-1C-23-08-01-24-23-08-02-24", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_NullWrappedValueGroupListModel_Success()
        {
            var origin = NullWrappedValueGroupListModel.BuildWithNull();
            var result = DeepClone(origin);

            AssertCollectionEquality(origin.ClassItems, result.ClassItems);
            AssertCollectionEquality(origin.NullableIntItems, result.NullableIntItems);
            AssertCollectionEquality(origin.StringItems, result.StringItems);
            AssertCollectionEquality(origin.IntItems, result.IntItems);
        }

        [ProtoContract]
        public class NullWrappedValueGroupListModel
        {
            [ProtoMember(1), NullWrappedValue(AsGroup = true)]
            public List<Bar> ClassItems { get; set; } = new();

            [ProtoMember(2), NullWrappedValue(AsGroup = true)]
            public List<int?> NullableIntItems { get; set; } = new();

            [ProtoMember(3), NullWrappedValue(AsGroup = true)]
            public List<string> StringItems { get; set; } = new();

            [ProtoMember(4), NullWrappedValue(AsGroup = true)]
            public List<int> IntItems { get; set; } = new();

            public static NullWrappedValueGroupListModel Build() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, 2 },
                StringItems = new() { "qwe", "rty" },
                IntItems = new() { 1, 2 }
            };

            public static NullWrappedValueGroupListModel BuildWithNull() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, null, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, null, 2 },
                StringItems = new() { "qwe", null, "rty" },
                IntItems = new() { 1, 2 }
            };
        }
    }
}
