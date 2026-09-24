#nullable enable
// Operations with no request, no response, or neither - the one place
// ProtoBuf.Grpc.Internal.Empty appears, and until this fixture existed it was entirely unmeasured.
//
// Empty is the stand-in protobuf-net.Grpc uses for a void side of a call. Three things about it are
// worth checking in the golden rather than assuming, because it is unlike every other payload:
//
//   * it must NOT appear in the SetMarshaller<T> block. It has no [ProtoContract], a private
//     constructor, and its own hand-written internal Marshaller<Empty> that writes zero bytes;
//     MarshallerCache pre-seeds that, so a TypeModel never sees it. Asking a [ProtoModel] to
//     serialize it would be asking for a contract it must refuse.
//   * ...but it DOES appear in the Method<,> type arguments and in the proxy bodies, because it is a
//     real type on the wire - a zero-length message.
//   * the client sends Empty.Instance and returns Empty.InstanceTask, rather than constructing one.
//
// It matters for seeding too, and for the same reason: the payload set handed to [ProtoModel] has to
// exclude it, or every void operation would drop a contract the consumer never wrote.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.VoidOperations;

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
    // void response
    Task PingAsync(Request request, CallContext context = default);

    // void request
    Task<Reply> StatusAsync(CallContext context = default);

    // both void
    Task NudgeAsync(CallContext context = default);

    // ValueTask, void response
    ValueTask ResetAsync(Request request, CallContext context = default);

    // synchronous, void response, and no context at all
    void Fire(Request request);
}

public class ThingService : IThing
{
    public Task PingAsync(Request request, CallContext context = default) => null!;
    public Task<Reply> StatusAsync(CallContext context = default) => null!;
    public Task NudgeAsync(CallContext context = default) => null!;
    public ValueTask ResetAsync(Request request, CallContext context = default) => default;
    public void Fire(Request request) { }
}

[ProtoModel]
public partial class VoidOperationsModel : TypeModel
{
    public static VoidOperationsModel Instance { get; } = new VoidOperationsModel();
}

[ProtoGrpc(Model = typeof(VoidOperationsModel))]
[ProtoService(typeof(IThing), typeof(ThingService))]
public sealed partial class VoidOperationsServices : ClientFactory
{
}
