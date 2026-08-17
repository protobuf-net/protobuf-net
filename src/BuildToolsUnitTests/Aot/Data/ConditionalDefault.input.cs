using ProtoBuf;
using ProtoBuf.Meta;
using System.ComponentModel;

namespace AotFixtures.ConditionalDefault;

// a conditional (Specified / ShouldSerialize) *replaces* the declared-default guard rather than
// nesting around it: an explicitly-present declared default still goes to the wire. This is the
// composition that the CustomProtogenSerializer bootstrap caught: descriptor.proto's DTOs pair
// [DefaultValue("")] with ShouldSerialize, and an explicitly-empty default_value was being lost.
[ProtoContract]
public class ConditionalDefault
{
    // the condition is derived from serialized state, so that it survives a round-trip
    // (a condition over non-serialized state cannot, which the differential suite checks)
    [ProtoMember(1), DefaultValue("abc")] public string Text { get; set; } = "abc";
    public bool ShouldSerializeText() => Text != null;

    [ProtoMember(2), DefaultValue(5)] public int Number { get; set; } = 5;
    public bool NumberSpecified { get; set; }

    [ProtoMember(3), DefaultValue(7)] public int? Wrapped { get; set; } = 7;
    public bool WrappedSpecified { get; set; }

    // the unconditional forms, as contrast: these keep the declared-default guard
    [ProtoMember(4), DefaultValue("xyz")] public string Plain { get; set; } = "xyz";
    [ProtoMember(5), DefaultValue(9)] public int Bare { get; set; } = 9;
}

[ProtoModel]
[ProtoSerializable(typeof(ConditionalDefault))]
public partial class ConditionalDefaultModel : TypeModel
{
}

public static class ConditionalDefaultSamples
{
    public static object[] Values =>
    [
        new ConditionalDefault(),
        // the sharp cases: the declared default, explicitly present - must reach the wire
        new ConditionalDefault { Text = "abc" },
        new ConditionalDefault { Text = "" },
        new ConditionalDefault { Text = "other" },
        new ConditionalDefault { Number = 5, NumberSpecified = true },
        new ConditionalDefault { Wrapped = 7, WrappedSpecified = true },
        // and absent-by-condition, whatever the value
        new ConditionalDefault { Number = 6, NumberSpecified = false },
        new ConditionalDefault { Wrapped = null, WrappedSpecified = true },
        new ConditionalDefault { Number = 8, NumberSpecified = true },
        new ConditionalDefault { Wrapped = 11, WrappedSpecified = true },
    ];
}
