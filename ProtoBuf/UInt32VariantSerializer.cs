using System;

namespace ProtoBuf
{
    sealed class UInt32VariantSerializer : ISerializer<uint>
    {
        public static uint ReadFromStream(SerializationContext context)
        {
            Base128Variant.DecodeFromStream(context, 4);
            BlobSerializer.LocalToFromBigEndian(context, 4);
            return BitConverter.ToUInt32(context.Workspace, context.WorkspaceIndex);
        }
        public string DefinedType { get { return "uint32"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public uint Deserialize(uint value, SerializationContext context)
        {
            return ReadFromStream(context);
        }
        public static int WriteToStream(uint value, SerializationContext context)
        {
            byte[] valueBuffer = BitConverter.GetBytes(value);
            BlobSerializer.LocalToFromBigEndian(valueBuffer);
            return context.Write(Base128Variant.EncodeToWorkspace(valueBuffer, context));
        }
        public int Serialize(uint value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }
        public int GetLength(uint value, SerializationContext context)
        {
            return GetLength(value);
        }
        public static int GetLength(uint value)
        {
            value >>= 7; if (value == 0) return 1;
            value >>= 7; if (value == 0) return 2;
            value >>= 7; if (value == 0) return 3;
            value >>= 7; if (value == 0) return 4;
            return 5;
        }
    }
}
