using System;

namespace ProtoBuf
{
    sealed class DoubleSerializer : ISerializer<double>
    {
        public string DefinedType { get { return "double"; } }
        public WireType WireType { get { return WireType.Fixed64; } }
        public double Deserialize(double value, SerializationContext context)
        {
            // read 8 bytes (or die trying)
            BlobSerializer.ReadBlock(context, 8);
            // ensure little-endian
            BlobSerializer.LocalToFromLittleEndian(context, 8);
            // convert the buffer to a float
            return BitConverter.ToDouble(context.Workspace, context.WorkspaceIndex);
        }
        public int Serialize(double value, SerializationContext context)
        {
            // get a local buffer for the float
            byte[] buffer = BitConverter.GetBytes(value);
            // ensure little-endian
            BlobSerializer.LocalToFromLittleEndian(buffer);
            // write to stream
            context.Stream.Write(buffer, 0, 8);
            return 8;
        }
        public int GetLength(double value, SerializationContext context)
        {
            return 8;
        }

    }
}
