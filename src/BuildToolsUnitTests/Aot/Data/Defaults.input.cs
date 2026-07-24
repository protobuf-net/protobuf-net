using ProtoBuf;
using ProtoBuf.Meta;
using System.ComponentModel;

namespace AotFixtures.Defaults;

[ProtoContract]
public class Declared
{
    // Note the initialisers: [DefaultValue] only affects *writing* - the reader never applies it, so
    // a declared default without a matching initialiser is lossy across a round-trip. That is
    // protobuf-net behaviour generally, not something the generator introduces, and is what
    // ShouldDeclareDefaultCodeFixProvider exists to nag about.

    // the write guard becomes '!= 5', so a 5 is omitted and a 0 is written - the exact inverse of
    // the implicit-default behaviour, which is why getting this wrong is silent and nasty
    [ProtoMember(1), DefaultValue(5)]
    public int Number { get; set; } = 5;

    [ProtoMember(2), DefaultValue("abc")]
    public string Text { get; set; } = "abc";

    [ProtoMember(3), DefaultValue(true)]
    public bool Flag { get; set; } = true;

    [ProtoMember(4), DefaultValue(2.5d)]
    public double Ratio { get; set; } = 2.5d;

    [ProtoMember(5), DefaultValue(7L)]
    public long Big { get; set; } = 7L;

    // no declared default: still uses the type default
    [ProtoMember(6)]
    public int Plain { get; set; }
}

public static class DefaultsSamples
{
    public static object[] Values =>
    [
        new Declared(),                                              // all CLR defaults, none of which match
        new Declared { Number = 5, Text = "abc", Flag = true, Ratio = 2.5d, Big = 7L },  // all at declared default
        new Declared { Number = 0, Text = "", Flag = false, Ratio = 0d, Big = 0L },      // all at CLR default
        new Declared { Number = 6, Text = "xyz", Flag = false, Ratio = 0.5d, Big = 8L },
        new Declared { Text = null, Plain = 3 },                     // null string with a declared default
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Declared))]
public partial class DefaultsModel : TypeModel
{
}
