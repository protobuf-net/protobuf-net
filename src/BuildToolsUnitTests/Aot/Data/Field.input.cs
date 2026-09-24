using ProtoBuf;
using ProtoBuf.Meta;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace AotFixtures.Field;

// fields are members in their own right, and ref-emit treats them exactly as it does properties -
// same write guards, same read shapes. The only differences are on our side: a field has no
// accessors to check, but it can be readonly or const, which a property cannot.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id;
}

[ProtoContract]
public class Fields
{
    [ProtoMember(1)] public int Number;
    [ProtoMember(2)] public string Text;
    [ProtoMember(3)] public Nested Message;
    [ProtoMember(4, DataFormat = DataFormat.ZigZag)] public int Zig;
    [ProtoMember(5), DefaultValue(7)] public int Defaulted = 7;
    [ProtoMember(6)] public int? Nullable;

    // a property alongside fields, to prove they interleave by field number as usual
    [ProtoMember(7)] public int Property { get; set; }
}

[ProtoContract]
public struct FieldStruct
{
    [ProtoMember(1)] public int Number;
}

// [DataMember] supplies orders for fields just as it does for properties
[DataContract]
public class DataFields
{
    [DataMember(Order = 1)] public int First;
    [DataMember(Order = 2)] public string Second;
}

public static class FieldSamples
{
    public static object[] Values =>
    [
        new Fields { Defaulted = 7 },
        new Fields { Number = 1, Text = "a", Defaulted = 7 },
        new Fields { Message = new Nested { Id = 2 }, Defaulted = 7 },
        new Fields { Zig = -3, Defaulted = 7 },
        new Fields { Defaulted = 4 },
        new Fields { Nullable = 0, Defaulted = 7 },
        new Fields { Property = 5, Defaulted = 7 },
        new FieldStruct(),
        new FieldStruct { Number = 6 },
        new DataFields(),
        new DataFields { First = 7, Second = "b" },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Fields))]
[ProtoSerializable(typeof(FieldStruct))]
[ProtoSerializable(typeof(DataFields))]
public partial class FieldModel : TypeModel
{
}
