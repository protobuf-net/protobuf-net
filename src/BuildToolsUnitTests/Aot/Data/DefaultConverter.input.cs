using ProtoBuf;
using ProtoBuf.Meta;
using System.ComponentModel;

namespace AotFixtures.DefaultConverter;

// [DefaultValue(typeof(T), "…")] - the form that goes through a TypeConverter. The attribute's own
// constructor does the conversion (TypeDescriptor.GetConverter(type).ConvertFromInvariantString) and
// stores the result in Value, which is what protobuf-net reads. Roslyn does not run constructors, so
// the generator sees the raw string and converts it itself - invariant, matching the BCL.
//
// Note each member is initialised to its declared default, as [DefaultValue] affects writing only.
[ProtoContract]
public class Converted
{
    [ProtoMember(1), DefaultValue(typeof(int), "5")] public int Number { get; set; } = 5;
    [ProtoMember(2), DefaultValue(typeof(string), "abc")] public string Text { get; set; } = "abc";
    [ProtoMember(3), DefaultValue(typeof(bool), "true")] public bool Flag { get; set; } = true;
    // note: no `decimal` here - [DefaultValue] on a compatibility-level BCL type is refused
    // separately, for want of a ref-emit shape to copy, and that refusal predates this
    [ProtoMember(5), DefaultValue(typeof(double), "2.25")] public double Ratio { get; set; } = 2.25d;
    [ProtoMember(6), DefaultValue(typeof(long), "-7")] public long Big { get; set; } = -7L;

    // the ordinary single-argument form alongside, to prove both still work
    [ProtoMember(7), DefaultValue(9)] public int Plain { get; set; } = 9;
}

public static class DefaultConverterSamples
{
    public static object[] Values =>
    [
        new Converted(),
        new Converted { Number = 6, Text = "z", Flag = false, Ratio = 0d, Big = 8L, Plain = 10 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Converted))]
public partial class DefaultConverterModel : TypeModel
{
}
