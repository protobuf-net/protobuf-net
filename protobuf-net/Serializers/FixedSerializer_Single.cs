using System;

namespace ProtoBuf
{
    internal partial class FixedSerializer : ISerializer<float>
    {
        string ISerializer<float>.DefinedType { get { return ProtoFormat.FLOAT; } }
        WireType ISerializer<float>.WireType { get { return WireType.Fixed32; } }
        float ISerializer<float>.Deserialize(float value, SerializationContext context)
        {
            context.ReadBlock(4);
            if (!BitConverter.IsLittleEndian)
            {
                SerializationContext.Reverse4(context.Workspace);
            }
            return BitConverter.ToSingle(context.Workspace, 0);
        }
        int ISerializer<float>.Serialize(float value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                SerializationContext.Reverse4(buffer);
            }
            context.Write(buffer, 0, 4);
            return 4;
        }
    }
}
