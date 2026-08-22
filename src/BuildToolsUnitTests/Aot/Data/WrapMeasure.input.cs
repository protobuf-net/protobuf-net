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
using System.Collections.Generic;

namespace AotFixtures.WrapMeasure;

public enum Tint { None, Deep }

[ProtoContract]
public class Leaf
{
    [ProtoMember(1)] public int Id { get; set; }
}

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

// The COLLECTION scopes, which have the opposite inner rule to the lone form above: an element
// wrapper carries OptionWrappedValueFieldPresence, so its inner field is written even for a zero -
// [0] is 0A-02-08-00 where the lone int? 0 is 0A-00. A collection wrapper renumbers its contents to
// field 1 and length-prefixes them, so null and empty become distinguishable (nothing vs 0A-00).
[ProtoContract]
public class Crate
{
    // element wrapping: a null element is an EMPTY wrapper, not an absent one
    [ProtoMember(1), NullWrappedValue] public List<int?> Counts { get; set; }
    [ProtoMember(2), NullWrappedValue(AsGroup = true)] public List<int?> Grouped { get; set; }
    [ProtoMember(3), NullWrappedValue] public List<string> Labels { get; set; }
    [ProtoMember(4), NullWrappedValue] public List<Tint?> Shades { get; set; }

    // collection wrapping: null writes nothing, empty writes an empty wrapper
    [ProtoMember(5), NullWrappedCollection] public List<int> Sizes { get; set; }
    [ProtoMember(6), NullWrappedCollection(AsGroup = true)] public List<int> GroupedSizes { get; set; }
    [ProtoMember(7), NullWrappedCollection] public List<string> Names { get; set; }

    // ...and the two together, at different scopes
    [ProtoMember(8), NullWrappedCollection, NullWrappedValue] public List<int?> Both { get; set; }
    [ProtoMember(21), NullWrappedValue] public List<int?> Far { get; set; }

    // MESSAGE elements, which reach the target's own Measure_ with a NULL slot buffer - a wrapped
    // element is written by the stateful engine, so this sub-tree reserves nothing. An EMPTY
    // message is the sharp sample: its wrapper is 0A-02-0A-00, i.e. a present inner field over a
    // zero-length body, where a NULL element is 0A-00
    [ProtoMember(10), NullWrappedValue] public List<Leaf> Parts { get; set; }
    [ProtoMember(11), NullWrappedValue(AsGroup = true)] public List<Leaf> GroupedParts { get; set; }
    [ProtoMember(12), NullWrappedCollection] public List<Leaf> Bundle { get; set; }
    [ProtoMember(13), NullWrappedCollection, NullWrappedValue] public List<Leaf> BundledParts { get; set; }

    [ProtoMember(9)] public int Trailer { get; set; }
}

// A map wraps exactly as a collection does, in both scopes - but its VALUE guard is unchanged and
// still asks HasNonTrivialValue of the UNWRAPPED value, so a wrapped int? of 0 and one of null are
// BOTH omitted: the wrapper never gets to tell them apart. That is protobuf-net's behaviour, not
// ours, and the samples pin it rather than wish otherwise.
[ProtoContract]
public class Ledger
{
    [ProtoMember(1), NullWrappedValue] public Dictionary<int, int?> Counts { get; set; }
    [ProtoMember(2), NullWrappedValue] public Dictionary<int, string> Notes { get; set; }
    [ProtoMember(3), NullWrappedValue(AsGroup = true)] public Dictionary<int, int?> Grouped { get; set; }
    [ProtoMember(4), NullWrappedCollection] public Dictionary<int, int> Whole { get; set; }
    [ProtoMember(5), NullWrappedCollection, NullWrappedValue] public Dictionary<int, int?> Both { get; set; }
    [ProtoMember(20), NullWrappedValue] public Dictionary<string, string> Far { get; set; }
    [ProtoMember(6)] public int Trailer { get; set; }
}

// nests it, so something above needs a length and the measure actually runs
[ProtoContract]
public class Carton
{
    [ProtoMember(1)] public Boxed Inner { get; set; }
    [ProtoMember(2)] public int Tag { get; set; }
    [ProtoMember(3)] public Crate Packed { get; set; }
    [ProtoMember(4)] public Ledger Book { get; set; }
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

        new Crate(),
        // every collection present and EMPTY: nothing for element wrapping, an empty wrapper for
        // collection wrapping - the pair that a shared rule would get wrong
        new Crate { Counts = [], Grouped = [], Labels = [], Shades = [], Sizes = [], GroupedSizes = [], Names = [], Both = [], Far = [] },
        // a null element in every element-wrapped scope, and a zero beside it: the zero is written
        new Crate { Counts = [1, null, 0], Grouped = [null, 2, 0], Labels = ["", null, "a"], Shades = [Tint.Deep, null, Tint.None], Far = [null, 7] },
        new Crate { Sizes = [1, 0, 300], GroupedSizes = [0, 4], Names = ["", "bc"] },
        new Crate { Both = [1, null, 0], Trailer = 9 },
        // an EMPTY message, a null, and a populated one, in every message scope
        new Crate { Parts = [new Leaf { Id = 7 }, null, new Leaf()], GroupedParts = [new Leaf { Id = 8 }, null] },
        new Crate { Bundle = [new Leaf { Id = 9 }, new Leaf()], BundledParts = [null, new Leaf { Id = 10 }] },
        new Crate { Bundle = [], BundledParts = [] },
        new Carton { Packed = new Crate { Counts = [null], Sizes = [] }, Tag = 5 },

        new Ledger(),
        // a zero and a null value are byte-identical here - both omitted from the entry
        new Ledger { Counts = new() { [1] = 2, [2] = 0, [3] = null } },
        // an EMPTY string value is written, where a null one is not
        new Ledger { Notes = new() { [1] = "a", [2] = "", [3] = null } },
        new Ledger { Grouped = new() { [1] = 5, [2] = null } },
        // null vs empty is the whole point of the collection scope
        new Ledger { Whole = new() },
        new Ledger { Whole = new() { [1] = 2, [0] = 0 } },
        new Ledger { Both = new() { [1] = 2, [2] = null }, Trailer = 8 },
        new Ledger { Far = new() { ["k"] = "v", ["e"] = "" } },
        new Carton { Book = new Ledger { Counts = new() { [1] = 0 } }, Tag = 6 },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Boxed))]
[ProtoSerializable(typeof(Crate))]
[ProtoSerializable(typeof(Ledger))]
[ProtoSerializable(typeof(Leaf))]
[ProtoSerializable(typeof(Carton))]
public partial class WrapMeasureModel : TypeModel { }
