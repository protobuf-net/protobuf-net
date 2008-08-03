
namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<short>
    {
        string ISerializer<short>.DefinedType { get { return ProtoFormat.INT32; } }

        short ISerializer<short>.Deserialize(short value, SerializationContext context)
        {
            return (short) ReadInt32(context);
        }

        int ISerializer<short>.Serialize(short value, SerializationContext context)
        {
            return WriteToStream((int)value, context);
        }
    }

}
