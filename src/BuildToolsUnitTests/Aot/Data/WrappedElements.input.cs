using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.WrappedElements;

// docs/nullwrappers.md draws the line at the *scope*, not the type: a lone [NullWrappedValue] is
// valid only on a nullable scalar and throws otherwise, but in a collection "any scalar or message
// type will be accepted (but not nested collections)". Probed against RuntimeTypeModel rather than
// taken from the doc - message, nullable enum, nullable BCL, string and map elements all work.
[ProtoContract]
public class Payload
{
    [ProtoMember(1)] public int Id { get; set; }
}

public enum Shade { None, Light, Dark }

[ProtoContract]
public class Wrapped
{
    // a message element: the wrapper is what lets a null appear in the list at all
    [ProtoMember(1), NullWrappedValue] public List<Payload> Messages { get; set; } = new();

    // the array form, whose factory differs
    [ProtoMember(2), NullWrappedValue] public Payload[] Array { get; set; }

    // a nullable enum element - note the services type has to expose ISerializerProxy<Shade?>
    [ProtoMember(3), NullWrappedValue] public List<Shade?> Shades { get; set; } = new();

    // a nullable compatibility-level BCL element
    [ProtoMember(4), NullWrappedValue] public List<DateTime?> Dates { get; set; } = new();

    // a string element: already nullable, but the wrapper distinguishes null from empty
    [ProtoMember(5), NullWrappedValue] public List<string> Names { get; set; } = new();

    // a map value, which wraps exactly as a collection element does
    [ProtoMember(6), NullWrappedValue] public Dictionary<int, Payload> ById { get; set; } = new();

    // ...and the same shapes with the group form of the wrapper
    [ProtoMember(7), NullWrappedValue(AsGroup = true)] public List<Payload> Grouped { get; set; } = new();

    // without the attribute a nullable element is an ordinary element: the encoding is unchanged and
    // it only faults if a null actually turns up, which is the same trade already made for scalars
    [ProtoMember(8)] public List<Shade?> BareShades { get; set; } = new();
    [ProtoMember(9)] public List<DateTime?> BareDates { get; set; } = new();

    // the *non*-nullable BCL element, which had the same bug: these are length-prefixed, not
    // varints, so an element wire type defaulting to varint disagreed with ref-emit
    [ProtoMember(10)] public List<DateTime> Plain { get; set; } = new();
    [ProtoMember(11)] public List<decimal> Amounts { get; set; } = new();
    [ProtoMember(12)] public Dictionary<int, Guid> Ids { get; set; } = new();
}

public static class WrappedElementsSamples
{
    public static object[] Values =>
    [
        new Wrapped(),
        new Wrapped { Messages = { new Payload { Id = 1 }, null, new Payload { Id = 2 } } },
        new Wrapped { Array = [new Payload { Id = 3 }, null] },
        new Wrapped { Shades = { Shade.Light, null, Shade.None } },
        new Wrapped { Dates = { new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), null } },
        new Wrapped { Names = { "a", null, "" } },
        new Wrapped { ById = { [1] = new Payload { Id = 4 }, [2] = null } },
        new Wrapped { Grouped = { new Payload { Id = 5 }, null } },
        new Wrapped { BareShades = { Shade.Dark }, BareDates = { new DateTime(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc) } },
        new Wrapped
        {
            Plain = { new DateTime(2021, 6, 7, 8, 9, 10, DateTimeKind.Utc) },
            Amounts = { 1.25m, -3m },
            Ids = { [1] = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e") },
        },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Wrapped))]
public partial class WrappedElementsModel : TypeModel
{
}
