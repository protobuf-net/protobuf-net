#nullable enable
// PBN4011: a [ProtoService] naming a type that does not resolve, which arrives as an error symbol.
//
// The usual real cause is naming a contract produced by *another* source generator in the same
// project: generators all run against the same input compilation and never see each other's output,
// so the seed is an error symbol with a name and nothing else. This fixture reproduces that state the
// blunt way, by naming a type that simply is not there.
//
// So this fixture does not compile, deliberately - the consumer's CS0246 is part of the picture, and
// the .txt golden beside this pins both diagnostics together.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;

namespace GrpcFixtures.Unresolved;

[ProtoContract]
public class Request
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

public partial class UnresolvedModel : TypeModel
{
    public static UnresolvedModel Instance { get; } = new UnresolvedModel();
}

[ProtoGrpc(Model = typeof(UnresolvedModel))]
[ProtoService(typeof(IGeneratedElsewhere))]
public sealed partial class UnresolvedServices : ClientFactory
{
}
