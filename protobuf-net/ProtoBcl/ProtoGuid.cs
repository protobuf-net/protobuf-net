using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.ProtoBcl
{
    internal static class ProtoGuid
    {
        const int FieldLo = 0x01, FieldHi = 0x02;

        internal static Guid Deserialize(SerializationContext context)
        {
            byte[] buffer = new byte[16];

            uint prefix;
            bool keepRunning = true;
            while (keepRunning && (prefix = context.TryReadFieldPrefix()) > 0)
            {
                switch (prefix)
                {
                    case (FieldLo << 3) | (int)WireType.Fixed64:
                        context.ReadBlock(buffer, 0, 8);
                        break;
                    case (FieldHi << 3) | (int)WireType.Fixed64:
                        context.ReadBlock(buffer, 8, 8);
                        break;
                    default:
                        WireType wireType;
                        int fieldTag;
                        Serializer.ParseFieldToken(prefix, out wireType, out fieldTag);
                        if (wireType == WireType.EndGroup)
                        {
                            context.EndGroup(fieldTag);
                            keepRunning = false;
                            continue;
                        }
                        switch (fieldTag)
                        {
                            case FieldLo:
                            case FieldHi:
                                throw new ProtoException("Incorrect wire-type deserializing Guid");
                            default:
                                Serializer.SkipData(context, fieldTag, wireType);
                                break;
                        }
                        break;
                }
            }
            return new Guid(buffer);
        }
        internal static int Serialize(Guid value, SerializationContext context, bool lengthPrefix) {
            if (value == Guid.Empty)
            {
                if (lengthPrefix)
                {
                    context.WriteByte(0);
                    return 1;
                }
                return 0;
            }
            byte[] buffer = value.ToByteArray();
            
            int len = 0;
            if (lengthPrefix)
            {
                context.WriteByte((byte)18);
                len = 1;
            }
            context.WriteByte(FieldLo << 3 | (int)WireType.Fixed64);
            context.WriteBlock(buffer, 0, 8);
            context.WriteByte(FieldHi << 3 | (int)WireType.Fixed64);
            context.WriteBlock(buffer, 8, 8);
            return len + 18;

        }
    }
}

//using System;

//namespace ProtoBuf.ProtoBcl
//{
//    [ProtoContract(Name = "bcl.Guid")]
//    internal sealed class ProtoGuid
//    {
//        private ulong lo;
//        [ProtoMember(1, Name = "lo", DataFormat = DataFormat.FixedSize)]
//        public ulong Low { get { return lo; } set { lo = value; } }

//        private ulong hi;
//        [ProtoMember(2, Name = "hi", DataFormat = DataFormat.FixedSize)]
//        public ulong High { get { return hi; } set { hi = value; } }

//        public void Reset()
//        {
//            Low = High = 0;
//        }
//        public static readonly EntitySerializer<ProtoGuid> Serializer
//            = new EntitySerializer<ProtoGuid>();
//    }
//}

