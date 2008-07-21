using System;

namespace ProtoBuf
{
    partial class FixedSerializer : ISerializer<ulong>
    {
        int ISerializer<ulong>.GetLength(ulong value, SerializationContext context)
        {
            return 8;
        }

        WireType ISerializer<ulong>.WireType { get { return WireType.Fixed64; } }

        string ISerializer<ulong>.DefinedType { get { return ProtoFormat.FIXED64; } }


        int ISerializer<ulong>.Serialize(ulong value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(buffer, 0, 8);
            }
            context.Stream.Write(buffer, 0, 8);
            return 8;
        }

        ulong ISerializer<ulong>.Deserialize(ulong value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 8);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(context.Workspace, context.WorkspaceIndex, 8);
            }
            return BitConverter.ToUInt64(context.Workspace, context.WorkspaceIndex);
        }
    }
}
