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
    }
}
