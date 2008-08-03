using System;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer : ISerializer<Guid>, ILengthSerializer<Guid>
    {
        Guid ISerializer<Guid>.Deserialize(Guid oldValue, SerializationContext context)
        {
            ProtoGuid guid = context.GuidTemplate;
            guid.Reset();
            ProtoGuid.Serializer.Deserialize(guid, context);
            if (guid.Low == 0 && guid.Low == 0) return Guid.Empty;
            byte[] buffer = new byte[16];
            ulong value = guid.Low;
            buffer[0] = (byte)(value & 0xFF);
            buffer[1] = (byte)(value >> 8 & 0xFF);
            buffer[2] = (byte)(value >> 16 & 0xFF);
            buffer[3] = (byte)(value >> 24 & 0xFF);
            buffer[4] = (byte)(value >> 32 & 0xFF);
            buffer[5] = (byte)(value >> 40 & 0xFF);
            buffer[6] = (byte)(value >> 48 & 0xFF);
            buffer[7] = (byte)(value >> 56 & 0xFF);
            value = guid.High;
            buffer[8] = (byte)(value & 0xFF);
            buffer[9] = (byte)(value >> 8 & 0xFF);
            buffer[10] = (byte)(value >> 16 & 0xFF);
            buffer[11] = (byte)(value >> 24 & 0xFF);
            buffer[12] = (byte)(value >> 32 & 0xFF);
            buffer[13] = (byte)(value >> 40 & 0xFF);
            buffer[14] = (byte)(value >> 48 & 0xFF);
            buffer[15] = (byte)(value >> 56 & 0xFF);

            return new Guid(buffer);
        }

        int ISerializer<Guid>.Serialize(Guid value, SerializationContext context)
        {
            if (value == Guid.Empty) return 0;

            ProtoGuid guid = context.GuidTemplate;
            guid.Reset();
            byte[] buffer = value.ToByteArray();
            ulong val = 0;
            for (int i = 7; i >= 0; i--)
            {
                val = (val << 8) | buffer[i];
            }
            guid.Low = val;
            val = 0;
            for (int i = 15; i >= 8; i--)
            {
                val = (val << 8) | buffer[i];
            }
            guid.High = val;
            return ProtoGuid.Serializer.Serialize(guid, context);
        }

        string ISerializer<Guid>.DefinedType
        {
            get { return ProtoGuid.Serializer.DefinedType; }
        }

        public int UnderestimateLength(Guid value)
        {
            return value == Guid.Empty ? 0 : 18;
        }
    }
}
