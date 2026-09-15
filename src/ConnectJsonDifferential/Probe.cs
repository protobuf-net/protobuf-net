using System.Collections.Generic;
using ProtoBuf;

namespace ProtoBuf.ConnectJsonDifferential;

// Shapes with no canonical JSON form. These are not expected to work; they are here so that "does
// not work" means *refused with a diagnostic* rather than *emits code the consumer's build rejects*,
// which is the worst failure a generator has available. The binary serializer for every one of them
// must still be emitted - the JSON surface is a subset, and narrowing it must not narrow the other.
//
// ONE REFUSAL REASON PER CONTRACT, deliberately. A contract bails at its first bad member, so a
// single holds-everything type reports one reason and silently masks the rest - which it did, hiding
// three of the five below until this was split.

/// <summary>A collection the JSON reader cannot construct: it builds a <c>List&lt;T&gt;</c>.</summary>
[ProtoContract]
public class HasHashSet
{
    [ProtoMember(1)] public HashSet<int> Unique { get; set; } = new();
}

/// <summary>Likewise; a <c>Queue&lt;T&gt;</c> is neither a list nor assignable from one.</summary>
[ProtoContract]
public class HasQueue
{
    [ProtoMember(1)] public Queue<int> Pending { get; set; } = new();
}

/// <summary>
/// A <c>DateTime</c> at the default compatibility level, where it is a protobuf-net message rather
/// than a <c>google.protobuf.Timestamp</c> and so has no JSON form at all.
/// </summary>
[ProtoContract]
public class HasLevel200DateTime
{
    [ProtoMember(1)] public DateTime When { get; set; }
}

/// <summary>
/// A <c>bool</c> key, which protobuf-net does not model as a map even though protobuf allows one.
/// </summary>
/// <remarks>
/// <b>The obvious reading of this is wrong, and I had it wrong first.</b> <c>bool</c> <em>is</em> a
/// legal protobuf map key - verified against plain protoc, which accepts <c>map&lt;bool, V&gt;</c>.
/// It is protobuf-net's <c>IsValidKey</c> that omits <c>TypeCode.Boolean</c>, so it emits a
/// <c>repeated KeyValuePair_Boolean_Int32</c> instead. That schema is perfectly valid; it just is not
/// a map, and canonical JSON has a map form only for a map. Same story for <c>char</c>, <c>nint</c>
/// and <c>nuint</c>. The genuinely illegal keys are float, double, bytes and message - and enum.
/// </remarks>
[ProtoContract]
public class HasBoolKeyedMap
{
    [ProtoMember(1)] public Dictionary<bool, int> BoolKeyed { get; set; } = new();
}

/// <summary>
/// An <b>enum</b> map key, which is refused for a sharper reason than the others.
/// </summary>
/// <remarks>
/// protobuf-net believes this one <em>is</em> a valid map - <c>IsValidProtobufMap</c> accepts an enum
/// key - and <c>GetProto</c> duly emits <c>map&lt;Shade,int32&gt;</c>. protoc rejects that outright:
/// <em>"Key in map fields cannot be enum types."</em> The spec allows any integral or string type and
/// nothing else. So protobuf-net generates a schema no protobuf tool will compile, which is a bug in
/// the schema generator rather than anything to do with JSON - found here only because the breadth
/// sweep tried to add the cell.
/// </remarks>
[ProtoContract]
public class HasEnumKeyedMap
{
    [ProtoMember(1)] public Dictionary<Shade, int> EnumKeyed { get; set; } = new();
}

/// <summary>Shapes that <em>do</em> work, kept beside the refusals so the split stays honest.</summary>
[ProtoContract]
public class Supported
{
    [ProtoMember(1)] public string[] Names { get; set; }
    [ProtoMember(2)] public int? Maybe { get; set; }
    [ProtoMember(3)] public List<int> ReadOnly { get; } = new();
}

[ProtoContract]
[ProtoInclude(100, typeof(Derived))]
public class Base
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class Derived : Base
{
    [ProtoMember(1)] public string Extra { get; set; }
}

/// <summary>Reaches a hierarchy, so it is dropped by cascade rather than on its own merits.</summary>
[ProtoContract]
public class Holder
{
    [ProtoMember(1)] public Base Item { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(HasHashSet))]
[ProtoSerializable(typeof(HasQueue))]
[ProtoSerializable(typeof(HasLevel200DateTime))]
[ProtoSerializable(typeof(HasBoolKeyedMap))]
[ProtoSerializable(typeof(HasEnumKeyedMap))]
[ProtoSerializable(typeof(Supported))]
[ProtoSerializable(typeof(Holder))]
[ProtoSerializable(typeof(HasSurrogate))]
[ProtoSerializable(typeof(HasTuple))]
public partial class ProbeModel : ProtoBuf.Meta.TypeModel
{
}

// --- the two remaining gaps, probed rather than assumed ---------------------------------------

/// <summary>A surrogated contract: the wire shape - and so the JSON - is the surrogate's.</summary>
[ProtoContract(Surrogate = typeof(MoneySurrogate))]
public struct Money
{
    public Money(long units, Currency currency) { Units = units; Currency = currency; }
    public long Units { get; }
    public Currency Currency { get; }
}

[ProtoContract]
public class MoneySurrogate
{
    [ProtoMember(1)] public long Units { get; set; }
    [ProtoMember(2)] public Currency Currency { get; set; }

    public static implicit operator MoneySurrogate(Money value)
        => new() { Units = value.Units, Currency = value.Currency };

    public static implicit operator Money(MoneySurrogate value)
        => value is null ? default : new Money(value.Units, value.Currency);
}

[ProtoContract]
public enum Currency { Unknown = 0, Gbp = 1, Usd = 2 }

[ProtoContract]
public class HasSurrogate
{
    [ProtoMember(1)] public Money Price { get; set; }
}

/// <summary>
/// An auto-tuple: no contract attribute, no public setters, one matching constructor.
/// </summary>
/// <remarks>
/// Its <em>read</em> is a different emit shape - locals per constructor parameter, constructed at the
/// end - because there is nothing to assign to. Whether the JSON emitter knows that is the question.
/// </remarks>
public sealed class Point
{
    public Point(int x, int y) { X = x; Y = y; }
    public int X { get; }
    public int Y { get; }
}

[ProtoContract]
public class HasTuple
{
    [ProtoMember(1)] public Point Where { get; set; }
}
