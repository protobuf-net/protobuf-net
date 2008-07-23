
using System;
namespace ProtoBuf
{
    internal partial class FixedSerializer : ISerializer<uint>
    {
        int ISerializer<uint>.GetLength(uint value, SerializationContext context)
        {
            return 4;
        }
        WireType ISerializer<uint>.WireType { get { return WireType.Fixed32; } }
        string ISerializer<uint>.DefinedType { get { return ProtoFormat.FIXED32; } }
        int ISerializer<uint>.Serialize(uint value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(buffer, 0, 4);
            }
            context.Stream.Write(buffer, 0, 4);
            return 4;
        }
        uint ISerializer<uint>.Deserialize(uint value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 4);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(context.Workspace, context.WorkspaceIndex, 4);
            }
            return BitConverter.ToUInt32(context.Workspace, context.WorkspaceIndex);
        }
    }
}
