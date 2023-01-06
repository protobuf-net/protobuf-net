using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    // 3: FooWithAttributes [NullWrappedValue(AsGroup = true)],
    // * not touching* SupportNull at all ** works exactly like [SupportNull]**
    public partial class CollectionsWithNullsTests
    {
        [Fact]
        public void ProtoSchema_NullWrappedValueGroupListModel()
        {
            _log.WriteLine(Serializer.GetProto<NullWrappedValueGroupListModel>());

            AssertSchemaSections<NullWrappedValueGroupListModel>(
                "group Bar { }",
                "message NullWrappedValueGroupListModel { repeated Bar Items = 1; }"
            );
        }

        [Fact]
        public void NullWrappedValueGroupListModel_ByteOutput()
        {
            var model = SupportsNullListModel.Build();
            var hex = GetSerializationOutputHex(model);
            Assert.Equal("will-be-defined-later", hex);
        }

        [Fact]
        public void ProtoSerializationWithNulls_NullWrappedValueGroupListModel_Fails()
        {
            var model = SupportsNullListModel.BuildWithNull();
            var ms = new MemoryStream();
            Assert.Throws<System.NullReferenceException>(() => Serializer.Serialize(ms, model));
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
