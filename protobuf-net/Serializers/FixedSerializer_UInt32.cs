
using System;
namespace ProtoBuf
{
    internal partial class FixedSerializer : ISerializer<uint>
    {
        WireType ISerializer<uint>.WireType { get { return WireType.Fixed32; } }
        string ISerializer<uint>.DefinedType { get { return ProtoFormat.FIXED32; } }
        int ISerializer<uint>.Serialize(uint value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                SerializationContext.Reverse4(buffer);
            }
            context.Write(buffer, 0, 4);
            return 4;
        }
        uint ISerializer<uint>.Deserialize(uint value, SerializationContext context)
        {
            context.ReadBlock(4);
            if (!BitConverter.IsLittleEndian)
            {
                SerializationContext.Reverse4(context.Workspace);
            }
            return BitConverter.ToUInt32(context.Workspace, 0);
        }
    }
}
