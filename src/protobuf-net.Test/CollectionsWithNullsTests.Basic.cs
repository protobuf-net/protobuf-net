using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    // 1. if model not tweaked with SupportNull, then: 
    //  1a. schema is just "message Foo { repeated Bar Items = 1; }"
    //  1b. payload output has no extra layer, i.e. (in bytes) "field 1, length prefix, for each item"
    //  1c. fails with null values
    public partial class CollectionsWithNullsTests
    {
        [Fact]
        public void ProtoSchema_BasicNullableModel()
        {
            AssertSchemaSections<BasicNullableListModel>(
                "message Bar { }",
                "message BasicNullableListModel { repeated Bar Items = 1; }"
            );
        }

        [Fact]
        public void BasicNullableModel_ByteOutput()
        {
            var model = BasicNullableListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("0A-00-0A-00", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_BasicNullableModel_Fails()
        {
            var model = BasicNullableListModel.BuildWithNull();
            var ms = new MemoryStream();
            Assert.Throws<System.NullReferenceException>(() => Serializer.Serialize(ms, model));
        }

        [ProtoContract]
        public class BasicNullableListModel
        {
            [ProtoMember(1)]
            public List<Bar?> Items { get; set; } = new();

            public static BasicNullableListModel BuildWithNull() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, null, new Bar { Id = 2 }
                }
            };

            public static BasicNullableListModel Build() => new()
            {
                Items = new()
                {
                    new Bar { Id = 1 }, new Bar { Id = 2 }
                }
            };
        }
    }
}
