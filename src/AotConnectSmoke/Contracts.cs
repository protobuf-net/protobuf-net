using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;
using ProtoBuf.Meta;

namespace ProtoBuf.AotConnectSmoke;

// The service, defined the way protobuf-net.Grpc defines one: a plain interface of async methods,
// each taking a request message and a context.
//
// NOTE the absence of a [Service] attribute and of protobuf-net.Grpc's CallContext. That is not an
// oversight - where the contract-facing vocabulary should come from is an open decision recorded in
// notes/connect/findings.md §9, and it does not have to be settled yet: nothing reads an attribute
// until the *generator* exists, and the bindings here are hand-written. Settling it by accident, in a
// fixture, is exactly how a decision like that gets made badly.

public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request, ConnectServerCallContext? context = null);

    /// <summary>Reports a failure the deliberate way, with a code the caller can act on.</summary>
    Task<HelloReply> RefuseAsync(HelloRequest request, ConnectServerCallContext? context = null);

    /// <summary>Fails the undeliberate way, to prove an unhandled exception is not leaked.</summary>
    Task<HelloReply> ExplodeAsync(HelloRequest request, ConnectServerCallContext? context = null);

    /// <summary>Takes longer than any caller will wait, to exercise <c>connect-timeout-ms</c>.</summary>
    Task<HelloReply> DawdleAsync(HelloRequest request, ConnectServerCallContext? context = null);
}

[ProtoContract]
public class HelloRequest
{
    [ProtoMember(1)]
    public string? Name { get; set; }

    [ProtoMember(2)]
    public int Repeat { get; set; }
}

[ProtoContract]
public class HelloReply
{
    [ProtoMember(1)]
    public string? Message { get; set; }

    [ProtoMember(2)]
    public int Length { get; set; }
}

public sealed class GreeterService : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, ConnectServerCallContext? context = null)
    {
        // trailing metadata: for a unary call this is a `trailer-` prefixed response header, which is
        // how the protocol avoids HTTP trailers and therefore avoids requiring HTTP/2
        context?.AddTrailer("greeter-version", "1");

        var name = string.IsNullOrWhiteSpace(request.Name) ? "world" : request.Name;
        var message = string.Concat(Enumerable.Repeat($"hello {name}; ", Math.Max(1, request.Repeat))).TrimEnd(' ', ';');
        return Task.FromResult(new HelloReply { Message = message, Length = message.Length });
    }

    public Task<HelloReply> RefuseAsync(HelloRequest request, ConnectServerCallContext? context = null)
        => throw new ConnectException(ConnectCode.PermissionDenied, $"'{request.Name}' may not greet.");

    public Task<HelloReply> ExplodeAsync(HelloRequest request, ConnectServerCallContext? context = null)
        => throw new InvalidOperationException("a secret that must not reach the caller");

    public async Task<HelloReply> DawdleAsync(HelloRequest request, ConnectServerCallContext? context = null)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), context?.CancellationToken ?? default);
        return new HelloReply { Message = "eventually" };
    }
}

[ProtoModel]
[ProtoSerializable(typeof(HelloRequest))]
[ProtoSerializable(typeof(HelloReply))]
public partial class SmokeModel : TypeModel
{
}
