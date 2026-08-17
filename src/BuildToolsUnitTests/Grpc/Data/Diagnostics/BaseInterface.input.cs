#nullable enable
// PBN4004: a contract inheriting an interface that declares operations but is not [SubService].
//
// The runtime's rule is what makes this a refusal rather than something to reproduce: only a
// [SubService] base is bound under this contract's service name. For any other base carrying
// operations, the runtime emits a *throwing stub* on the client and binds nothing on the server -
// so the members exist and fail when called. Reproducing that at build time would be work spent
// making a broken contract look supported.
//
// IDisposable and IAsyncDisposable are the two exceptions, and Basic.input.cs is where those live.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.BaseInterface;

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

// no [SubService], and it declares an operation - which is what takes the derived contract out
public interface IAudited
{
    Task<Reply> WhoAmIAsync(Request request, CallContext context = default);
}

[Service]
public interface IThing : IAudited
{
    Task<Reply> GetAsync(Request request, CallContext context = default);
}

public partial class BaseInterfaceModel : TypeModel
{
    public static BaseInterfaceModel Instance { get; } = new BaseInterfaceModel();
}

[ProtoGrpc(Model = typeof(BaseInterfaceModel))]
[ProtoService(typeof(IThing))]
public sealed partial class BaseInterfaceServices : ClientFactory
{
}
