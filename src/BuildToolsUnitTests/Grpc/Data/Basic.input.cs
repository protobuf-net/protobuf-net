#nullable enable
// The everyday shape: one contract, a named [ProtoModel], and an implementation - so both the client
// proxy and the server bindings are emitted. Note there is no [ModuleInitializer] and no registry in
// the output: the proxy is reached through BasicServices.CreateClient<T>, and the server bindings
// through the generated AddBasicServices().
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcFixtures.Basic;

[ProtoContract]
public class HelloRequest
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[ProtoContract]
public class HelloReply
{
    [ProtoMember(1)]
    public string? Message { get; set; }
}

[Service]
public interface IGreeter
{
    // unary, CallContext
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

    // unary, CancellationToken - the context is converted at the call site
    ValueTask<HelloReply> PingAsync(HelloRequest request, CancellationToken cancellationToken = default);

    // server-streaming
    IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default);

    // client-streaming
    Task<HelloReply> CollectAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);

    // duplex
    IAsyncEnumerable<HelloReply> Chat(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);
}

public class GreeterService : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default) => null!;
    public ValueTask<HelloReply> PingAsync(HelloRequest request, CancellationToken cancellationToken = default) => default;
    public IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default) => null!;
    public Task<HelloReply> CollectAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default) => null!;
    public IAsyncEnumerable<HelloReply> Chat(IAsyncEnumerable<HelloRequest> requests, CallContext context = default) => null!;
}

// stands in for the [ProtoModel]-generated model; the gRPC generator only needs `Instance`, and the
// two generators never see each other's output, so the golden tests supply it by hand
public partial class BasicModel : TypeModel
{
    public static BasicModel Instance { get; } = new BasicModel();
}

[ProtoGrpc(Model = typeof(BasicModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public sealed partial class BasicServices : ClientFactory { }
