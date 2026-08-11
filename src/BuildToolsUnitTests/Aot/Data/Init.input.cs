using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.Init;

// IL has no notion of init-only - it is a modreq the C# compiler enforces - so ref-emit simply calls
// the setter and merges into an existing instance as usual. [UnsafeAccessor] is the equivalent for
// generated code: resolved at publish time, so it stays AOT-safe, unlike reflection.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id { get; init; }
}

[ProtoContract]
public class Inits
{
    [ProtoMember(1)] public int Number { get; init; }
    [ProtoMember(2)] public string Text { get; init; }
    [ProtoMember(3)] public Nested Message { get; init; }

    // an ordinary setter alongside, to prove they interleave
    [ProtoMember(4)] public int Mutable { get; set; }
}

// a struct target takes its accessor by ref
[ProtoContract]
public struct InitStruct
{
    [ProtoMember(1)] public int Number { get; init; }
}

public static class InitSamples
{
    public static object[] Values =>
    [
        new Inits(),
        new Inits { Number = 1 },
        new Inits { Text = "a" },
        new Inits { Message = new Nested { Id = 2 } },
        new Inits { Mutable = 3 },
        new InitStruct(),
        new InitStruct { Number = 4 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Inits))]
[ProtoSerializable(typeof(InitStruct))]
public partial class InitModel : TypeModel
{
}
