using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProtoBuf.AotSmoke;

[ProtoContract]
public class Wrapper<T>
{
    [ProtoMember(1)] public T Value { get; set; }
}

// an interface is an inheritance root like any other; worth proving under ILC, since the sub-type
// machinery is where the trim annotations had to be got right
[ProtoContract]
[ProtoInclude(10, typeof(Courier))]
public interface IShipper
{
}

[ProtoContract]
public class Courier : IShipper
{
    [ProtoMember(1)] public string Company { get; set; }
}

[ProtoContract]
public class Customer
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
}

/// <summary>
/// Constructed through <c>[UnsafeAccessor(UnsafeAccessorKind.Constructor)]</c>, which ILC has to
/// resolve at publish time — and which ref-emit's compiled path refuses outright.
/// </summary>
[ProtoContract]
public class Ticket
{
    private Ticket() { }
    public Ticket(string code) => Code = code;

    [ProtoMember(1)] public string Code { get; set; }
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

    // null-wrapping: a lone value goes through ReadAny/WriteAny, and a wrapped enum resolves its
    // serializer through the model's proxy - neither of which any other member here exercises
    [ProtoMember(13), NullWrappedValue] public int? Optional { get; set; }
    [ProtoMember(14), NullWrappedValue] public Status? OptionalStatus { get; set; }

    // and a null *inside* a collection, which is the whole point of the feature
    [ProtoMember(15), NullWrappedValue] public List<int?> Sparse { get; } = new();

    // a null collection, distinguishable from an empty one
    [ProtoMember(16), NullWrappedCollection] public List<int> MaybeNone { get; set; }

    // These two are the only members that reach RepeatedSerializer's `serializer ??=` fallback,
    // which TypeModel.ResolveSerializer routes past the reflective resolve under AOT. A repeated
    // *enum* is the sharp case: nothing is passed, and resolution has to find ISerializerProxy<T>
    // through the model. A repeated *message* passes `this` and so proves the other arm still
    // arrives at the same place. Neither is covered by the JIT differential suite in any useful
    // sense - a JIT run takes the dynamic arm, so it never executes this path at all.
    [ProtoMember(24)] public List<Status> History { get; set; }
    [ProtoMember(25)] public List<Customer> Contacts { get; set; }

    // a surrogate: the serializer is the surrogate's body with a conversion at each end
    [ProtoMember(17)] public Money Price { get; set; }

    // constructed via a non-public constructor, reached through [UnsafeAccessor]
    [ProtoMember(23)] public Ticket Ticket { get; set; }

    // an interface root: the same sub-type machinery as Payment, but reached by implementing
    [ProtoMember(22)] public IShipper Shipper { get; set; }

    // closed generics: each instantiation is its own contract, and the value-type one is the
    // interesting case for ILC, which has to generate concrete code for it rather than share
    [ProtoMember(20)] public Wrapper<string> Tag { get; set; }
    [ProtoMember(21)] public Wrapper<int> Count { get; set; }

    // getter-only, reached by writing its backing field through [UnsafeAccessor] - which is worth
    // proving here specifically, since ILC resolves those at publish time
    [ProtoMember(18)] public int Sequence { get; }

    // ...and the same via a trivial getter, where the field is named from the source. Note it is
    // `readonly`, so this also proves a ref to an initonly field is writable under AOT
    private readonly string _token = "";
    [ProtoMember(19)] public string Token => _token;

    public Order() { }
    public Order(int sequence, string token)
    {
        Sequence = sequence;
        _token = token;
    }
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
public class MoneySurrogate
{
    [ProtoMember(1)] public long Units { get; set; }

    public static implicit operator MoneySurrogate(Money value) => new() { Units = value.Units };
    public static implicit operator Money(MoneySurrogate value)
        => value is null ? default : new Money(value.Units);
}

/// <summary>
/// Immutable, and with no parameterless constructor — the canonical reason to use a surrogate.
/// </summary>
[ProtoContract(Surrogate = typeof(MoneySurrogate))]
public readonly struct Money
{
    public Money(long units) => Units = units;
    public long Units { get; }
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
        var original = new Order(sequence: 5, token: "tok")
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
            Optional = 0,
            OptionalStatus = Status.Unknown,
            Sparse = { 1, null, 0 },
            MaybeNone = [],
            History = [Status.Open, Status.Closed, Status.Unknown],
            Contacts = [new Customer { Id = 1, Name = "ann" }, new Customer { Id = 2, Name = "bob" }],
            Price = new Money(1999),
            Tag = new Wrapper<string> { Value = "boxed" },
            Shipper = new Courier { Company = "acme" },
            Ticket = new Ticket("T-1"),
            Count = new Wrapper<int> { Value = 11 },
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

        // an explicit zero must survive as a zero, not collapse to null - that is what the wrapper is for
        Check(ref failures, "Optional", original.Optional, clone.Optional);
        Check(ref failures, "OptionalStatus", original.OptionalStatus, clone.OptionalStatus);
        Check(ref failures, "Sparse", "1,null,0", string.Join(",",
            clone.Sparse.Select(static x => x?.ToString() ?? "null")));
        Check(ref failures, "MaybeNone empty-not-null", "empty",
            clone.MaybeNone is null ? "null" : clone.MaybeNone.Count == 0 ? "empty" : "items");
        Check(ref failures, "Price via surrogate", original.Price.Units, clone.Price.Units);

        // the repeated fallback: the enum resolves through the model's proxy, the message through `this`
        Check(ref failures, "History", "Open,Closed,Unknown",
            clone.History is null ? "null" : string.Join(",", clone.History));
        Check(ref failures, "Contacts", "1:ann,2:bob", clone.Contacts is null ? "null"
            : string.Join(",", clone.Contacts.Select(static x => $"{x.Id}:{x.Name}")));

        // closed generics, one per instantiation
        Check(ref failures, "Tag", original.Tag.Value, clone.Tag?.Value);
        Check(ref failures, "Shipper", "acme", (clone.Shipper as Courier)?.Company);
        Check(ref failures, "Ticket", "T-1", clone.Ticket?.Code);
        Check(ref failures, "Count", original.Count.Value, clone.Count?.Value);

        // getter-only members, restored by writing the backing field through [UnsafeAccessor]
        Check(ref failures, "Sequence", original.Sequence, clone.Sequence);
        Check(ref failures, "Token", original.Token, clone.Token);
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
