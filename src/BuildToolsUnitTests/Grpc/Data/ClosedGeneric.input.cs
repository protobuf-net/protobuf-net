#nullable enable
// Closed generic contracts, which are ordinary contracts: Roslyn hands us their members already
// substituted, so IBox<Request> and IBox<Reply> are two contracts that happen to share a definition.
// Only the *open* form is refused (PBN4003), because the emitted proxy is a non-generic type.
//
// Two of them, deliberately - one definition reached at two constructions is what proves the emitted
// proxy and provider names carry the type arguments rather than colliding on "IBox".
//
// The other thing to check in the golden is the service name. protobuf-net.Grpc builds it as
// `Namespace.Name_Arg1`, where Name has its leading "I" stripped and its arity suffix cut, and each
// argument contributes its [ProtoContract(Name)] if it has one and its metadata name otherwise. So
// Renamed below is expected to bind as ...IBox_the_payload, not ...IBox_Renamed: that name is on the
// wire, and a generated client that computed it differently from a reflection-bound server would
// simply not find the service.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.ClosedGeneric;

[ProtoContract]
public class Request
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[ProtoContract]
public class Reply
{
    [ProtoMember(1)]
    public string? Message { get; set; }
}

[ProtoContract(Name = "the_payload")]
public class Renamed
{
    [ProtoMember(1)]
    public int Value { get; set; }
}

[Service]
public interface IBox<T> where T : class
{
    Task<Reply> GetAsync(T request, CallContext context = default);
}

public class RequestBox : IBox<Request>
{
    public Task<Reply> GetAsync(Request request, CallContext context = default) => null!;
}

[ProtoModel]
public partial class ClosedGenericModel : TypeModel
{
    public static ClosedGenericModel Instance { get; } = new ClosedGenericModel();
}

[ProtoGrpc(Model = typeof(ClosedGenericModel))]
[ProtoService(typeof(IBox<Request>), typeof(RequestBox))]
[ProtoService(typeof(IBox<Renamed>))]
public sealed partial class ClosedGenericServices : ClientFactory
{
}
