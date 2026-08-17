#nullable enable
// PBN4006: [ProtoGrpc] on a partial class that does not derive from ClientFactory.
//
// Deriving from it is the entire seam - it is what makes `channel.CreateGrpcService<IThing>(...)`
// accept the generated type, and its two abstract members are what we override. Without it there is
// nothing to hang the proxies off, so nothing is emitted.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.NotClientFactory;

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

[ProtoModel]
public partial class NotClientFactoryModel : TypeModel
{
    public static NotClientFactoryModel Instance { get; } = new NotClientFactoryModel();
}

[ProtoGrpc(Model = typeof(NotClientFactoryModel))]
[ProtoService(typeof(IThing))]
public sealed partial class NotClientFactoryServices
{
}
