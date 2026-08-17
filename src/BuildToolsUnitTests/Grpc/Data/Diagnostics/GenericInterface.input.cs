#nullable enable
// PBN4003: a generic service contract.
//
// Both spellings land here - a closed construction as below, and the unbound `typeof(IBox<>)`, which
// arrives as a generic symbol just the same. The closed one is the interesting case, because it is
// the one a consumer might reasonably expect to work: `IBox<Request>` names a single concrete
// contract, and the runtime binds it happily.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.GenericInterface;

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

[Service]
public interface IBox<T> where T : class
{
    Task<Reply> GetAsync(T request, CallContext context = default);
}

public partial class GenericInterfaceModel : TypeModel
{
    public static GenericInterfaceModel Instance { get; } = new GenericInterfaceModel();
}

[ProtoGrpc(Model = typeof(GenericInterfaceModel))]
[ProtoService(typeof(IBox<Request>))]
public sealed partial class GenericInterfaceServices : ClientFactory
{
}
