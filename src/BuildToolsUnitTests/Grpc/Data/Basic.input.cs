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

// Stands in for the [ProtoModel]-generated model. The gRPC generator only needs `Instance`, and only
// GrpcProxyGenerator runs in these golden tests, so there is no generated model to point at and the
// harness supplies one by hand. Do NOT read this as evidence that payload types are discovered
// automatically - the stand-in serializes nothing at all. Real seeding is ProtoModelGenerator's job and
// is covered by GrpcSeedingTests and src/AotGrpcSmoke.
//
// This one deliberately carries no [ProtoModel], so PBN4012 fires and the golden .txt beside this file
// records it. That is the check working: naming a model that has no compile-time serializers is exactly
// the mistake that otherwise surfaces as a bare CS0117 on the generated Instance, or as a green build
// whose payloads are all marshalled reflectively. Every other fixture marks its stand-in, so that its
// own goldens stay about what it is testing.
public partial class BasicModel : TypeModel
{
    public static BasicModel Instance { get; } = new BasicModel();
}

[ProtoGrpc(Model = typeof(BasicModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public sealed partial class BasicServices : ClientFactory { }
