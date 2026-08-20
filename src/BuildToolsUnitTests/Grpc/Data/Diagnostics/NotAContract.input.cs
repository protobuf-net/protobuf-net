#nullable enable
// PBN4007, twice, from its two distinct causes - both worth pinning, because the message has to
// cover both:
//
//   * a [ProtoService] naming something that is not an interface at all;
//   * an interface that is one, but carries neither [Service] nor [ServiceContract].
//
// Neither would be bound by protobuf-net.Grpc either, so both are matches rather than shortfalls.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.NotAContract;

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

// a class, not an interface
public class NotAnInterface
{
    public Task<Reply> GetAsync(Request request, CallContext context = default) => null!;
}

// an interface, but unmarked
public interface IUnmarked
{
    Task<Reply> GetAsync(Request request, CallContext context = default);
}

[ProtoModel]
public partial class NotAContractModel : TypeModel
{
    public static NotAContractModel Instance { get; } = new NotAContractModel();
}

[ProtoGrpc(Model = typeof(NotAContractModel))]
[ProtoService(typeof(NotAnInterface))]
[ProtoService(typeof(IUnmarked))]
public sealed partial class NotAContractServices : ClientFactory
{
}
