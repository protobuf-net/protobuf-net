using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    // https://github.com/protobuf-net/protobuf-net.Grpc/issues/282
    public class Grpc282
    {
        [Fact]
        public void Execute()
        {
            var model = RuntimeTypeModel.Create();
            IntPtrSerializer.Configure(model);

            using var ms = new MemoryStream();
            model.Serialize(ms, new setCommCellIdRequest
            {
                m_pEvAlertObj = 42
            });
            if (!ms.TryGetBuffer(out var buffer)) buffer = new(ms.ToArray());
            var hex = BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count);
            Assert.Equal("10-2A", hex); // Field #2 Varint Value = 42,

            ms.Position = 0;
            var clone = model.Deserialize<setCommCellIdRequest>(ms);
            Assert.Equal(42, clone.m_pEvAlertObj);
        }

        [ProtoContract]
        public class setCommCellIdRequest
        {
            [ProtoMember(1)]
            public Int64 commCellId { get; set; }

            [ProtoMember(2)]
            public nint m_pEvAlertObj { get; set; }

            [ProtoMember(3)]
            public Boolean isDisposed { get; set; }
        }

        public sealed class IntPtrSerializer : ISerializer<nint>, ISerializer<nuint>
        {
            public static void Configure(RuntimeTypeModel model)
            {
                model.Add<nint>(false).SerializerType = typeof(IntPtrSerializer);
                model.Add<nuint>(false).SerializerType = typeof(IntPtrSerializer);
            }

            public SerializerFeatures Features
                => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
            public nint Read(ref ProtoReader.State state, IntPtr value)
                => new IntPtr(state.ReadInt64());
            public UIntPtr Read(ref ProtoReader.State state, UIntPtr value)
                => new UIntPtr(state.ReadUInt64());
            public void Write(ref ProtoWriter.State state, IntPtr value)
                => state.WriteInt64(value.ToInt64());
            public void Write(ref ProtoWriter.State state, UIntPtr value)
                => state.WriteUInt64(value.ToUInt64());
        }
    }
}
