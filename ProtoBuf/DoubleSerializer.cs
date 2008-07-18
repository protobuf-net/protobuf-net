using System;

namespace ProtoBuf
{
    sealed class DoubleSerializer : ISerializer<double>
    {
        public string DefinedType { get { return "double"; } }
        public WireType WireType { get { return WireType.Fixed64; } }
        public double Deserialize(double value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 8);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(context.Workspace, context.WorkspaceIndex, 8);
            }
            return BitConverter.ToDouble(context.Workspace, context.WorkspaceIndex);
        }
        public int Serialize(double value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(buffer, 0, 8);
            }
            context.Stream.Write(buffer, 0, 8);
            return 8;
        }
        public int GetLength(double value, SerializationContext context)
        {
            return 8;
        }

    }
}
