using System;

namespace ProtoBuf
{
    internal partial class FixedSerializer : ISerializer<double>
    {
        string ISerializer<double>.DefinedType { get { return ProtoFormat.DOUBLE; } }
        WireType ISerializer<double>.WireType { get { return WireType.Fixed64; } }
        double ISerializer<double>.Deserialize(double value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 8);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(context.Workspace, context.WorkspaceIndex, 8);
            }
            return BitConverter.ToDouble(context.Workspace, context.WorkspaceIndex);
        }
        int ISerializer<double>.Serialize(double value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(buffer, 0, 8);
            }
            context.Stream.Write(buffer, 0, 8);
            return 8;
        }
        int ISerializer<double>.GetLength(double value, SerializationContext context)
        {
            return 8;
        }
    }
}
