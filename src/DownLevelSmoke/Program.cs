using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace ProtoBuf.DownLevelSmoke;

// Everything here that needs [UnsafeAccessor] is expected to be *dropped* with a warning, because
// this project targets net472. The point of the exercise is that the rest of the model still emits,
// still compiles, and still round-trips - a down-level consumer gets a smaller model, not a broken
// build. The dropped contracts fall back to TypeModel's "no serializer for type" throw, which is the
// intended backstop.

[ProtoContract]
public class Fine
{
    [ProtoMember(1)] public int Value { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
    [ProtoMember(3)] public Nested Child { get; set; }
}

[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id { get; set; }
}

// dropped here, emitted on net8.0+: its parameterless constructor is not public
[ProtoContract]
public class NonPublicCtor
{
    private NonPublicCtor() { }
    public NonPublicCtor(int value) => Value = value;

    [ProtoMember(1)] public int Value { get; set; }
}

// dropped here, emitted on net8.0+: an init-only setter cannot be assigned after construction
[ProtoContract]
public class InitOnly
{
    [ProtoMember(1)] public int Value { get; init; }
}

// dropped here, emitted on net8.0+: a non-public setter is not callable from generated code
[ProtoContract]
public class NonPublicSetter
{
    [ProtoMember(1)] public int Value { get; private set; }

    public void Set(int value) => Value = value;
}

[ProtoModel]
[ProtoSerializable(typeof(Fine))]
[ProtoSerializable(typeof(NonPublicCtor))]
[ProtoSerializable(typeof(InitOnly))]
[ProtoSerializable(typeof(NonPublicSetter))]
public partial class DownLevelModel : TypeModel
{
}

internal static class Program
{
    private static int Main()
    {
        var failures = 0;
        var model = new DownLevelModel();

        var original = new Fine { Value = 42, Name = "hello", Child = new Nested { Id = 7 } };

        using var ms = new MemoryStream();
        model.Serialize(ms, original);
        var bytes = ms.ToArray();
        Console.WriteLine($"serialized {bytes.Length} bytes: {BitConverter.ToString(bytes)}");

        ms.Position = 0;
        var clone = (Fine)model.Deserialize(ms, null, typeof(Fine));

        Check(ref failures, "Value", original.Value, clone.Value);
        Check(ref failures, "Name", original.Name, clone.Name);
        Check(ref failures, "Child.Id", original.Child.Id, clone.Child?.Id);

        // ...and a dropped contract fails loudly rather than silently doing the wrong thing
        try
        {
            using var dropped = new MemoryStream();
            model.Serialize(dropped, new InitOnly { Value = 1 });
            Console.Error.WriteLine("FAIL: expected a dropped contract to throw");
            failures++;
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("dropped contract throws as expected");
        }

        Console.WriteLine(failures == 0 ? "down-level smoke test PASSED" : $"down-level smoke test FAILED ({failures})");
        return failures;
    }

    private static void Check<T>(ref int failures, string label, T expected, T actual)
    {
        if (Equals(expected, actual)) return;

        Console.Error.WriteLine($"FAIL {label}: expected {expected}, got {actual}");
        failures++;
    }
}
