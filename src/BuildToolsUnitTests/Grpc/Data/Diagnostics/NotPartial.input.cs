#nullable enable
// PBN4005: [ProtoGrpc] on a type that is not partial, so there is nowhere to put our half.
//
// The two members stubbed at the bottom are what the generator would have supplied; without them
// this fixture would not compile at all. That is worth seeing rather than hiding: a real consumer
// who forgets `partial` gets CS0534 twice *as well as* PBN4005, and the CS error is the louder one.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.NotPartial;

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

public partial class NotPartialModel : TypeModel
{
    public static NotPartialModel Instance { get; } = new NotPartialModel();
}

[ProtoGrpc(Model = typeof(NotPartialModel))]
[ProtoService(typeof(IThing))]
public sealed class NotPartialServices : ClientFactory
{
    protected override BinderConfiguration BinderConfiguration => null!;
    public override TService CreateClient<TService>(Grpc.Core.CallInvoker channel) => null!;
}
