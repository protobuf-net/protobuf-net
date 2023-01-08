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
                "message Bar { }",
                "message WrappedBar { Bar value = 1; }",
                "message NullWrappedValueListModel { repeated WrappedBar Items = 1;}"
            );
        }

        [Fact]
        public void NullWrappedValueListModel_ByteOutput()
        {
            var model = NullWrappedValueListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0A-02-0A-00-0A-02-0A-00", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_NullWrappedValueListModel_Success()
        {
            var origin = NullWrappedValueListModel.BuildWithNull();
            var result = DeepClone(origin);

            Assert.Equal(origin.Items[0], result.Items[0]);
            Assert.Null(result.Items[1]);
            Assert.Equal(origin.Items[2], result.Items[2]);
        }

        [ProtoContract]
        public class NullWrappedValueListModel
        {
            [ProtoMember(1), NullWrappedValue]
            public List<Bar> Items { get; set; } = new();

            public static NullWrappedValueListModel Build() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, new Bar { Id = 2 }
                }
            };

            public static NullWrappedValueListModel BuildWithNull() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, null, new Bar { Id = 2 }
                }
            };
        }
    }
}
