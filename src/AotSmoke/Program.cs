using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Linq;

namespace ProtoBuf.AotSmoke;

[ProtoContract]
public class Customer
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

[ProtoContract]
public class Order
{
    [ProtoMember(1)] public int Number { get; set; }
    [ProtoMember(2)] public string Description { get; set; }
    [ProtoMember(3)] public Customer Customer { get; set; }
    [ProtoMember(4)] public byte[] Payload { get; set; }
    [ProtoMember(5)] public Status Status { get; set; }
    [ProtoMember(6)] public double? Weight { get; set; }

    // reached through an [UnsafeAccessor]; the point of testing it here is that ILC has to resolve
    // that at publish time, which is exactly what a JIT run would not prove
    [ProtoMember(7)] public string Reference { get; init; }

    // a struct target takes its accessor by ref, which is a different UnsafeAccessor signature
    [ProtoMember(8)] public Dimensions Size { get; set; }

    // an inheritance hierarchy: reads and writes route through the root's ISubTypeSerializer, and
    // SubTypeState<T> constructs via TypeHelper<T>.Factory - the reflective path ILC has to keep
    [ProtoMember(9)] public Payment Payment { get; set; }

    // unknown fields are kept rather than discarded, which routes through IExtension/BufferExtension
    [ProtoMember(10)] public Note Note { get; set; }

    // the compatibility-level BCL types, at both ends of the range: level 200 goes through
    // bcl.proto, level 300 through the well-known and string forms
    [ProtoMember(11)] public Legacy Legacy { get; set; }
    [ProtoMember(12)] public Modern Modern { get; set; }
}

[ProtoContract]
public class Legacy
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public TimeSpan How { get; set; }
    [ProtoMember(3)] public Guid Id { get; set; }
    [ProtoMember(4)] public decimal Amount { get; set; }
}

[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
public class Modern
{
    [ProtoMember(1)] public DateTime When { get; set; }
    [ProtoMember(2)] public TimeSpan How { get; set; }
    [ProtoMember(3)] public Guid Id { get; set; }
    [ProtoMember(4)] public decimal Amount { get; set; }
}

[ProtoContract]
public class Note : Extensible
{
    [ProtoMember(1)] public string Text { get; set; }

    // a non-public setter, reached the same way an init-only one is. ref-emit's compiled path
    // refuses these outright, so this is a deliberate divergence - and ILC has to resolve a setter
    // that is not visible from the call site at all
    [ProtoMember(2)] public int Sequence { get; private set; }

    public void Stamp(int sequence) => Sequence = sequence;
}

/// <summary>
/// The same shape as <see cref="Note"/> plus a field it does not know about — which is how an
/// unknown field is produced without going anywhere near a reflective API.
/// </summary>
[ProtoContract]
public class NoteV2
{
    [ProtoMember(1)] public string Text { get; set; }
    [ProtoMember(5)] public int Number { get; set; }
}

[ProtoContract]
public struct Dimensions
{
    [ProtoMember(1)] public int Width { get; init; }
    [ProtoMember(2)] public int Height { get; init; }
}

[ProtoContract]
[ProtoInclude(100, typeof(CardPayment))]
public abstract class Payment
{
    [ProtoMember(1)] public int Amount { get; set; }
}

[ProtoContract]
public sealed class CardPayment : Payment
{
    [ProtoMember(1)] public string Last4 { get; set; }
}

public enum Status { Unknown = 0, Open = 1, Closed = 2 }

[ProtoModel]
[ProtoSerializable(typeof(Order))]
[ProtoSerializable(typeof(NoteV2))]
public partial class SmokeModel : TypeModel
{
}

internal static class Program
{
    /// <summary>
    /// Round-trips through the generated model using only the generic APIs, so nothing is resolved
    /// reflectively. Returns 0 on success; any failure is reported and returns non-zero.
    /// </summary>
    private static readonly DateTime When = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    private static readonly Guid Id = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");

    private static int Main()
    {
        var failures = 0;

        var model = new SmokeModel();
        var original = new Order
        {
            Number = 42,
            Description = "hello",
            Customer = new Customer { Id = 7, Name = "marc" },
            Payload = [1, 2, 3],
            Status = Status.Closed,
            Weight = 2.5d,
            Reference = "ref-1",
            Size = new Dimensions { Width = 3, Height = 4 },
            Payment = new CardPayment { Amount = 99, Last4 = "4242" },
            Note = new Note { Text = "hi" },
            Legacy = new Legacy
            {
                When = When,
                How = TimeSpan.FromMinutes(90),
                Id = Id,
                Amount = 1.25m,
            },
            Modern = new Modern
            {
                When = When,
                How = TimeSpan.FromMinutes(90),
                Id = Id,
                Amount = 1.25m,
            },
        };

        original.Note.Stamp(11);

        using var ms = new MemoryStream();
        model.Serialize(ms, original);
        var bytes = ms.ToArray();
        Console.WriteLine($"serialized {bytes.Length} bytes: {BitConverter.ToString(bytes)}");

        ms.Position = 0;
        var clone = model.Deserialize<Order>(ms);

        Check(ref failures, "Number", original.Number, clone.Number);
        Check(ref failures, "Description", original.Description, clone.Description);
        Check(ref failures, "Customer.Id", original.Customer.Id, clone.Customer?.Id);
        Check(ref failures, "Customer.Name", original.Customer.Name, clone.Customer?.Name);
        Check(ref failures, "Payload", BitConverter.ToString(original.Payload),
            clone.Payload is null ? null : BitConverter.ToString(clone.Payload));
        Check(ref failures, "Status", original.Status, clone.Status);
        Check(ref failures, "Weight", original.Weight, clone.Weight);
        Check(ref failures, "Reference", original.Reference, clone.Reference);
        Check(ref failures, "Size.Width", original.Size.Width, clone.Size.Width);
        Check(ref failures, "Size.Height", original.Size.Height, clone.Size.Height);
        Check(ref failures, "Payment type", typeof(CardPayment), clone.Payment?.GetType());
        Check(ref failures, "Payment.Amount", original.Payment.Amount, clone.Payment?.Amount);
        Check(ref failures, "Payment.Last4", ((CardPayment)original.Payment).Last4,
            (clone.Payment as CardPayment)?.Last4);
        Check(ref failures, "Note.Text", original.Note.Text, clone.Note?.Text);
        Check(ref failures, "Note.Sequence", original.Note.Sequence, clone.Note?.Sequence);
        Check(ref failures, "Legacy.When", original.Legacy.When, clone.Legacy?.When);
        Check(ref failures, "Legacy.How", original.Legacy.How, clone.Legacy?.How);
        Check(ref failures, "Legacy.Id", original.Legacy.Id, clone.Legacy?.Id);
        Check(ref failures, "Legacy.Amount", original.Legacy.Amount, clone.Legacy?.Amount);
        Check(ref failures, "Modern.When", original.Modern.When, clone.Modern?.When);
        Check(ref failures, "Modern.How", original.Modern.How, clone.Modern?.How);
        Check(ref failures, "Modern.Id", original.Modern.Id, clone.Modern?.Id);
        Check(ref failures, "Modern.Amount", original.Modern.Amount, clone.Modern?.Amount);

        // an unknown field must survive being read into a contract that does not declare it and
        // written back out. Producing it via NoteV2 keeps this on the generic, generated path -
        // Extensible.AppendValue would not, since it serializes through the reflective auxiliary
        // path and so silently does nothing once trimmed.
        using var v2 = new MemoryStream();
        model.Serialize(v2, new NoteV2 { Text = "hi", Number = 1234 });
        var v2Bytes = v2.ToArray();

        v2.Position = 0;
        var narrowed = model.Deserialize<Note>(v2);
        using var again = new MemoryStream();
        model.Serialize(again, narrowed);

        Check(ref failures, "Note.Text via v2", "hi", narrowed.Text);
        Check(ref failures, "unknown field preserved", BitConverter.ToString(v2Bytes),
            BitConverter.ToString(again.ToArray()));

        // and the bytes must be stable across a second pass
        using var second = new MemoryStream();
        model.Serialize(second, clone);
        Check(ref failures, "round-trip bytes", BitConverter.ToString(bytes),
            BitConverter.ToString(second.ToArray()));

        Console.WriteLine(failures == 0 ? "AOT smoke test PASSED" : $"AOT smoke test FAILED ({failures})");
        return failures;
    }

    private static void Check<T>(ref int failures, string what, T expected, T actual)
    {
        if (Equals(expected, actual)) return;
        Console.Error.WriteLine($"  MISMATCH {what}: expected '{expected}', got '{actual}'");
        failures++;
    }
}
