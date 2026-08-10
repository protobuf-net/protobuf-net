using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.Keywords;

// Members named after C# keywords. `ISymbol.Name` is the *metadata* name, so a member declared as
// `@case` comes back as `case` and has to be re-escaped before it can be emitted - otherwise the
// generated code reads `value.case = ...`, which is a syntax error in the *consumer's* build.
//
// Nobody writes this by hand, which is why it survived so long. It arrives instead through
// `.proto`-generated DTOs, where a schema field may be named after any C# keyword and protogen
// escapes it for them - `google/cloud/language/v1`'s `PartOfSpeech.case` is the one that found it.
[ProtoContract]
public class Keywords
{
    [ProtoMember(1)] public int @case { get; set; }
    [ProtoMember(2)] public string @event { get; set; }

    // a collection and a sub-message, since those reach the member through different emit paths
    [ProtoMember(3)] public List<int> @params { get; set; }
    [ProtoMember(4)] public Inner @class { get; set; }

    // an auto-tuple member, which is the one shape that reads its members outside the usual
    // accessor path - the constructor arguments are gathered by name
    [ProtoMember(5)] public Pair @lock { get; set; }

    // NOT a keyword: `value` is contextual, so it is already a legal identifier and must *not* be
    // escaped. This is the negative half of the test - over-escaping still compiles, so only the
    // golden pins it.
    [ProtoMember(6)] public int value { get; set; }
}

[ProtoContract]
public class Inner
{
    [ProtoMember(1)] public int @int { get; set; }
}

/// <summary>
/// An auto-tuple: no contract attribute, no public setters, and one constructor whose parameter
/// names match the members.
/// </summary>
public class Pair
{
    public Pair(int @if, string @else)
    {
        this.@if = @if;
        this.@else = @else;
    }

    public int @if { get; }
    public string @else { get; }
}

[ProtoModel]
[ProtoSerializable(typeof(Keywords))]
public partial class KeywordsModel : TypeModel
{
}

public static class KeywordsSamples
{
    public static object[] Values =>
    [
        new Keywords(),
        new Keywords { @case = 7, @event = "hi", value = 3 },
        new Keywords
        {
            @case = -1,
            @event = "",
            @params = [1, 2, 3],
            @class = new Inner { @int = 9 },
            @lock = new Pair(4, "four"),
            value = 0,
        },
        new Keywords { @params = [], @class = new Inner() },
    ];
}
