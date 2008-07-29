using System;

namespace ProtoBuf.ProtoBcl
{
    [ProtoContract(Name = "Bcl.Guid")]
    internal sealed class ProtoGuid
    {
        private ulong lo;
        [ProtoMember(1, Name = "lo", DataFormat = DataFormat.FixedSize)]
        public ulong Low { get { return lo; } set { lo = value; } }

        private ulong hi;
        [ProtoMember(2, Name = "hi", DataFormat = DataFormat.FixedSize)]
        public ulong High { get { return hi; } set { hi = value; } }

        public void Reset()
        {
            Low = High = 0;
        }
        public static readonly EntitySerializer<ProtoGuid> Serializer
            = new EntitySerializer<ProtoGuid>();
    }
}

