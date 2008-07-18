using System;

namespace ProtoBuf
{
    sealed class SingleSerializer : ISerializer<float>
    {
        public string DefinedType { get { return "float"; } }
        public WireType WireType { get { return WireType.Fixed32; } }
        public float Deserialize(float value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 4);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(context.Workspace, context.WorkspaceIndex, 4);
            }
            return BitConverter.ToSingle(context.Workspace, context.WorkspaceIndex);
        }
        public int Serialize(float value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse(buffer, 0, 4);
            }
            context.Stream.Write(buffer, 0, 4);
            return 4;
        }
        public int GetLength(float value, SerializationContext context)
        {
            return 4;
        }

    }
}
