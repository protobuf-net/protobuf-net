// A contract whose ONLY null-wrapping is the lone [NullWrappedValue] form, so it stays MEASURABLE
// (gap B42). Wrapped.input.cs cannot cover this: it also carries wrapped collections and maps,
// which have no measure arm, and one blocked member takes the whole contract.
//
// The write here stays on WriteAny - only the SIZE is arithmetic, which is the recurring point that
// measure and write eligibility are independent.
//
// The samples are chosen to break a naive measure. The inner field follows
// IValueChecker<T>.HasNonTrivialValue, NOT the member's own write guard, and the three disagree:
//   int? 0    -> 0A-00          wrapper present, inner OMITTED
//   string "" -> 0A-02-0A-00    inner PRESENT: protobuf-net writes "" for compat
//   enum? 0   -> 0A-02-08-00    inner PRESENT: EnumSerializer supplies no checker
// ...so a zero, an empty string, an empty array and a zero enum all have to be here, and each one
// exercises a different arm. The group form drops the length prefix for a start/end tag pair.
using ProtoBuf;
using ProtoBuf.Meta;

namespace AotFixtures.WrapMeasure;

public enum Tint { None, Deep }

[ProtoContract]
public class Boxed
{
    [ProtoMember(1), NullWrappedValue] public int? Count { get; set; }
    [ProtoMember(2), NullWrappedValue(AsGroup = true)] public int? Grouped { get; set; }
    [ProtoMember(3), NullWrappedValue] public string Label { get; set; }
    [ProtoMember(4), NullWrappedValue(AsGroup = true)] public string GroupedLabel { get; set; }
    [ProtoMember(5), NullWrappedValue] public byte[] Blob { get; set; }
    [ProtoMember(6), NullWrappedValue] public Tint? Shade { get; set; }
    [ProtoMember(7), NullWrappedValue] public bool? Flag { get; set; }
    [ProtoMember(8), NullWrappedValue] public double? Ratio { get; set; }
    // a field number past 15, so its tag is two bytes and a folded-tag mistake shows up
    [ProtoMember(20), NullWrappedValue] public long? Big { get; set; }
    // an ordinary member beside them, so the whole contract's length is exercised
    [ProtoMember(9)] public int Trailer { get; set; }
}

// nests it, so something above needs a length and the measure actually runs
[ProtoContract]
public class Carton
{
    [ProtoMember(1)] public Boxed Inner { get; set; }
    [ProtoMember(2)] public int Tag { get; set; }
}

public static class WrapMeasureSamples
{
    public static object[] Values =>
    [
        new Boxed(),
        // every wrapper present, every inner value TRIVIAL - the case the three rules disagree on
        new Boxed { Count = 0, Grouped = 0, Label = "", GroupedLabel = "", Blob = [], Shade = Tint.None, Flag = false, Ratio = 0, Big = 0 },
        new Boxed { Count = 1, Grouped = 2, Label = "a", GroupedLabel = "bc", Blob = [7, 8], Shade = Tint.Deep, Flag = true, Ratio = 1.5, Big = 300 },
        // a payload long enough that its own length prefix is two bytes
        new Boxed { Label = new string('x', 200) },
        new Boxed { Count = 0, Trailer = 42 },
        new Carton(),
        new Carton { Inner = new Boxed { Count = 0, Label = "" }, Tag = 3 },
        new Carton { Inner = new Boxed { Count = 5, Big = long.MaxValue }, Tag = 4 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Boxed))]
[ProtoSerializable(typeof(Carton))]
public partial class WrapMeasureModel : TypeModel { }
