using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Parseable;

// A type with a ToString() and a static Parse(string) can go on the wire as a string. This is opt-in
// on both sides: RuntimeTypeModel.AllowParseableTypes is off by default, so emitting it
// unconditionally would disagree with the runtime model's default behaviour.
//
// The rules are ParseableSerializer.TryCreate's, and the details matter: Parse and *not* TryParse,
// declared on the type itself, one string in, the type out. A value type additionally needs its own
// ToString() - one inheriting object.ToString() would round-trip its type name.
// System.Net.IPAddress is the canonical example - and the one the corpus actually uses - but it is
// not in the golden tests' reference set, so the fixture supplies its own equivalent
public sealed class Moniker
{
    public Moniker(string scheme, string value)
    {
        Scheme = scheme;
        Value = value;
    }

    public string Scheme { get; }
    public string Value { get; }

    public override string ToString() => $"{Scheme}:{Value}";

    public static Moniker Parse(string text)
    {
        var split = text.IndexOf(':');
        return new Moniker(text.Substring(0, split), text.Substring(split + 1));
    }
}

[ProtoContract]
public class Endpoint
{
    [ProtoMember(1)] public Moniker Address { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

// a value type that qualifies: custom ToString plus a static Parse
[ProtoContract]
public struct Fraction
{
    public int Numerator { get; set; }
    public int Denominator { get; set; }

    public override string ToString() => $"{Numerator}/{Denominator}";

    public static Fraction Parse(string value)
    {
        var parts = value.Split('/');
        return new Fraction { Numerator = int.Parse(parts[0]), Denominator = int.Parse(parts[1]) };
    }
}

// ...and one that does not: TryParse is not Parse, so this stays an ordinary contract
[ProtoContract]
public class NotParseable
{
    [ProtoMember(1)] public int Value { get; set; }

    public static bool TryParse(string text, out NotParseable result)
    {
        result = new NotParseable();
        return true;
    }
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Fraction Ratio { get; set; }
    [ProtoMember(2)] public NotParseable Child { get; set; }
}

public static class ParseableSamples
{
    public static object[] Values =>
    [
        new Endpoint(),
        new Endpoint { Address = Moniker.Parse("tcp:10.0.0.1"), Name = "primary" },
        new Endpoint { Address = new Moniker("unix", "/var/run/x") },
        new Holder(),
        new Holder { Ratio = new Fraction { Numerator = 3, Denominator = 4 } },
        new Holder { Child = new NotParseable { Value = 9 } },
    ];
}

[ProtoModel(AllowParseableTypes = true)]
[ProtoSerializable(typeof(Endpoint))]
[ProtoSerializable(typeof(Holder))]
public partial class ParseableModel : TypeModel
{
}
