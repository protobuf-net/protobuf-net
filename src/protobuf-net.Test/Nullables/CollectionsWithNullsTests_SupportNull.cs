using ProtoBuf.Meta;
using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_SupportNull : CollectionsWithNullsTestsBase
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
            runtimeTypeModel[typeof(SupportsNullListModel)][1].SupportNull = true;
            runtimeTypeModel[typeof(SupportsNullListModel)][2].SupportNull = true;
            runtimeTypeModel[typeof(SupportsNullListModel)][3].SupportNull = true;
        }

        [Fact]
        public void ProtoSchema_SupportsNullModelClass()
        {
            AssertSchemaSections<SupportsNullListModel>(
                "message Bar { int32 Id = 1; }",
                "message WrappedBar { optional Bar value = 1; }",
                "message Wrappedint32 { optional int32 value = 1; }",
                "message Wrappedstring { optional string value = 1; }",
                @"message SupportsNullListModel 
                { 
                    repeated group WrappedBar ClassItems = 1;
                    repeated group Wrappedint32 NullableIntItems = 2;
                    repeated group Wrappedstring StringItems = 3;
                }"
            );      
        }

        [Fact]
        public void SupportsNullModel_ByteOutput()
        {
            var model = SupportsNullListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0B-0A-02-08-01-0C-0B-0A-02-08-02-0C-13-08-01-14-13-08-02-14-1B-0A-03-71-77-65-1C-1B-0A-03-72-74-79-1C", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_SupportsNullModel_Success()
        {
            var origin = SupportsNullListModel.BuildWithNull();
            var result = DeepClone(origin);

            Assert.Equal(origin.ClassItems[0], result.ClassItems[0]);
            Assert.Null(result.ClassItems[1]);
            Assert.Equal(origin.ClassItems[2], result.ClassItems[2]);
        }

        [ProtoContract]
        class SupportsNullListModel
        {
            [ProtoMember(1)]
            public List<Bar?> ClassItems { get; set; } = new();

            [ProtoMember(2)]
            public List<int?> NullableIntItems { get; set; } = new();

            [ProtoMember(3)]
            public List<string> StringItems { get; set; } = new();

            public static SupportsNullListModel Build() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, 2 },
                StringItems = new() { "qwe", "rty" }
            };

            public static SupportsNullListModel BuildWithNull() => new()
            {
                ClassItems = new() { new Bar { Id = 1 }, null, new Bar { Id = 2 } },
                NullableIntItems = new() { 1, null, 2 },
                StringItems = new() { "qwe", null, "rty" }
            };
        }
    }
}
