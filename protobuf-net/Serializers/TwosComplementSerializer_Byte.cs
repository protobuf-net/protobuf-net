
using System.IO;
using System;
namespace ProtoBuf
{
    internal partial class TwosComplementSerializer : ISerializer<byte>
    {
        string ISerializer<byte>.DefinedType { get { return ProtoFormat.UINT32; } }

        int ISerializer<byte>.Serialize(byte value, SerializationContext context)
        {
            context.WriteByte(value);
            if ((value & 0x80) == 0x80)
            {                
                context.WriteByte(0x01);
                return 2;
            }
            return 1;
        }

        byte ISerializer<byte>.Deserialize(byte value, SerializationContext context)
        {
            int low = context.ReadByte();
            if (low < 0) throw new EndOfStreamException();
            if ((low & 0x80) == 0x80)
            {
                int high = context.ReadByte();
                if (high < 0) throw new EndOfStreamException();
                if (high != 0x01) throw new OverflowException("Overflow deserializing byte");
                low = low ^ ((~high) << 7);
            }
            return (byte)low;
        }
    }
}
