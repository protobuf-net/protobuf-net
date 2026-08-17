#nullable enable
// No diagnostic at all - and that is what this fixture is here to record.
//
// A [Service] interface with no *recognised operations* is dropped on the reasoning that it is most
// likely a marker interface rather than a contract. But nothing is reported, so the contract simply
// is not in the generated factory and the consumer is told nothing: CreateClient<IThing> throws at
// run time with no build-time hint that it was ever going to.
//
// Compare the serializer generator, where every dropped contract has to say why (PBN3001-PBN3004,
// with PBN3004 existing purely so a cascade is not silent). This is the same hole, and the .txt
// golden beside this being empty is the evidence.
//
// The interface below has a property and an event - neither is an operation - so it parses cleanly
// and yields nothing.
using ProtoBuf;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System;

namespace GrpcFixtures.MarkerInterface;

[ProtoContract]
public class Request
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[Service]
public interface IThing
{
    string? Name { get; set; }

    event EventHandler? Changed;
}

public partial class MarkerInterfaceModel : TypeModel
{
    public static MarkerInterfaceModel Instance { get; } = new MarkerInterfaceModel();
}

[ProtoGrpc(Model = typeof(MarkerInterfaceModel))]
[ProtoService(typeof(IThing))]
public sealed partial class MarkerInterfaceServices : ClientFactory
{
}
