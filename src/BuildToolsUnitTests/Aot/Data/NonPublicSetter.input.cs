using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.NonPublicSetter;

// A deliberate divergence from ref-emit. Its *compiled* path refuses a non-public setter outright -
// "cannot apply changes to property" - apparently to stay verifiable, while its runtime path reaches
// one by reflection. [UnsafeAccessor] needs neither compromise, so we support them.
//
// There is therefore no *.reference.cs for this fixture: ref-emit declines to compile it, and
// AotRefGen skips it rather than failing the whole run. The differential suite still covers it,
// because RuntimeTypeModel *does* handle these - which is exactly the comparison that matters.
[ProtoContract]
public class Guarded
{
    [ProtoMember(1)] public int Value { get; private set; }
    [ProtoMember(2)] public string Text { get; protected set; }
    [ProtoMember(3)] public List<int> Numbers { get; internal set; }

    // and the init-only form, which lands on the same mechanism for a different reason
    [ProtoMember(4)] public int Once { get; init; }

    public Guarded() { }

    public Guarded(int value, string text, List<int> numbers)
    {
        Value = value;
        Text = text;
        Numbers = numbers;
    }
}

public static class NonPublicSetterSamples
{
    public static object[] Values =>
    [
        new Guarded(),
        new Guarded(1, "a", null),
        new Guarded(2, "b", [3, 4]),
        new Guarded(5, null, []) { Once = 6 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Guarded))]
public partial class NonPublicSetterModel : TypeModel
{
}
