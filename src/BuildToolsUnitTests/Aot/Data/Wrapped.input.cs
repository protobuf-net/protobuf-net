using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.Wrapped;

// protobuf has no concept of null, so [NullWrappedValue] inserts a conceptual message layer to make
// one expressible - see docs/nullwrappers.md. A lone value uses a different *API* (ReadAny/WriteAny);
// on a collection or map it is pure features composition, and adds field presence so that a null
// element is distinguishable from a zero one. [NullWrappedCollection] applies the same trick to the
// collection itself, so a null collection differs from an empty one, and the two compose.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id { get; set; }
}

public enum Shade { None, Light }

[ProtoContract]
public class Wrapped
{
    // a lone value: only legal on something both scalar and nullable
    [ProtoMember(1), NullWrappedValue] public int? Value { get; set; }
    [ProtoMember(2), NullWrappedValue(AsGroup = true)] public int? Grouped { get; set; }
    [ProtoMember(3), NullWrappedValue] public string Text { get; set; }
    [ProtoMember(11), NullWrappedValue] public byte[] Blob { get; set; }

    // an enum has no serializer in PrimaryTypeProvider, so the services type supplies one
    [ProtoMember(12), NullWrappedValue] public Shade? Colour { get; set; }

    // collections: the wrapping applies to each element, which is what allows a null *inside* one
    [ProtoMember(4), NullWrappedValue] public List<int?> Ids { get; } = new();
    [ProtoMember(5), NullWrappedValue] public List<Nested> Items { get; } = new();
    [ProtoMember(6), NullWrappedValue(AsGroup = true)] public List<Nested> GroupedItems { get; } = new();

    // a map: the wrapping applies to the value only - the key cannot be null
    [ProtoMember(7), NullWrappedValue] public Dictionary<int, Nested> Keyed { get; } = new();
    [ProtoMember(13), NullWrappedValue(AsGroup = true)] public Dictionary<int, Nested> GroupedKeyed { get; } = new();
    [ProtoMember(14), NullWrappedValue] public Dictionary<int, int?> Scalars { get; } = new();

    // the collection itself, so null and empty are distinguishable
    [ProtoMember(8), NullWrappedCollection] public List<int> Numbers { get; set; }
    [ProtoMember(9), NullWrappedCollection(AsGroup = true)] public List<int> GroupedNumbers { get; set; }

    // the two compose, at different scopes
    [ProtoMember(10), NullWrappedCollection, NullWrappedValue] public List<Nested> Both { get; set; }

    // and a nullable element with *no* wrapping is an ordinary element: it only faults at runtime if
    // a null actually turns up, which is why there is no null in this one's samples
    [ProtoMember(15)] public List<int?> Bare { get; } = new();
}

public static class WrappedSamples
{
    public static object[] Values =>
    [
        new Wrapped(),
        new Wrapped { Value = 0, Grouped = 0, Text = "", Blob = [], Colour = Shade.None },
        new Wrapped { Value = 1, Grouped = 2, Text = "a", Blob = [3], Colour = Shade.Light },

        // the point of the exercise: nulls inside collections
        new Wrapped { Ids = { 1, null, 0, 2 } },
        new Wrapped { Items = { new Nested { Id = 1 }, null } },
        new Wrapped { GroupedItems = { null, new Nested { Id = 2 } } },
        new Wrapped { Keyed = { [1] = null, [2] = new Nested { Id = 3 } } },
        new Wrapped { GroupedKeyed = { [4] = null } },
        new Wrapped { Scalars = { [5] = null, [6] = 0 } },

        // a null collection versus an empty one
        new Wrapped { Numbers = null },
        new Wrapped { Numbers = [] },
        new Wrapped { Numbers = [7, 8] },
        new Wrapped { GroupedNumbers = [] },
        new Wrapped { Both = null },
        new Wrapped { Both = [] },
        new Wrapped { Both = [null, new Nested { Id = 9 }] },

        new Wrapped { Bare = { 10, 0 } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Wrapped))]
public partial class WrappedModel : TypeModel
{
}
