using System;

namespace ProtoBuf
{
    internal partial class FixedSerializer : ISerializer<long>
    {
        int ISerializer<long>.GetLength(long value, SerializationContext context)
        {
            return 8;
        }

        WireType ISerializer<long>.WireType { get { return WireType.Fixed64; } }

        string ISerializer<long>.DefinedType { get { return ProtoFormat.SFIXED64; } }
        
        int ISerializer<long>.Serialize(long value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse8(buffer);
            }

            context.Write(buffer, 0, 8);
            return 8;
        }

        long ISerializer<long>.Deserialize(long value, SerializationContext context)
        {
            context.ReadBlock(8);
            if (!BitConverter.IsLittleEndian)
            {
                BlobSerializer.Reverse8(context.Workspace);
            }

            return BitConverter.ToInt64(context.Workspace, 0);
        }
    }
}
