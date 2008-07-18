
using System;
namespace ProtoBuf
{
    partial class FixedSerializer : ISerializer<int>
    {
        int ISerializer<int>.GetLength(int value, SerializationContext context)
        {
            return 4;
        }
        WireType ISerializer<int>.WireType { get { return WireType.Fixed32; } }
        string ISerializer<int>.DefinedType { get { return ProtoFormat.FIXED32; } }
        int ISerializer<int>.Serialize(int value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(buffer, 0, 4);
            }
            context.Stream.Write(buffer, 0, 4);
            return 4;
        }
        int ISerializer<int>.Deserialize(int value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 4);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(context.Workspace, context.WorkspaceIndex, 4);
            }
            return BitConverter.ToInt32(context.Workspace, context.WorkspaceIndex);
        }
    }

}
