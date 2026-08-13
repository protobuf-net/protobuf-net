using ProtoBuf;
using System;

[assembly: CompatibilityLevel(CompatibilityLevel.Level300)]
[assembly: ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
[assembly: ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
[module: ProtoDataFormat(typeof(int), DataFormat.FixedSize)]
// the module declaration should win for int; the assembly's Guid declaration stands

namespace ProtoBuf.Test.TestDataFormat
{
    [ProtoContract]
    public class AssemblyScopedFormats
    {
        [ProtoMember(1)] public Guid Guid { get; set; }
        [ProtoMember(2)] public int Int32 { get; set; }
    }
}
