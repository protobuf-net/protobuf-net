using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace AotFixtures.WrappedUnsupported;

// protobuf-net enforces the null-wrapping rules by *throwing* rather than by ignoring the attribute,
// deliberately - so that widening them later is not a silent behaviour change. We refuse the same
// shapes at build time. Which ones those are was probed against ref-emit, not read off the docs:
// a message and a compatibility-level BCL type are both "not scalar" for this purpose.
[ProtoContract]
public class Nested
{
    [ProtoMember(1)] public int Id { get; set; }
}

[ProtoContract]
public class WrappedMessage
{
    [ProtoMember(1), NullWrappedValue] public Nested Item { get; set; }
}

[ProtoContract]
public class WrappedNonNullable
{
    [ProtoMember(1), NullWrappedValue] public int Value { get; set; }
}

[ProtoContract]
public class WrappedBcl
{
    [ProtoMember(1), NullWrappedValue] public DateTime? When { get; set; }
}

[ProtoContract]
public class WrappedMap
{
    [ProtoMember(1), NullWrappedCollection] public Dictionary<int, int> Map { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(WrappedMessage))]
[ProtoSerializable(typeof(WrappedNonNullable))]
[ProtoSerializable(typeof(WrappedBcl))]
[ProtoSerializable(typeof(WrappedMap))]
public partial class WrappedUnsupportedModel : TypeModel
{
}
