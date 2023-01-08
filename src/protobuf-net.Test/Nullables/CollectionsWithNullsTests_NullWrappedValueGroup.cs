using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_NullWrappedValueGroup : CollectionsWithNullsTestsBase
    {
        // 3: FooWithAttributes [NullWrappedValue(AsGroup = true)],
        // * not touching* SupportNull at all ** works exactly like [SupportNull]**

        public CollectionsWithNullsTests_NullWrappedValueGroup(ITestOutputHelper log) 
            : base(log)
        {
        }

        [Fact]
        public void ProtoSchema_NullWrappedValueGroupListModel()
        {
            AssertSchemaSections<NullWrappedValueGroupListModel>(
                "message Bar { }",
                "message WrappedBar { group Bar value = 1; }",
                "message NullWrappedValueGroupListModel { repeated WrappedBar Items = 1;}"
            );
        }

        [Fact]
        public void NullWrappedValueGroupListModel_ByteOutput()
        {
            var model = NullWrappedValueGroupListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0B-0A-00-0C-0B-0A-00-0C", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_NullWrappedValueGroupListModel_Success()
        {
            var origin = NullWrappedValueGroupListModel.BuildWithNull();
            var result = DeepClone(origin);

            Assert.Equal(origin.Items[0], result.Items[0]);
            Assert.Null(result.Items[1]);
            Assert.Equal(origin.Items[2], result.Items[2]);
        }

        [ProtoContract]
        public class NullWrappedValueGroupListModel
        {
            [ProtoMember(1), NullWrappedValue(AsGroup = true)]
            public List<Bar?> Items { get; set; } = new();

            public static NullWrappedValueGroupListModel Build() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, new Bar { Id = 2 }
                }
            };

            public static NullWrappedValueGroupListModel BuildWithNull() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, null, new Bar { Id = 2 }
                }
            };
        }
    }
}
