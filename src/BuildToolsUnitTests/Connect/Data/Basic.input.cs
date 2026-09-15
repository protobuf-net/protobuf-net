#nullable enable
// The everyday shape: one contract with every method kind, a named [ProtoModel], and an
// implementation - so the client proxy, the server bindings, the registration and the fluent
// extensions are all emitted.
using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectFixtures.Basic;

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

[Service("connectfixtures.v1.Greeter")]
public interface IGreeter
{
    // unary, CallContext
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

    // unary, CancellationToken - converted at the call site
    Task<HelloReply> PingAsync(HelloRequest request, CancellationToken cancellationToken = default);

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
    public Task<HelloReply> PingAsync(HelloRequest request, CancellationToken cancellationToken = default) => null!;
    public IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default) => null!;
    public Task<HelloReply> CollectAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default) => null!;
    public IAsyncEnumerable<HelloReply> Chat(IAsyncEnumerable<HelloRequest> requests, CallContext context = default) => null!;
}

// Stands in for the [ProtoModel]-generated model. Only ProtoConnectGenerator runs in these golden
// tests, so there is no generated model to point at and the harness supplies one by hand. Do NOT read
// this as evidence that payload types are discovered automatically.
public partial class BasicModel : TypeModel
{
    public static BasicModel Instance { get; } = new BasicModel();
}

[ProtoConnect(Model = typeof(BasicModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
internal partial class BasicServices
{
}
