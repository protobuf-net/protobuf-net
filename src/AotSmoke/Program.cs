using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using Google.Protobuf.Reflection;

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

    // [ProtoDataFormat]: a type-scoped default that promotes Vault's undecorated Guid to the
    // 16-byte fixed form, rather than the 36-character string Modern's bare Guid takes at the same
    // compatibility level - worth checking by eye in the hex dump for exactly that reason
    [ProtoMember(58)] public Vault Vault { get; set; }

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

    // Maps had *no* native coverage at all before these, which made every annotation on the
    // MapSerializer family unmeasured rather than known-harmless. Three shapes, because they reach
    // the value serializer three different ways: a scalar value inlines it, a message value is
    // passed `this`, and a repeated value is resolved from the model through ISerializerProxy -
    // the same sharp case as the repeated enum above, and the one an AOT-only bug would hide in.
    [ProtoMember(26)] public Dictionary<int, string> Labels { get; set; }
    [ProtoMember(27)] public Dictionary<string, Customer> Directory { get; set; }
    [ProtoMember(28)] public Dictionary<int, List<int>> Buckets { get; set; }

    // an enum on each side of a map: like a repeated enum, the serializer is resolved from the
    // *model* through ISerializerProxy<TEnum> rather than passed in, which is the arm that only a
    // native publish exercises
    [ProtoMember(56)] public Dictionary<Status, int> ByStatus { get; set; }
    [ProtoMember(57)] public Dictionary<int, Status> ToStatus { get; set; }

    // declaration-served scalar: field 63 is a bare varint - a wrongly-assumed message category
    // would have written a length prefix over it, so check the payload dump by eye too. Bonus is
    // the nullable twin, proving the same shape survives ILC for Nullable<T> of an externally
    // scalar-serialized struct.
    [ProtoMember(63)] public Tally<int> Score { get; set; }
    [ProtoMember(64)] public Tally<string>? Bonus { get; set; }

    // hand-written serializers, one per category - see the note by their declarations. These are
    // the only members that reach SerializerCache.Get<TProvider, T>(), i.e. the last genuinely
    // reflective step on the generated path.
    [ProtoMember(29)] public Barcode Barcode { get; set; }
    [ProtoMember(30)] public Gauge Gauge { get; set; }
    [ProtoMember(31)] public Batch Batch { get; set; }

    // Collections beyond List<T>. Which RepeatedSerializer factory serves which of these is a
    // priority-ordered provider walk rather than a lookup, so each of these lands on a *different*
    // factory - and every factory is a distinct generic instantiation ILC has to generate.
    // ImmutableArray<T> is the odd one: a struct, so neither side null-tests it, and its default
    // value throws on enumeration rather than behaving as empty.
    [ProtoMember(32)] public int[] Codes { get; set; }
    [ProtoMember(33)] public Customer[] Team { get; set; }
    [ProtoMember(34)] public HashSet<string> Tags { get; set; }
    [ProtoMember(35)] public Queue<int> Pending { get; set; }
    [ProtoMember(36)] public SortedSet<int> Ranks { get; set; }
    [ProtoMember(37)] public ImmutableArray<int> Frozen { get; set; }
    [ProtoMember(38)] public ConcurrentQueue<int> Inbox { get; set; }

    // The immutable *reference* families, which ImmutableArray says nothing about: it is a struct,
    // and more to the point these do not construct through Activator at all - CreateImmutableList
    // and friends go through the type's own Empty/builder - so the DynamicAccess.Activated fix that
    // rescued HashSet and Queue is no evidence about them either.
    [ProtoMember(44)] public ImmutableList<int> Archive { get; set; }
    [ProtoMember(45)] public ImmutableDictionary<string, int> Quotas { get; set; }

    // Four types with their own entries in ValueMember.TryGetCoreSerializer's switch. DateOnly and
    // TimeOnly go through BclHelpers under a *varint* header rather than a length prefix; nint is an
    // ordinary varint whose width ref-emit fixes at 64 regardless of the platform.
    [ProtoMember(46)] public DateOnly Day { get; set; }
    [ProtoMember(47)] public TimeOnly Time { get; set; }
    [ProtoMember(48)] public nint Offset { get; set; }

    // a parseable type - ToString() out, Parse(string) back - which needs the model to opt in. Both
    // halves run on the generated path, so this proves ILC keeps IPAddress.Parse reachable.
    [ProtoMember(49)] public IPAddress Host { get; set; }

    // [DefaultValue] changes the write guard from `!= 0` to `!= 5`. Set to zero deliberately: a
    // plain int would skip it, this one writes it, and it must come back as 0 rather than as the
    // initialiser's 5 - which is what proves the declared default reached the emitted comparison.
    [ProtoMember(50), DefaultValue(5)] public int Retries { get; set; } = 5;

    // the callback families, both spellings - the System.Runtime.Serialization one takes a
    // StreamingContext, built from SerializationContext.AsStreamingContext(state.Context)
    [ProtoMember(51)] public Audited Audited { get; set; }
    [ProtoMember(52)] public Tracked Tracked { get; set; }

    // {Name}Specified and ShouldSerialize{Name}(), matched by name rather than by attribute
    [ProtoMember(53)] public Conditional Conditional { get; set; }

    // ImplicitFields, both modes; the AllFields one reaches its private fields in *both* directions
    // through [UnsafeAccessor], which is the shape ILC has to resolve at publish time
    [ProtoMember(54)] public Sorted Sorted { get; set; }
    [ProtoMember(55)] public Ledger Ledger { get; set; }

    // DataFormat changes the emitted shape, not just a features constant. ZigZag is the one that
    // needs state.Hint(SignedVarint) before the read; FixedSize picks its width from the member;
    // Group swaps WriteMessage for WriteGroup on a lone message, and on a *collection* pushes
    // WireTypeStartGroup into the element features so both directions carry group markers.
    [ProtoMember(39, DataFormat = DataFormat.ZigZag)] public int Delta { get; set; }
    [ProtoMember(40, DataFormat = DataFormat.FixedSize)] public long Ticks { get; set; }
    [ProtoMember(41, DataFormat = DataFormat.Group)] public Customer Grouped { get; set; }
    [ProtoMember(42, DataFormat = DataFormat.Group)] public List<Customer> GroupedTeam { get; set; }

    // SkipConstructor routes construction through BclHelpers.GetUninitializedObject, which is the
    // kind of thing ILC could plausibly not support. Its effect is only observable in a member that
    // is *not* serialized - see Blank.Stamp.
    [ProtoMember(43)] public Blank Blank { get; set; }

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

/// <summary>
/// A type-scoped <see cref="ProtoDataFormatAttribute"/> default: the bare <see cref="Guid"/> member
/// takes <see cref="DataFormat.FixedSize"/> - the 16-byte form - purely from the declaration below,
/// with no <c>[ProtoMember(DataFormat = ...)]</c> on the member itself.
/// </summary>
[ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
[ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
public class Vault
{
    [ProtoMember(1)] public Guid Entry { get; set; }
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

/// <summary>
/// protobuf-net's own callback family. <see cref="Trace"/> is not serialized, so it is the only
/// evidence the hooks ran at all — and the "before" hook must be seen to fire after construction
/// but before the field loop.
/// </summary>
[ProtoContract]
public class Audited
{
    [ProtoMember(1)] public string Value { get; set; }

    public string Trace { get; set; } = "";

    [ProtoBeforeSerialization] public void BeforeSer() => Trace += "bs;";
    [ProtoAfterSerialization] public void AfterSer() => Trace += "as;";
    [ProtoBeforeDeserialization] public void BeforeDes() => Trace += "bd;";
    [ProtoAfterDeserialization] public void AfterDes() => Trace += "ad;";
}

/// <summary>
/// The <c>System.Runtime.Serialization</c> spelling of the same four points, which differs only in
/// taking a <see cref="StreamingContext"/>.
/// </summary>
[ProtoContract]
public class Tracked
{
    [ProtoMember(1)] public string Value { get; set; }

    public string Trace { get; set; } = "";

    [OnSerializing] public void OnSer(StreamingContext context) => Trace += "os;";
    [OnSerialized] public void OnSerd(StreamingContext context) => Trace += "od;";
    [OnDeserializing] public void OnDes(StreamingContext context) => Trace += "ds;";
    [OnDeserialized] public void OnDesd(StreamingContext context) => Trace += "dd;";
}

/// <summary>
/// The two by-name conventions. <see cref="Explicit"/> is zero and still written, because
/// <see cref="ExplicitSpecified"/> replaces the trivial-value guard rather than adding to it — and
/// it is assigned again on the way back in. <see cref="Hidden"/> is non-zero and never written.
/// </summary>
[ProtoContract]
public class Conditional
{
    [ProtoMember(1)] public int Explicit { get; set; }
    public bool ExplicitSpecified { get; set; }

    [ProtoMember(2)] public int Hidden { get; set; }
    public bool ShouldSerializeHidden() => false;
}

/// <summary>
/// <see cref="ImplicitFields.AllPublic"/>: members are numbered by sorting on *name*, so these take
/// tags 1, 2, 3 in the order Apple, Mango, Zebra rather than as declared.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class Sorted
{
    public string Zebra { get; set; }
    public string Apple { get; set; }
    public string Mango { get; set; }
}

/// <summary>
/// <see cref="ImplicitFields.AllFields"/> over private fields, which need an
/// <c>[UnsafeAccessor]</c> for <em>both</em> directions — unlike a property reached by its backing
/// field, neither the read nor the write can touch these directly.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
public class Ledger
{
    private int _balance;
    private string _owner;

    public Ledger() { }
    public Ledger(int balance, string owner)
    {
        _balance = balance;
        _owner = owner;
    }

    public int Balance => _balance;
    public string Owner => _owner;
}

// Hand-written serializers. The generated model emits no body for these at all: the services type
// implements ISerializerProxy<T>, and members reach the serializer through
// SerializerCache.Get<TProvider, T>() - which activates TProvider with
// Activator.CreateInstance(typeof(TProvider), nonPublic: true). That is the one genuinely
// reflective step left on the generated path, held open only by DynamicAccess.Serializer, and it
// has broken before: the annotation was once missing at that public boundary, ILC trimmed the
// constructor, and the first serialize threw MissingMethodException. Nothing but a native publish
// catches that, which is why all three shapes are here.
//
// All three, because the serializer's *category* changes the framing rather than just the body: a
// message is written as a sub-message, a scalar is framed by the serializer's own wire type via
// WriteAny, and IsScalar is the only route that survives into metadata.

public sealed class BarcodeSerializer : ISerializer<Barcode>
{
    SerializerFeatures ISerializer<Barcode>.Features
        => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

    Barcode ISerializer<Barcode>.Read(ref ProtoReader.State state, Barcode value)
    {
        value ??= new Barcode();
        int field;
        while ((field = state.ReadFieldHeader()) > 0)
        {
            if (field == 1) value.Code = state.ReadString();
            else state.SkipField();
        }
        return value;
    }

    void ISerializer<Barcode>.Write(ref ProtoWriter.State state, Barcode value)
        => state.WriteString(1, value.Code);
}

[ProtoContract(Serializer = typeof(BarcodeSerializer))]
public class Barcode
{
    public string Code { get; set; }
}

public sealed class GaugeSerializer : ISerializer<Gauge>
{
    SerializerFeatures ISerializer<Gauge>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

    Gauge ISerializer<Gauge>.Read(ref ProtoReader.State state, Gauge value)
        => new Gauge(state.ReadInt64());

    void ISerializer<Gauge>.Write(ref ProtoWriter.State state, Gauge value)
        => state.WriteInt64(value.Value);
}

[ProtoContract(Serializer = typeof(GaugeSerializer))]
public readonly struct Gauge
{
    public Gauge(long value) => Value = value;
    public long Value { get; }
}

// category stated outright rather than read from the Features declaration - the only route
// available when the serializer arrives through a compiled reference
public sealed class BatchSerializer : ISerializer<Batch>
{
    SerializerFeatures ISerializer<Batch>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeFixed32;

    Batch ISerializer<Batch>.Read(ref ProtoReader.State state, Batch value)
        => new Batch(state.ReadInt32());

    void ISerializer<Batch>.Write(ref ProtoWriter.State state, Batch value)
        => state.WriteInt32(value.Value);
}

[ProtoContract(Serializer = typeof(BatchSerializer), IsScalar = true)]
public readonly struct Batch
{
    public Batch(int value) => Value = value;
    public int Value { get; }
}

// [ProtoSerializer] on the model: the open mapping closes over each instantiation, so ILC must
// generate SerializerCache<TallySerializer<int>, Tally<int>> (and the string one) from a name that
// exists only in generated code - the declaration-served twin of Batch/Gauge/Barcode above. Bonus
// is Nullable<Tally<string>>, proving the ISerializerProxy-for-Nullable path an externally-scalar
// serialized struct member takes also survives under real ILC, not just JIT.
public readonly struct Tally<T>
{
    public Tally(long count) => Count = count;
    public long Count { get; }
}

public sealed class TallySerializer<T> : ISerializer<Tally<T>>
{
    SerializerFeatures ISerializer<Tally<T>>.Features
        => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

    Tally<T> ISerializer<Tally<T>>.Read(ref ProtoReader.State state, Tally<T> value)
        => new Tally<T>(state.ReadInt64());

    void ISerializer<Tally<T>>.Write(ref ProtoWriter.State state, Tally<T> value)
        => state.WriteInt64(value.Count);
}

/// <summary>
/// Constructed via <c>BclHelpers.GetUninitializedObject</c> rather than <c>new</c>, so the
/// constructor never runs on deserialize. <see cref="Stamp"/> is not serialized and exists purely
/// to make that observable: it comes back null, not "ctor".
/// </summary>
[ProtoContract(SkipConstructor = true)]
public class Blank
{
    public Blank() => Stamp = "ctor";

    [ProtoMember(1)] public int Value { get; set; }

    public string Stamp { get; set; }
}

// AllowParseableTypes is off by default in RuntimeTypeModel, so the compile-time mirror of it has
// to be asked for; without it the IPAddress member is not a scalar and the contract is refused.
[ProtoModel(AllowParseableTypes = true)]
[ProtoSerializable(typeof(Order))]
[ProtoSerializable(typeof(NoteV2))]
// an open [ProtoSerializer] mapping: the declaration names Tally<> and TallySerializer<> as open
// generic definitions, and the generator closes each over the type arguments of its use sites
// (Tally<int> for Score, Tally<string> for Bonus) - the declaration-served twin of the
// [ProtoContract(Serializer = ...)] shapes above, proven under real ILC rather than JIT alone.
[ProtoSerializer(typeof(Tally<>), typeof(TallySerializer<>), IsScalar = true)]
// A .proto-generated DTO tree, from descriptor.proto - the schema every other schema
// imports, and about as unlike a hand-written contract as protobuf-net produces: getter-only
// collections reached through their backing fields, ShouldSerialize* on nearly every member,
// IExtensible throughout, recursive nesting, and enums. Nothing else here proves that a
// schema-generated model survives ILC rather than merely matching ref-emit on a JIT runtime.
[ProtoSerializable(typeof(global::Google.Protobuf.Reflection.FileDescriptorSet))]
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

        var model = SmokeModel.Instance;
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
            Labels = new() { [1] = "one", [2] = "two" },
            Directory = new() { ["ann"] = new Customer { Id = 3, Name = "ann" } },
            Buckets = new() { [7] = [70, 71], [8] = [80] },
            ByStatus = new() { [Status.Open] = 1, [Status.Unknown] = 0 },
            ToStatus = new() { [1] = Status.Closed, [2] = Status.Unknown },
            Barcode = new Barcode { Code = "X-9" },
            Gauge = new Gauge(4242),
            Batch = new Batch(77),
            Score = new Tally<int>(421),
            Bonus = new Tally<string>(-9),
            Codes = [11, 12, 13],
            Team = [new Customer { Id = 4, Name = "cat" }],
            Tags = ["red", "blue"],
            Pending = new Queue<int>([21, 22]),
            Ranks = [5, 3, 9],
            Frozen = [31, 32],
            Inbox = new ConcurrentQueue<int>([41, 42]),
            Archive = [51, 52],
            Quotas = ImmutableDictionary.CreateRange(
                [KeyValuePair.Create("a", 61), KeyValuePair.Create("b", 62)]),
            Day = new DateOnly(2021, 6, 7),
            Time = new TimeOnly(13, 14, 15),
            Offset = 4096,
            Host = IPAddress.Parse("10.0.0.7"),
            // zero, so it differs from the declared default of 5 and is therefore written
            Retries = 0,
            Audited = new Audited { Value = "a" },
            Tracked = new Tracked { Value = "t" },
            Conditional = new Conditional { Explicit = 0, ExplicitSpecified = true, Hidden = 9 },
            Sorted = new Sorted { Zebra = "z", Apple = "a", Mango = "m" },
            Ledger = new Ledger(1234, "ann"),
            // negative, so ZigZag actually differs from the default varint encoding
            Delta = -5,
            Ticks = 1234567890123L,
            Grouped = new Customer { Id = 5, Name = "dan" },
            GroupedTeam = [new Customer { Id = 6, Name = "eve" }],
            Blank = new Blank { Value = 8 },
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
            Vault = new Vault { Entry = Guid.Parse("c416e4af-455e-414c-948c-f27873326547") },
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

        // maps: scalar value, message value, and a repeated value resolved through a proxy
        Check(ref failures, "Labels", "1=one,2=two", clone.Labels is null ? "null"
            : string.Join(",", clone.Labels.OrderBy(static x => x.Key).Select(static x => $"{x.Key}={x.Value}")));
        Check(ref failures, "Directory", "ann=3:ann", clone.Directory is null ? "null"
            : string.Join(",", clone.Directory.OrderBy(static x => x.Key, StringComparer.Ordinal)
                .Select(static x => $"{x.Key}={x.Value.Id}:{x.Value.Name}")));
        Check(ref failures, "Buckets", "7=[70,71],8=[80]", clone.Buckets is null ? "null"
            : string.Join(",", clone.Buckets.OrderBy(static x => x.Key)
                .Select(static x => $"{x.Key}=[{string.Join(",", x.Value)}]")));

        Check(ref failures, "ByStatus", "Unknown=0,Open=1", clone.ByStatus is null ? "null"
            : string.Join(",", clone.ByStatus.OrderBy(static x => x.Key)
                .Select(static x => $"{x.Key}={x.Value}")));
        Check(ref failures, "ToStatus", "1=Closed,2=Unknown", clone.ToStatus is null ? "null"
            : string.Join(",", clone.ToStatus.OrderBy(static x => x.Key)
                .Select(static x => $"{x.Key}={x.Value}")));

        // hand-written serializers, reached via SerializerCache.Get<TProvider, T>()
        Check(ref failures, "Barcode", "X-9", clone.Barcode?.Code);
        Check(ref failures, "Gauge", original.Gauge.Value, clone.Gauge.Value);
        Check(ref failures, "Batch", original.Batch.Value, clone.Batch.Value);

        // the declaration-served open mapping, closed over int and (nullable) string
        Check(ref failures, "Score", original.Score.Count, clone.Score.Count);
        Check(ref failures, "Bonus", original.Bonus?.Count, clone.Bonus?.Count);

        // the collection families, one per RepeatedSerializer factory
        Check(ref failures, "Codes", "11,12,13", Join(clone.Codes));
        Check(ref failures, "Team", "4:cat", clone.Team is null ? "null"
            : string.Join(",", clone.Team.Select(static x => $"{x.Id}:{x.Name}")));
        Check(ref failures, "Tags", "blue,red", clone.Tags is null ? "null"
            : string.Join(",", clone.Tags.OrderBy(static x => x, StringComparer.Ordinal)));
        Check(ref failures, "Pending", "21,22", Join(clone.Pending));
        Check(ref failures, "Ranks", "3,5,9", Join(clone.Ranks));
        Check(ref failures, "Frozen", "31,32", clone.Frozen.IsDefault ? "default" : Join(clone.Frozen));
        Check(ref failures, "Inbox", "41,42", Join(clone.Inbox));
        Check(ref failures, "Archive", "51,52", Join(clone.Archive));
        Check(ref failures, "Quotas", "a=61,b=62", clone.Quotas is null ? "null"
            : string.Join(",", clone.Quotas.OrderBy(static x => x.Key, StringComparer.Ordinal)
                .Select(static x => $"{x.Key}={x.Value}")));

        // the four types with their own core-serializer entries
        Check(ref failures, "Day", original.Day, clone.Day);
        Check(ref failures, "Time", original.Time, clone.Time);
        Check(ref failures, "Offset", original.Offset, clone.Offset);
        Check(ref failures, "Host (parseable)", "10.0.0.7", clone.Host?.ToString());

        // written because it differs from the declared default, and so must not come back as the
        // initialiser's 5
        Check(ref failures, "Retries ([DefaultValue])", 0, clone.Retries);

        // callbacks: both families, and both ends of each
        Check(ref failures, "Audited write hooks", "bs;as;", original.Audited.Trace);
        Check(ref failures, "Audited read hooks", "bd;ad;", clone.Audited?.Trace);
        Check(ref failures, "Audited.Value", "a", clone.Audited?.Value);
        Check(ref failures, "Tracked write hooks", "os;od;", original.Tracked.Trace);
        Check(ref failures, "Tracked read hooks", "ds;dd;", clone.Tracked?.Trace);
        Check(ref failures, "Tracked.Value", "t", clone.Tracked?.Value);

        // Specified replaces the trivial-value guard, so an explicit zero is written and the flag is
        // set again on read; ShouldSerialize suppresses a non-zero value entirely
        Check(ref failures, "Explicit zero written", 0, clone.Conditional?.Explicit);
        Check(ref failures, "ExplicitSpecified set on read", true, clone.Conditional?.ExplicitSpecified);
        Check(ref failures, "Hidden suppressed", 0, clone.Conditional?.Hidden);

        // implicit numbering, by name; and private fields reached in both directions
        Check(ref failures, "Sorted", "a/m/z",
            $"{clone.Sorted?.Apple}/{clone.Sorted?.Mango}/{clone.Sorted?.Zebra}");
        Check(ref failures, "Ledger.Balance", 1234, clone.Ledger?.Balance);
        Check(ref failures, "Ledger.Owner", "ann", clone.Ledger?.Owner);

        // DataFormat: ZigZag needs a read hint, FixedSize picks its width, Group reframes
        Check(ref failures, "Delta (zigzag)", original.Delta, clone.Delta);
        Check(ref failures, "Ticks (fixed)", original.Ticks, clone.Ticks);
        Check(ref failures, "Grouped", "5:dan", clone.Grouped is null ? "null"
            : $"{clone.Grouped.Id}:{clone.Grouped.Name}");
        Check(ref failures, "GroupedTeam", "6:eve", clone.GroupedTeam is null ? "null"
            : string.Join(",", clone.GroupedTeam.Select(static x => $"{x.Id}:{x.Name}")));

        // SkipConstructor: the value survives, and the constructor demonstrably did not run
        Check(ref failures, "Blank.Value", original.Blank.Value, clone.Blank?.Value);
        Check(ref failures, "Blank ctor skipped", null, clone.Blank?.Stamp);

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

        // [ProtoDataFormat]: the type-scoped default alone must be enough to pick FixedSize - see
        // the printed hex dump for the 16-byte proof (0A-10 + 16 bytes, not 0A-24 + a 36-char string)
        Check(ref failures, "Vault.Entry", original.Vault.Entry, clone.Vault?.Entry);

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

        // The .proto-generated half: a descriptor tree built by hand, round-tripped, and compared by
        // re-serializing. Worth doing by shape rather than by field-by-field assertion, because the
        // point is the *closure* - FileDescriptorSet reaches some thirty generated contracts.
        failures += CheckDescriptors(model);

        // Extensible.AppendValue resolves its serializer reflectively, so it cannot work under native
        // AOT. It used to *discard* the failed result and report success - silent data loss on an API
        // whose whole purpose is round-trip fidelity. Both halves are pinned here because the correct
        // behaviour differs by runtime, and only a native publish exercises the half that was broken.
        failures += CheckAppendValue();

        // and the bytes must be stable across a second pass
        using var second = new MemoryStream();
        model.Serialize(second, clone);
        Check(ref failures, "round-trip bytes", BitConverter.ToString(bytes),
            BitConverter.ToString(second.ToArray()));

        Console.WriteLine(failures == 0 ? "AOT smoke test PASSED" : $"AOT smoke test FAILED ({failures})");
        return failures;
    }

    /// <summary>
    /// Round-trip a <c>descriptor.proto</c> tree through the generated model.
    /// </summary>
    /// <remarks>
    /// These DTOs exercise shapes the hand-written contracts here do not: every collection is
    /// getter-only (so the read goes through an <c>[UnsafeAccessor]</c> on the backing field), every
    /// optional scalar carries <c>ShouldSerialize{Name}()</c>, every message implements
    /// <c>IExtensible</c>, and <c>DescriptorProto</c> nests inside itself.
    /// </remarks>
    private static int CheckDescriptors(SmokeModel model)
    {
        var failures = 0;

        var set = new FileDescriptorSet();
        var file = new FileDescriptorProto
        {
            Name = "smoke.proto",
            Package = "smoke",
            Syntax = "proto3",
        };
        file.Dependencies.Add("google/protobuf/any.proto");

        var message = new DescriptorProto { Name = "Order" };
        message.Fields.Add(new FieldDescriptorProto
        {
            Name = "id",
            Number = 1,
            type = FieldDescriptorProto.Type.TypeInt32,
            label = FieldDescriptorProto.Label.LabelOptional,
            JsonName = "id",
        });
        // an explicit zero, which only survives because ShouldSerializeNumber() says it was set
        message.Fields.Add(new FieldDescriptorProto
        {
            Name = "zero",
            Number = 0,
            type = FieldDescriptorProto.Type.TypeString,
        });
        // ...and the recursive case
        message.NestedTypes.Add(new DescriptorProto { Name = "Line" });

        var @enum = new EnumDescriptorProto { Name = "Status" };
        @enum.Values.Add(new EnumValueDescriptorProto { Name = "UNKNOWN", Number = 0 });
        @enum.Values.Add(new EnumValueDescriptorProto { Name = "OPEN", Number = 1 });

        file.MessageTypes.Add(message);
        file.EnumTypes.Add(@enum);
        set.Files.Add(file);

        using var ms = new MemoryStream();
        model.Serialize(ms, set);
        var bytes = ms.ToArray();
        Console.WriteLine($"descriptor set: {bytes.Length} bytes");

        ms.Position = 0;
        var clone = model.Deserialize<FileDescriptorSet>(ms);

        Check(ref failures, "descriptor file count", 1, clone.Files.Count);
        var cloned = clone.Files.Count == 1 ? clone.Files[0] : null;
        Check(ref failures, "descriptor file name", "smoke.proto", cloned?.Name);
        Check(ref failures, "descriptor package", "smoke", cloned?.Package);
        Check(ref failures, "descriptor dependency", "google/protobuf/any.proto",
            cloned?.Dependencies.Count == 1 ? cloned.Dependencies[0] : null);
        Check(ref failures, "descriptor message", "Order",
            cloned?.MessageTypes.Count == 1 ? cloned.MessageTypes[0].Name : null);
        Check(ref failures, "descriptor field type", FieldDescriptorProto.Type.TypeInt32,
            cloned?.MessageTypes[0].Fields.Count == 2 ? cloned.MessageTypes[0].Fields[0].type : default);
        Check(ref failures, "descriptor nested type", "Line",
            cloned?.MessageTypes[0].NestedTypes.Count == 1
                ? cloned.MessageTypes[0].NestedTypes[0].Name : null);
        Check(ref failures, "descriptor enum value", "OPEN",
            cloned?.EnumTypes.Count == 1 && cloned.EnumTypes[0].Values.Count == 2
                ? cloned.EnumTypes[0].Values[1].Name : null);

        using var again = new MemoryStream();
        model.Serialize(again, clone);
        Check(ref failures, "descriptor round-trip bytes", BitConverter.ToString(bytes),
            BitConverter.ToString(again.ToArray()));

        return failures;
    }

    /// <summary>
    /// <c>Extensible.AppendValue</c> and <c>GetValue</c>, which now keep <c>TValue</c> all the way
    /// down instead of degrading to the reflective auxiliary path.
    /// </summary>
    /// <remarks>
    /// The assertion is deliberately strict — the value must come back — because "either it works or
    /// it throws" was the *previous* fix, and would pass whether or not the typed path is being
    /// taken. Under a native publish this is the only thing here that proves it.
    /// </remarks>
    private static int CheckAppendValue()
    {
        var failures = 0;
        var note = new Note { Text = "hi" };
        Extensible.AppendValue(note, 42, 123);
        Check(ref failures, "AppendValue round-trips", 123, Extensible.GetValue<int>(note, 42));
        return failures;
    }

    private static void Check<T>(ref int failures, string what, T expected, T actual)
    {
        if (Equals(expected, actual)) return;
        Console.Error.WriteLine($"  MISMATCH {what}: expected '{expected}', got '{actual}'");
        failures++;
    }

    private static string Join<T>(IEnumerable<T> values)
        => values is null ? "null" : string.Join(",", values);
}
