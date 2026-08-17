#nullable enable
// PBN4008: the implementation named alongside the contract does not implement it.
//
// This one exists purely to give a better error than the consumer would otherwise get: the generated
// server bindings are typed on the implementation, so without this check the failure is a CS0311 in
// generated code the consumer never wrote. Note the client proxy is lost along with the bindings -
// the contract is dropped whole.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.BadImplementation;

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
public interface IThing
{
    Task<Reply> GetAsync(Request request, CallContext context = default);
}

// has the right shape, but does not declare that it implements IThing
public class ThingService
{
    public Task<Reply> GetAsync(Request request, CallContext context = default) => null!;
}

[ProtoModel]
public partial class BadImplementationModel : TypeModel
{
    public static BadImplementationModel Instance { get; } = new BadImplementationModel();
}

[ProtoGrpc(Model = typeof(BadImplementationModel))]
[ProtoService(typeof(IThing), typeof(ThingService))]
public sealed partial class BadImplementationServices : ClientFactory
{
}
