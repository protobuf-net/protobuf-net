using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    // 4: FooWithAttributes2 [NullWrappedValue], * not touching* SupportNull at all
    //  4a.schema has new wrapper layer, "message Foo { repeated NullWrappedBar Items = 1; }" // naming is hard, with "Bar value = 1" **valid** syntax
    //  4b.payload has the extra layer with "length prefix"
    //  4c. null works correctly! 
    public partial class CollectionsWithNullsTests
    {
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
            Assert.Equal("will-be-defined-later", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_NullWrappedValueListModel_Fails()
        {
            var model = NullWrappedValueListModel.BuildWithNull();
            var ms = new MemoryStream();

            // runs with no exceptions raised
            Serializer.Serialize(ms, model);
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
