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

    // proves type scope beats module scope, not just module beats assembly: this type's own
    // [ProtoDataFormat] for int (ZigZag) must win over the module's declaration for int (FixedSize)
    [ProtoContract, ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
    public class TypeOverridesModule
    {
        [ProtoMember(1)] public int Int32 { get; set; }
    }
}
