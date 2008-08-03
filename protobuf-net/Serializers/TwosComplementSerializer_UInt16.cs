
namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<ushort>
    {
        string ISerializer<ushort>.DefinedType { get { return ProtoFormat.UINT32; } }

        int ISerializer<ushort>.Serialize(ushort value, SerializationContext context)
        {
            return Serialize((uint)value, context);
        }

        ushort ISerializer<ushort>.Deserialize(ushort value, SerializationContext context)
        {
            return (ushort)Deserialize((uint)value, context);
        }
    }
}
