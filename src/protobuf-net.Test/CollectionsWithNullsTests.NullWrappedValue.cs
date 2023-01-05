using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Test
{
    public partial class CollectionsWithNullsTests
    {
        [Fact]
        public void ProtoSchema_NullWrappedValueListModel()
        {
            _log.WriteLine(Serializer.GetProto<NullWrappedValueListModel>());

            AssertSchemaSections<NullWrappedValueListModel>(
                "message Bar { }",
                "message NullWrappedValueListModel { repeated Bar Items = 1; }"
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
            var model = SupportsNullListModel.BuildWithNull();
            var ms = new MemoryStream();
            Assert.Throws<System.NullReferenceException>(() => Serializer.Serialize(ms, model));
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
