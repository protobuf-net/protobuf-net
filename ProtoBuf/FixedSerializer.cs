using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf
{
    internal sealed class FixedSerializer : ISerializer<int>, ISerializer<long>
    {
        public static readonly FixedSerializer Default = new FixedSerializer();
        private FixedSerializer() {}
        int ISerializer<int>.GetLength(int value, SerializationContext context)
        {
            return 4;
        }
        int ISerializer<long>.GetLength(long value, SerializationContext context)
        {
            return 8;
        }
        WireType ISerializer<int>.WireType { get { return WireType.Fixed32; } }
        WireType ISerializer<long>.WireType { get { return WireType.Fixed64; } }
        string ISerializer<int>.DefinedType { get { return "int32"; } }
        string ISerializer<long>.DefinedType { get { return "int64"; } }

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
        int ISerializer<long>.Serialize(long value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(buffer, 0, 8);
            }
            context.Stream.Write(buffer, 0, 8);
            return 8;
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
        long ISerializer<long>.Deserialize(long value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 8);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(context.Workspace, context.WorkspaceIndex, 8);
            }
            return BitConverter.ToInt64(context.Workspace, context.WorkspaceIndex);
        }
    }
}
