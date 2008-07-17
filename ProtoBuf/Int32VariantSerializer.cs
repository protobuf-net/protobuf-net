using System;

namespace ProtoBuf
{
    sealed class Int32VariantSerializer : ISerializer<int>
    {
        public static int ReadFromStream(SerializationContext context)
        {
            Base128Variant.DecodeFromStream(context, 4);
            BlobSerializer.LocalToFromBigEndian(context, 4);
            return BitConverter.ToInt32(context.Workspace, context.WorkspaceIndex);
        }
        internal static bool TryReadFromStream(SerializationContext context, out int value)
        {
            bool hasData = Base128Variant.DecodeFromStream(context, 4, false);
            if (hasData)
            {
                BlobSerializer.LocalToFromBigEndian(context, 4);
                value = BitConverter.ToInt32(context.Workspace, context.WorkspaceIndex);
            }
            else
            {
                value = default(int);
            }
            return hasData;
        }
        
        public static int WriteToStream(int value, SerializationContext context)
        {
            byte[] valueBuffer = BitConverter.GetBytes(value);
            BlobSerializer.LocalToFromBigEndian(valueBuffer);
            return context.Write(Base128Variant.EncodeToWorkspace(valueBuffer, context));
        }
        public string DefinedType { get { return "int32"; } }
        public WireType WireType { get { return WireType.Variant; } }
        public int Deserialize(int value, SerializationContext context)
        {
            return ReadFromStream(context);
        }
        public int Serialize(int value, SerializationContext context)
        {
            return WriteToStream(value, context);
        }
        
        public static int GetLength(int value)
        {
            if (value < 0) return 5;
            unchecked
            {
                return UInt32VariantSerializer.GetLength((uint)value);
            }
        }
        public int GetLength(int value, SerializationContext context)
        {
            return GetLength(value);
        }
    }
}
