using ProtoBuf;
using System;

[assembly: CompatibilityLevel(CompatibilityLevel.Level240)]
[module: CompatibilityLevel(CompatibilityLevel.Level300)]
// module should win!

namespace ProtoBuf.Test.TestCompatibilityLevel
{
    [ProtoContract]
    public class AllDefaultWithModuleLevel
    {
        [ProtoMember(1)]
        public int Int32 { get; set; }
        [ProtoMember(2)]
        public DateTime DateTime { get; set; }
        [ProtoMember(3)]
        public TimeSpan TimeSpan { get; set; }
        [ProtoMember(4)]
        public Guid Guid { get; set; }
        [ProtoMember(5)]
        public decimal Decimal { get; set; }
    }
}
