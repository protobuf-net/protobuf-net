using NodaTime;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;

namespace ProtoBuf.AotNodaTimeSmoke;

/// <summary>
/// A consumer that only knows it is using NodaTime. It declares no surrogates and calls no
/// AddNodaTime: the pairings come from protobuf-net.NodaTime's assembly-level [ProtoSurrogate]
/// declarations. IsoDayOfWeek needs no pairing at all - it is an enum.
/// </summary>
[ProtoContract]
public class Appointment
{
    [ProtoMember(1)] public string Title { get; set; }
    [ProtoMember(2)] public Instant Starts { get; set; }
    [ProtoMember(3)] public Duration Runs { get; set; }
    [ProtoMember(4)] public LocalDate Day { get; set; }
    [ProtoMember(5)] public LocalTime Time { get; set; }
    [ProtoMember(6)] public IsoDayOfWeek Dow { get; set; }
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
            Day = new LocalDate(2020, 1, 2),
            Time = new LocalTime(3, 4, 5).PlusNanoseconds(123456789),
            Dow = IsoDayOfWeek.Thursday,
        };

        using var ms = new MemoryStream();
        model.Serialize(ms, original);
        var bytes = ms.ToArray();
        Console.WriteLine($"serialized {bytes.Length} bytes: {BitConverter.ToString(bytes)}");

        ms.Position = 0;
        var clone = model.Deserialize<Appointment>(ms);

        Check(ref failures, "Title", original.Title, clone.Title);
        Check(ref failures, "Starts", original.Starts, clone.Starts);
        Check(ref failures, "Runs", original.Runs, clone.Runs);
        Check(ref failures, "Day", original.Day, clone.Day);
        Check(ref failures, "Time", original.Time, clone.Time);
        Check(ref failures, "Dow", original.Dow, clone.Dow);

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
