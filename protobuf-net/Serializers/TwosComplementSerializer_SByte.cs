
namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<sbyte>
    {
        string ISerializer<sbyte>.DefinedType { get { return ProtoFormat.INT32; } }

        sbyte ISerializer<sbyte>.Deserialize(sbyte value, SerializationContext context)
        {
            return (sbyte)ReadInt32(context);
        }

        int ISerializer<sbyte>.Serialize(sbyte value, SerializationContext context)
        {
            return WriteToStream((int)value, context);
        }
    }

}
