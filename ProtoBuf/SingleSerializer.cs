using System;

namespace ProtoBuf
{
    sealed class SingleSerializer : ISerializer<float>
    {
        public string DefinedType { get { return "float"; } }
        public WireType WireType { get { return WireType.Fixed32; } }
        public float Deserialize(float value, SerializationContext context)
        {
            // read 4 bytes (or die trying)
            BlobSerializer.ReadBlock(context, 4);
            // ensure little-endian
            BlobSerializer.LocalToFromLittleEndian(context, 4);
            // convert the buffer to a float
            return BitConverter.ToSingle(context.Workspace, context.WorkspaceIndex);
        }
        public int Serialize(float value, SerializationContext context)
        {
            // get a local buffer for the float
            byte[] buffer = BitConverter.GetBytes(value);
            // ensure little-endian
            BlobSerializer.LocalToFromLittleEndian(buffer);
            // write to stream
            context.Stream.Write(buffer, 0, 4);
            return 4;
        }
        public int GetLength(float value, SerializationContext context)
        {
            return 4;
        }

    }
}
