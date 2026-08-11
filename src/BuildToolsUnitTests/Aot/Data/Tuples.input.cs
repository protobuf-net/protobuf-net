using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.Tuples;

// no contract attribute of any kind: that is the *only* situation in which tuple detection engages
// (MetaType.GetContractFamily). A [ProtoContract] here would defeat it and yield a serializer that
// cannot construct anything.
public class ClassTuple
{
    public ClassTuple(int a, string b) { A = a; B = b; }
    public int A { get; }
    public string B { get; }
}

public readonly struct StructTuple
{
    public StructTuple(int x, string y) { X = x; Y = y; }
    public int X { get; }
    public string Y { get; }
}

// public *mutable* fields, allowed only because "Tuple" appears in the type name
public struct NamedLikeATuple
{
    public NamedLikeATuple(int first, int second) { First = first; Second = second; }
    public int First;
    public int Second;
}

public static class TuplesSamples
{
    public static object[] Values =>
    [
        new ClassTuple(0, null),                 // every member at its default: still written
        new ClassTuple(1, "a"),
        new ClassTuple(-1, ""),
        new StructTuple(0, null),
        new StructTuple(2, "b"),
        new NamedLikeATuple(0, 0),
        new NamedLikeATuple(3, -4),
        new KeyValuePair<int, string>(0, null),
        new KeyValuePair<int, string>(5, "c"),

        // the BCL tuples: generic *and* name-exempt. ValueTuple's Item1/Item2 are public mutable
        // fields, so it qualifies only because of the name rule.
        new System.ValueTuple<int, string>(0, null),
        new System.ValueTuple<int, string>(6, "d"),
        new System.Tuple<int, string>(0, null),
        new System.Tuple<int, string>(7, "e"),
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(ClassTuple))]
[ProtoSerializable(typeof(StructTuple))]
[ProtoSerializable(typeof(NamedLikeATuple))]
[ProtoSerializable(typeof(KeyValuePair<int, string>))]
[ProtoSerializable(typeof(System.ValueTuple<int, string>))]
[ProtoSerializable(typeof(System.Tuple<int, string>))]
public partial class TuplesModel : TypeModel
{
}
