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
        }

        [Fact]
        public void ProtoSchema_SupportsNullModelClass()
        {
            AssertSchemaSections<SupportsNullListModel>(
                "message Bar { }",
                "message WrappedBar { group Bar value = 1; }",
                "message SupportsNullListModel { repeated WrappedBar Items = 1;}"
            );      
        }

        [Fact]
        public void SupportsNullModel_ByteOutput()
        {
            var model = SupportsNullListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0B-0A-00-0C-0B-0A-00-0C", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_SupportsNullModel_Success()
        {
            var origin = SupportsNullListModel.BuildWithNull();
            var result = DeepClone(origin);

            Assert.Equal(origin.Items[0], result.Items[0]);
            Assert.Null(result.Items[1]);
            Assert.Equal(origin.Items[2], result.Items[2]);
        }

        [ProtoContract]
        class SupportsNullListModel
        {
            [ProtoMember(1)]
            public List<Bar?> Items { get; set; } = new();

            public static SupportsNullListModel Build() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, new Bar { Id = 2 }
                }
            };

            public static SupportsNullListModel BuildWithNull() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, null, new Bar { Id = 2 }
                }
            };
        }
    }
}
