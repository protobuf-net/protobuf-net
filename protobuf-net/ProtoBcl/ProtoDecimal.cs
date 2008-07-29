using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.ProtoBcl
{
    [ProtoContract(Name = "Bcl.Decimal")]
    internal sealed class ProtoDecimal
    {
        private ulong lo;
        [ProtoMember(1, Name="lo")]
        public ulong Low { get { return lo; } set { lo = value; } }

        private uint hi;
        [ProtoMember(2, Name = "hi")]
        public uint High { get { return hi; } set { hi = value; } }

        private uint signScale;
        [ProtoMember(3, Name = "signScale")]
        public uint SignScale { get { return signScale; } set { signScale = value; } }

        public void Reset()
        {
            Low = 0;
            High = 0;
            SignScale = 0;
        }

        public static readonly EntitySerializer<ProtoDecimal> Serializer
            = new EntitySerializer<ProtoDecimal>();
    }
}
