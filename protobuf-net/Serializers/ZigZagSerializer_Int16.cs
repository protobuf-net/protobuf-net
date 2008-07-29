using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf
{
    internal partial class ZigZagSerializer : ISerializer<short>
    {
        string ISerializer<short>.DefinedType { get { return ProtoFormat.SINT32; } }
        WireType  ISerializer<short>.WireType { get { return WireType.Variant; } }

        short ISerializer<short>.Deserialize(short value, SerializationContext context)
        {
            return (short)ReadInt32(context);
        }

        int ISerializer<short>.Serialize(short value, SerializationContext context)
        {
            return TwosComplementSerializer.WriteToStream(ZigInt32((int)value), context);
        }

        int ISerializer<short>.GetLength(short value, SerializationContext context)
        {
            int i = ZigInt32(value);

            if ((i & ~0x007F) == 0) return 1;
            if ((i & ~0x3FFF) == 0) return 2;
            return 3;
        }

    }
}
