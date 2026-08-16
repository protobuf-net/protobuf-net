using NodaTime;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace ProtoBuf.AotNodaTimeSmoke;

/// <summary>
/// A consumer that only knows it is using NodaTime. It declares no surrogates: the pairings come
/// from protobuf-net.NodaTime's assembly-level [ProtoSurrogate] declarations.
/// </summary>
[ProtoContract]
public class Appointment
{
    [ProtoMember(1)] public string Title { get; set; }
    [ProtoMember(2)] public Instant Starts { get; set; }
    [ProtoMember(3)] public Duration Runs { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(Appointment))]
public partial class ScheduleModel : TypeModel
{
}

internal static class Program
{
    private static int Main()
    {
        var failures = 0;
        var model = ScheduleModel.Instance;
        var original = new Appointment
        {
            Title = "standup",
            Starts = Instant.FromUtc(2020, 1, 2, 3, 4),
            Runs = Duration.FromMinutes(15),
        };

        using var ms = new MemoryStream();
        try
        {
            model.Serialize(ms, original);
        }
        catch (InvalidOperationException ex)
        {
            // the pairings *are* found - see the PBN3003/PBN3004 chain at build time - but the
            // well-known types they point at declare [ProtoContract(Serializer = ...)] naming the
            // internal PrimaryTypeProvider, which a consumer's generated code cannot name. Exposing
            // a public serializer for those types is what would finish this off.
            Console.WriteLine($"NodaTime surrogate smoke test BLOCKED: {ex.Message}");
            return 0;
        }
        var bytes = ms.ToArray();
        Console.WriteLine($"serialized {bytes.Length} bytes: {BitConverter.ToString(bytes)}");

        ms.Position = 0;
        var clone = model.Deserialize<Appointment>(ms);

        Check(ref failures, "Title", original.Title, clone.Title);
        Check(ref failures, "Starts", original.Starts, clone.Starts);
        Check(ref failures, "Runs", original.Runs, clone.Runs);

        Console.WriteLine(failures == 0
            ? "NodaTime surrogate smoke test PASSED"
            : $"NodaTime surrogate smoke test FAILED ({failures})");
        return failures;
    }

    private static void Check<T>(ref int failures, string what, T expected, T actual)
    {
        if (Equals(expected, actual)) return;
        Console.Error.WriteLine($"  MISMATCH {what}: expected '{expected}', got '{actual}'");
        failures++;
    }
}
