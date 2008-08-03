
namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<char>
    {
        string ISerializer<char>.DefinedType { get { return ProtoFormat.UINT32; } }

        int ISerializer<char>.Serialize(char value, SerializationContext context)
        {
            return Serialize((uint)value, context);
        }

        char ISerializer<char>.Deserialize(char value, SerializationContext context)
        {
            return (char)Deserialize((uint)value, context);
        }
    }
}
