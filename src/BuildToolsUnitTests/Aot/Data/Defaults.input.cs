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

public enum Shade : ushort { None = 0, Red = 1, Green = 2, Blue = 4 }

// ValueMember.ParseDefaultValue does not *convert* a string, it parses it by shape - so a string
// default means something quite different depending on the member's type. These three forms all end
// up as ordinary constants, but only after a lookup that Convert.ChangeType would have thrown on.
[ProtoContract]
public class Parsed
{
    // Enum.Parse(type, s, ignoreCase: true) - by member *name*, and case-insensitively
    [ProtoMember(1), DefaultValue("green")]
    public Shade ByName { get; set; } = Shade.Green;

    // ...while a numeric default on the same enum is the underlying constant, as before
    [ProtoMember(2), DefaultValue(4)]
    public Shade ByValue { get; set; } = Shade.Blue;

    // a char takes s[0], and demands exactly one character
    [ProtoMember(3), DefaultValue("x")]
    public char Letter { get; set; } = 'x';

    [ProtoMember(4), DefaultValue('y')]
    public char DirectChar { get; set; } = 'y';

    // the (Type, string) form, which DefaultValueAttribute converts through a TypeConverter
    [ProtoMember(5), DefaultValue(typeof(Shade), "Red")]
    public Shade ByConverter { get; set; } = Shade.Red;
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

        new Parsed(),                                                // every member at its declared default
        new Parsed { ByName = Shade.None, ByValue = Shade.None, Letter = '\0',
            DirectChar = '\0', ByConverter = Shade.None },           // every member at the CLR default
        new Parsed { ByName = Shade.Red, ByValue = Shade.Green, Letter = 'z',
            DirectChar = 'w', ByConverter = Shade.Blue },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Declared))]
[ProtoSerializable(typeof(Parsed))]
public partial class DefaultsModel : TypeModel
{
}
