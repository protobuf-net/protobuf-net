#nullable enable
// The interceptor half. Opted in by the sidecar Intercept.interceptors, which holds what
// <InterceptorsNamespaces> becomes by the time a generator sees it - the switch is per-project, so it
// cannot be expressed in the fixture source, exactly as with .langver.
//
// Three call sites, and the golden should show only *two* of them intercepted:
//
//   * the plain ChannelBase call - the everyday shape, since GrpcChannel derives from ChannelBase, so
//     the emitted body has to add CreateCallInvoker();
//   * the plain CallInvoker call - a second method, because the receiver differs;
//   * the call that already passes a factory, which must be LEFT ALONE. That consumer has done the thing
//     we would be doing for them, and taking it over would mean quietly ignoring their argument.
//
// Note the emitted `data` strings are checksums of *this file*, so any edit here changes them - that is
// inherent (the compiler rejects a stale checksum with CS9234) and the goldens are rewritten anyway.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.Intercept;

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
public interface IGreeter
{
    Task<Reply> SayHelloAsync(Request request, CallContext context = default);
}

[ProtoModel]
public partial class InterceptModel : TypeModel
{
    public static InterceptModel Instance { get; } = new InterceptModel();
}

[ProtoGrpc(Model = typeof(InterceptModel))]
[ProtoService(typeof(IGreeter))]
public sealed partial class InterceptServices : ClientFactory
{
}

public static class Consumer
{
    public static IGreeter ViaChannel(Grpc.Core.ChannelBase channel)
        => channel.CreateGrpcService<IGreeter>();

    public static IGreeter ViaInvoker(Grpc.Core.CallInvoker invoker)
        => invoker.CreateGrpcService<IGreeter>();

    // already explicit: not ours to take over
    public static IGreeter AlreadyExplicit(Grpc.Core.CallInvoker invoker)
        => invoker.CreateGrpcService<IGreeter>(InterceptServices.Instance);
}
