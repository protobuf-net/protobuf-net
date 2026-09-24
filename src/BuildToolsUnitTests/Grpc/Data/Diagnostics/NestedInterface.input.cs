#nullable enable
// PBN4001: a service contract nested inside another type.
//
// This is about the shape of the *generated* type rather than any one operation: the proxy and the
// server bindings are emitted as top-level types named after the contract, so a nested contract has
// nowhere to go. It takes the whole interface out rather than any single member.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.NestedInterface;

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

public static class Outer
{
    [Service]
    public interface IThing
    {
        Task<Reply> GetAsync(Request request, CallContext context = default);
    }
}

[ProtoModel]
public partial class NestedInterfaceModel : TypeModel
{
    public static NestedInterfaceModel Instance { get; } = new NestedInterfaceModel();
}

[ProtoGrpc(Model = typeof(NestedInterfaceModel))]
[ProtoService(typeof(Outer.IThing))]
public sealed partial class NestedInterfaceServices : ClientFactory
{
}
