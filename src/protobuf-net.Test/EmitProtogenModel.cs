using Google.Protobuf.Reflection;
using ProtoBuf.Meta;
using ProtoBuf.Reflection;
using ProtoBuf.unittest;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf
{
    public class EmitProtogenModel
    {
        [Fact]
        public void CustomProtogenSerializer()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<FileDescriptorSet>();
            model.Add<Access>();
            model.Add<ProtogenFileOptions>();
            model.Add<ProtogenMessageOptions>();
            model.Add<ProtogenFieldOptions>();
            model.Add<ProtogenEnumOptions>();
            model.Add<ProtogenEnumValueOptions>();
            model.Add<ProtogenServiceOptions>();
            model.Add<ProtogenMethodOptions>();
            model.Add<ProtogenOneofOptions>();
            PEVerify.CompileAndVerify(model, deleteOnSuccess: false);
        }

        // doesn't really belong here, but left over from the protobuf-net.Reflection.Test split
        [Fact]
        public void CanRountripExtensionData()
        {
            var obj = new CanRountripExtensionData_WithFields { X = 1, Y = 2 };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            var a = BitConverter.ToString(ms.ToArray());
            ms.Position = 0;
            var raw = Serializer.Deserialize<CanRountripExtensionData_WithoutFields>(ms);
            ms.Position = 0;
            ms.SetLength(0);
            Serializer.Serialize(ms, raw);
            var b = BitConverter.ToString(ms.ToArray());

            Assert.Equal(a, b);

            var extData = raw.ExtensionData;
            Assert.NotEqual(0, extData?.Length ?? 0);

            extData = raw.ExtensionData;
            Assert.NotEqual(0, extData?.Length ?? 0);
        }
        [ProtoContract]
        private class CanRountripExtensionData_WithFields
        {
            [ProtoMember(1)]
            public int X { get; set; }
            [ProtoMember(2)]
            public int Y { get; set; }
        }
        [ProtoContract]
        private class CanRountripExtensionData_WithoutFields : Extensible
        {
            public byte[] ExtensionData
            {
                get { return DescriptorProto.GetRawExtensionData(this); }
                set { DescriptorProto.SetRawExtensionData(this, value); }
            }
        }
    }
}
