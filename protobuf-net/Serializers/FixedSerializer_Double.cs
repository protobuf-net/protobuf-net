using System;

namespace ProtoBuf
{
    internal partial class FixedSerializer : ISerializer<double>
    {
        string ISerializer<double>.DefinedType { get { return ProtoFormat.DOUBLE; } }
        WireType ISerializer<double>.WireType { get { return WireType.Fixed64; } }
        double ISerializer<double>.Deserialize(double value, SerializationContext context)
        {
            context.ReadBlock(8);
            if (!BitConverter.IsLittleEndian)
            {
                SerializationContext.Reverse8(context.Workspace);
            }
            return BitConverter.ToDouble(context.Workspace, 0);
        }
        int ISerializer<double>.Serialize(double value, SerializationContext context)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                SerializationContext.Reverse8(buffer);
            }
            context.Write(buffer, 0, 8);
            return 8;
        }
    }
}
