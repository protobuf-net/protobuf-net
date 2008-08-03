using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf
{
    internal partial class ZigZagSerializer : ISerializer<sbyte>
    {
        string ISerializer<sbyte>.DefinedType { get { return ProtoFormat.SINT32; } }
        WireType ISerializer<sbyte>.WireType { get { return WireType.Variant; } }

        sbyte ISerializer<sbyte>.Deserialize(sbyte value, SerializationContext context)
        {
            return (sbyte)ReadInt32(context);
        }

        int ISerializer<sbyte>.Serialize(sbyte value, SerializationContext context)
        {
            return TwosComplementSerializer.WriteToStream(ZigInt32((int)value), context);
        }
    }
}
