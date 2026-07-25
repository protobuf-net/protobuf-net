using ProtoBuf;
using ProtoBuf.Meta;
using System.Collections.Generic;

namespace AotFixtures.MapKey;

// a dictionary whose key is not expressible as a protobuf map key - bool, char, floating-point, or
// a message - is still serialized as a map, but with OptionFailOnDuplicateKey, which makes reading
// a repeated key throw rather than overwrite.
//
// The samples deliberately use *disjoint* keys per field: the differential suite manufactures
// repeated fields by concatenating payloads, and that option is exactly what rejects a collision.
[ProtoContract]
public class Payload
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class Keys
{
    [ProtoMember(1)] public Dictionary<bool, int> Bool { get; set; }
    [ProtoMember(2)] public Dictionary<double, int> Double { get; set; }
    [ProtoMember(3)] public Dictionary<char, int> Char { get; set; }

    // a message key is passed `this` as the *first* trailing serializer
    [ProtoMember(4)] public Dictionary<Payload, int> Message { get; set; }
    [ProtoMember(5)] public Dictionary<Payload, Payload> BothMessages { get; set; }
}

public static class MapKeySamples
{
    public static object[] Values =>
    [
        new Keys { Bool = new() { [true] = 1 } },
        new Keys { Bool = new() { [false] = 2 } },
        new Keys { Double = new() { [1.5] = 3 } },
        new Keys { Double = new() { [2.5] = 4 } },
        new Keys { Char = new() { ['a'] = 5 } },
        new Keys { Char = new() { ['b'] = 6 } },
        new Keys { Message = new() { [new Payload { Id = 7 }] = 8 } },
        new Keys { Message = new() { [new Payload { Id = 9 }] = 10 } },
        new Keys { BothMessages = new() { [new Payload { Id = 11 }] = new Payload { Id = 12 } } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Keys))]
public partial class MapKeyModel : TypeModel
{
}
