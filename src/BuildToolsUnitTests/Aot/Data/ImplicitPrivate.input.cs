using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.ImplicitPrivate;

// ImplicitFields.AllFields takes *any* field, public or not - which is most of the point of it, and
// is why this cannot live in Implicit.input.cs: ref-emit's compiled path refuses a non-public member
// ("Non-public member cannot be used with full dll compilation") and would decline the whole model,
// so there is no .reference.cs here. RuntimeTypeModel handles it by reflection, and we reach it with
// [UnsafeAccessor] - the same three-way split a non-public setter has.
//
// Note the accessor serves *both* directions here: unlike a property reached by its backing field,
// a private field cannot be read directly either.
[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
public class Private
{
    private int _zebra;
    private string _apple;
    public int Public;

    public void Set(int zebra, string apple)
    {
        _zebra = zebra;
        _apple = apple;
    }

    public int Zebra => _zebra;
    public string Apple => _apple;
}

// an explicit [ProtoMember] on a private field works the same way, without implicit mode
[ProtoContract]
public class Explicit
{
    [ProtoMember(1)] private int _value;

    public void Set(int value) => _value = value;
    public int Value => _value;
}

public static class ImplicitPrivateSamples
{
    public static object[] Values =>
    [
        new Private(),
        Build(7, "a", 9),
        new Explicit(),
        BuildExplicit(11),
    ];

    private static Private Build(int zebra, string apple, int pub)
    {
        var result = new Private { Public = pub };
        result.Set(zebra, apple);
        return result;
    }

    private static Explicit BuildExplicit(int value)
    {
        var result = new Explicit();
        result.Set(value);
        return result;
    }
}

[ProtoModel]
[ProtoSerializable(typeof(Private))]
[ProtoSerializable(typeof(Explicit))]
public partial class ImplicitPrivateModel : TypeModel
{
}
