using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.Surrogate;

// A surrogate carries the wire shape for a type that cannot describe itself - the classic use being
// a BCL type you do not own. The serializer for the underlying type *is* the surrogate's body, with
// a conversion at each end; nothing changes for a member whose type is surrogated, which stays an
// ordinary sub-message. The surrogate is also a contract in its own right, and gets its own
// serializer alongside.
[ProtoContract]
public class MoneySurrogate
{
    [ProtoMember(1)] public long Units { get; set; }

    public static implicit operator MoneySurrogate(Money value) => new() { Units = value.Units };
    public static implicit operator Money(MoneySurrogate value)
        => value is null ? default : new Money(value.Units);
}

// a value type, and one with no settable members of its own
[ProtoContract(Surrogate = typeof(MoneySurrogate))]
public struct Money
{
    public Money(long units) => Units = units;
    public long Units { get; }
}

[ProtoContract]
public class TagSurrogate
{
    [ProtoMember(1)] public string Text { get; set; }

    public static implicit operator TagSurrogate(Tag value)
        => value is null ? null : new TagSurrogate { Text = value.Text };
    public static implicit operator Tag(TagSurrogate value)
        => value is null ? null : new Tag(value.Text);
}

// a reference type, and an immutable one
[ProtoContract(Surrogate = typeof(TagSurrogate))]
public class Tag
{
    public Tag(string text) => Text = text;
    public string Text { get; }
}

// explicit operators work too: the emitted conversion is a cast either way
[ProtoContract]
public class CodeSurrogate
{
    [ProtoMember(1)] public int Value { get; set; }

    public static explicit operator CodeSurrogate(Code value) => new() { Value = value.Value };
    public static explicit operator Code(CodeSurrogate value)
        => value is null ? default : new Code(value.Value);
}

[ProtoContract(Surrogate = typeof(CodeSurrogate))]
public struct Code
{
    public Code(int value) => Value = value;
    public int Value { get; }
}

[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Money Amount { get; set; }
    [ProtoMember(2)] public Tag Label { get; set; }
    [ProtoMember(3)] public Code Code { get; set; }
    [ProtoMember(4)] public List<Money> Amounts { get; } = new();
    [ProtoMember(5)] public Dictionary<int, Tag> Tags { get; } = new();
}

public static class SurrogateSamples
{
    public static object[] Values =>
    [
        new Holder(),
        new Holder { Amount = new Money(5) },
        new Holder { Label = new Tag("x") },
        new Holder { Code = new Code(7) },
        new Holder { Amounts = { new Money(1), new Money(0) } },
        new Holder { Tags = { [1] = new Tag("a") } },
        new Holder { Amount = new Money(2), Label = new Tag("b"), Code = new Code(3) },
        new Money(9),
        new Tag("direct"),
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Holder))]
[ProtoSerializable(typeof(Money))]
[ProtoSerializable(typeof(Tag))]
public partial class SurrogateModel : TypeModel
{
}
