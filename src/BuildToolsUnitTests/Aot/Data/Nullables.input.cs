using ProtoBuf;
using ProtoBuf.Meta;
using System.ComponentModel;

namespace AotFixtures.Nullables;

[ProtoContract]
public class Optional
{
    // presence decides: a nullable zero IS written, where a plain zero is not
    [ProtoMember(1)] public int? MaybeInt { get; set; }
    [ProtoMember(2)] public bool? MaybeBool { get; set; }
    [ProtoMember(3)] public double? MaybeDouble { get; set; }
    [ProtoMember(4)] public long? MaybeLong { get; set; }

    // a declared default nests inside the HasValue test rather than replacing it, so null and 5
    // both serialize to nothing - they are indistinguishable on the wire
    [ProtoMember(5), DefaultValue(5)] public int? IntWithDefault { get; set; }

    // a null declared default means "no declared default" at all
    [ProtoMember(6), DefaultValue(null)] public int? IntWithNullDefault { get; set; }

    // the non-nullable counterpart, for contrast
    [ProtoMember(7)] public int Plain { get; set; }
}

public static class NullablesSamples
{
    public static object[] Values =>
    [
        new Optional(),                                     // everything absent
        new Optional { MaybeInt = 0, MaybeBool = false, MaybeDouble = 0d, MaybeLong = 0L },
        new Optional { MaybeInt = 1, MaybeBool = true, MaybeDouble = -1.5d, MaybeLong = long.MinValue },
        new Optional { IntWithDefault = 5 },                // at the declared default: not written
        new Optional { IntWithDefault = 0 },                // present, not the default: written
        new Optional { IntWithNullDefault = 0 },            // null default => plain presence rules
        new Optional { Plain = 0 },                         // implicit default: not written
        new Optional { Plain = 1 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Optional))]
public partial class NullablesModel : TypeModel
{
}
