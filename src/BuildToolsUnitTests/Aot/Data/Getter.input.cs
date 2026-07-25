using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.Getter;

// A getter-only property still round-trips: the read runs exactly as it would otherwise, but the
// result is discarded rather than assigned. For a collection or sub-message that is the whole
// mechanism - the instance the property already holds is passed in and mutated. For a scalar the
// value really is read and thrown away, which is what ref-emit does.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id { get; set; }
}

public enum Shade { None, Light }

[ProtoContract]
public class Getters
{
    [ProtoMember(1)] public List<int> Numbers { get; } = new();
    [ProtoMember(2)] public Dictionary<int, string> Map { get; } = new();
    [ProtoMember(3)] public Nested Child { get; } = new();
    [ProtoMember(10)] public int[] Array { get; } = [];

    // read and discarded: these write, but never come back
    [ProtoMember(4)] public int Value { get; }
    [ProtoMember(5)] public string Text { get; }
    [ProtoMember(6)] public byte[] Blob { get; }
    [ProtoMember(7)] public int? Maybe { get; }
    [ProtoMember(8)] public Shade Colour { get; }
    [ProtoMember(9)] public DateTime When { get; }
}

public static class GetterSamples
{
    public static object[] Values =>
    [
        new Getters(),
        new Getters { Numbers = { 1, 2 } },
        new Getters { Map = { [3] = "c" } },
        new Getters { Child = { Id = 4 } },
        new Getters { Numbers = { 5 }, Map = { [6] = "f" }, Child = { Id = 7 } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Getters))]
public partial class GetterModel : TypeModel
{
}
