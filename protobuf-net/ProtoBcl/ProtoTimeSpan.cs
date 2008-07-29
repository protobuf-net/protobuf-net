using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.ProtoBcl
{
    [ProtoContract(Name = "Bcl.TimeSpan")]
    internal sealed class ProtoTimeSpan
    {
        private long value;
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        public long Value { get { return value; } set { this.value = value; } }

        private ProtoTimeSpanScale scale;
        [ProtoMember(2)]
        public ProtoTimeSpanScale Scale { get { return scale; } set { scale = value; } }

        public void Reset()
        {
            Value = 0;
            Scale = ProtoTimeSpanScale.Days;
        }
        [ProtoContract(Name = "Bcl.TimeSpan.TimeSpanScale")]
        public enum ProtoTimeSpanScale
        {
            Days = 0,
            Hours = 1,
            Minutes = 2,
            Seconds = 3,
            Milliseconds = 4,

            MinMax = 15
        }

        public static readonly EntitySerializer<ProtoTimeSpan> Serializer
            = new EntitySerializer<ProtoTimeSpan>();
    }
}
