using ProtoBuf;
using ProtoBuf.Meta;

namespace ProtoBuf.ConnectProbe;

// Hand-written contracts for connectrpc.eliza.v1, the demo service running on demo.connectrpc.com.
// Written by hand rather than generated from the .proto deliberately: the point of this probe is that
// protobuf-net's *code-first* bytes are interoperable with a reference Connect implementation, so
// starting from a .proto would test the wrong thing.
//
//   message SayRequest  { string sentence = 1; }
//   message SayResponse { string sentence = 1; }

[ProtoContract]
public class SayRequest
{
    [ProtoMember(1)]
    public string? Sentence { get; set; }
}

[ProtoContract]
public class SayResponse
{
    [ProtoMember(1)]
    public string? Sentence { get; set; }
}

/// <summary>
/// Field 1 as a varint where the service's schema says length-delimited string.
/// </summary>
/// <remarks>
/// This does <em>not</em> produce a server error, which is the point of keeping it: in protobuf a
/// wire-type mismatch is not malformed data, it is an <em>unknown field</em> - the parser skips it and
/// leaves the declared field at its default. So the call succeeds and the service sees an empty
/// sentence. Found by expecting the opposite; worth pinning, because "the schemas disagree" failing
/// silently is the whole reason field numbers have to be managed rather than assumed.
/// </remarks>
[ProtoContract]
public class WireTypeMismatchRequest
{
    [ProtoMember(1)]
    public long Sentence { get; set; }
}

// server-streaming: rpc Introduce(IntroduceRequest) returns (stream IntroduceResponse)
[ProtoContract]
public class IntroduceRequest
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[ProtoContract]
public class IntroduceResponse
{
    [ProtoMember(1)]
    public string? Sentence { get; set; }
}

[ProtoModel]
[ProtoSerializable(typeof(SayRequest))]
[ProtoSerializable(typeof(SayResponse))]
[ProtoSerializable(typeof(WireTypeMismatchRequest))]
[ProtoSerializable(typeof(IntroduceRequest))]
[ProtoSerializable(typeof(IntroduceResponse))]
public partial class ElizaModel : TypeModel
{
}
