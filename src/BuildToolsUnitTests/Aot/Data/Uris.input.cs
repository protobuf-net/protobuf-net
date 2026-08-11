using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.Uris;

// System.Uri has inbuilt behaviour in protobuf-net - ProtoTypeCode.Uri resolves to StringSerializer
// wrapped in a UriDecorator - so it is a scalar, not a surrogate case. On the wire it is a plain
// string: OriginalString out, `new Uri(s, UriKind.RelativeOrAbsolute)` back, with empty meaning null.
[ProtoContract]
public class Links
{
    [ProtoMember(1)] public Uri Home { get; set; }
    [ProtoMember(2)] public string Name { get; set; }

    // relative and absolute both round-trip, which is why UriKind.RelativeOrAbsolute is used
    [ProtoMember(3)] public Uri Relative { get; set; }

    [ProtoMember(4)] public List<Uri> All { get; set; }
    [ProtoMember(5)] public Uri[] More { get; set; }

    [ProtoMember(6)] public Dictionary<int, Uri> ById { get; set; }

    // a getter-only one, to prove it goes through the same backing-field path as any other scalar
    [ProtoMember(7)] public Uri Fixed { get; }
}

public static class UrisSamples
{
    public static object[] Values =>
    [
        new Links(),
        new Links { Home = new Uri("https://example.org/a?b=c") },
        new Links { Home = new Uri("urn:x"), Name = "n" },
        new Links { Relative = new Uri("../sibling", UriKind.Relative) },
        new Links { All = [new Uri("https://a/"), new Uri("https://b/")] },
        new Links { More = [new Uri("https://c/")] },
        new Links { ById = new() { [1] = new Uri("https://d/") } },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Links))]
public partial class UrisModel : TypeModel
{
}
