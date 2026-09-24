using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.Lists;

[ProtoContract]
public class Inner
{
    [ProtoMember(1)] public int Value { get; set; }
    [ProtoMember(2)] public string Label { get; set; }
}

// a repeated enum needs the services type to expose ISerializerProxy<TEnum>, because
// RepeatedSerializer resolves an ISerializer<TEnum> from the model rather than writing it inline
public enum Colour { None = 0, Red = 1, Blue = 2 }

public enum Small : byte { Zero = 0, Big = 200 }

[ProtoContract]
public class Repeated
{
    // arrays vs List<T>; the only difference is CreateVector<T> vs CreateList<T>
    [ProtoMember(1)] public int[] Int32Array { get; set; }
    [ProtoMember(2)] public List<int> Int32List { get; set; }

    // element wire type drives the features constant
    [ProtoMember(3)] public double[] DoubleArray { get; set; }
    [ProtoMember(4)] public float[] SingleArray { get; set; }
    [ProtoMember(5)] public List<bool> BoolList { get; set; }
    [ProtoMember(6)] public string[] StringArray { get; set; }
    [ProtoMember(7)] public List<string> StringList { get; set; }

    // message elements: same shape, plus 'this' as the sub-serializer
    [ProtoMember(9)] public List<Inner> Messages { get; set; }
    [ProtoMember(10)] public Inner[] MessageArray { get; set; }

    [ProtoMember(11)] public int Scalar { get; set; }

    // repeated enums, including a non-int underlying type
    [ProtoMember(12)] public List<Colour> Colours { get; set; }
    [ProtoMember(13)] public Small[] Smalls { get; set; }

    // ... and a plain enum member alongside, which is still written inline
    [ProtoMember(14)] public Colour SingleColour { get; set; }
}

public static class ListsSamples
{
    public static object[] Values =>
    [
        new Repeated(),                                          // all null: nothing written
        new Repeated { Int32Array = [] },                        // empty is not null
        new Repeated { Int32Array = [1, 2, 3], Int32List = [4, 5] },
        new Repeated { Int32Array = [0], Int32List = [0] },      // zero elements still written
        new Repeated { DoubleArray = [1.5d, -2.25d], SingleArray = [0.5f] },
        new Repeated { BoolList = [true, false, true] },
        // note: protobuf-net rejects null *elements* in a collection, so no null here
        new Repeated { StringArray = ["a", ""], StringList = ["b"] },
        new Repeated { Messages = [new Inner { Value = 1, Label = "x" }, new Inner()] },
        new Repeated { MessageArray = [new Inner { Value = 2 }] },
        new Repeated { Int32List = [7], Scalar = 8 },
        new Repeated { Colours = [Colour.None, Colour.Blue, Colour.Red] },
        new Repeated { Smalls = [Small.Zero, Small.Big] },
        new Repeated { Colours = [], SingleColour = Colour.Red },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Repeated))]
public partial class ListsModel : TypeModel
{
}
