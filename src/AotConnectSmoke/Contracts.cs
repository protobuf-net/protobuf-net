using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Connect.AspNetCore;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;

namespace ProtoBuf.AotConnectSmoke;

// The service, defined *exactly* the way protobuf-net.Grpc defines one: [Service] on the interface,
// and protobuf-net.Grpc's own CallContext as the context parameter. Not a lookalike - the same types.
//
// That is the decision recorded in notes/connect/findings.md §17, and the reason for it is the sell:
// an existing protobuf-net.Grpc contract is served over Connect with no edit at all. Nothing here is
// Connect-specific, and this same interface can be hosted over gRPC simultaneously.

// the wire name is pinned rather than derived: [Service] with no name gives
// "{namespace}.{name-without-I}", which is fine within .NET but is not a name another language's
// schema would have chosen. Pinning it is what you do for cross-language interop.
[Service("aotconnectsmoke.v1.Greeter")]
public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

    /// <summary>Reports a failure the deliberate way, with a code the caller can act on.</summary>
    Task<HelloReply> RefuseAsync(HelloRequest request, CallContext context = default);

    /// <summary>Fails the undeliberate way, to prove an unhandled exception is not leaked.</summary>
    Task<HelloReply> ExplodeAsync(HelloRequest request, CallContext context = default);

    /// <summary>Takes longer than any caller will wait, to exercise <c>connect-timeout-ms</c>.</summary>
    Task<HelloReply> DawdleAsync(HelloRequest request, CallContext context = default);

    /// <summary>Server-streaming: <c>Repeat</c> messages, then a clean terminator.</summary>
    IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default);

    /// <summary>
    /// Fails <em>after</em> the stream has started, which HTTP cannot express: the status was committed
    /// to 200 with the first message, so the failure has to travel in the terminating message.
    /// </summary>
    IAsyncEnumerable<HelloReply> SubscribeThenFail(HelloRequest request, CallContext context = default);

    /// <summary>Client-streaming: many requests, one reply.</summary>
    Task<HelloReply> CollectAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);

    /// <summary>Bidirectional: echoes each request as it arrives. HTTP/2 only.</summary>
    IAsyncEnumerable<HelloReply> Chat(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);
}

// Deliberately NOT HelloRequest/HelloReply. Every other method here happens to share one pair, which
// is an artefact of the fixture rather than anything real - and an artefact that quietly made a
// hand-written helper look emittable. A generator has to name each method's own types.
[ProtoContract]
public class WaveRequest
{
    [ProtoMember(1)]
    public int Times { get; set; }
}

[ProtoContract]
public class WaveReply
{
    [ProtoMember(1)]
    public int Index { get; set; }
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

/// <summary>A second, deliberately unrelated contract, to see what multi-service costs.</summary>
[Service("aotconnectsmoke.v1.Farewell")]
public interface IFarewell
{
    Task<HelloReply> GoodbyeAsync(HelloRequest request, CallContext context = default);

    IAsyncEnumerable<WaveReply> WaveAsync(WaveRequest request, CallContext context = default);
}

public sealed class FarewellService : IFarewell
{
    public Task<HelloReply> GoodbyeAsync(HelloRequest request, CallContext context = default)
    {
        var message = $"goodbye {request.Name}";
        return Task.FromResult(new HelloReply { Message = message, Length = message.Length });
    }

    public async IAsyncEnumerable<WaveReply> WaveAsync(WaveRequest request, CallContext context = default)
    {
        for (var i = 1; i <= Math.Max(1, request.Times); i++)
        {
            yield return new WaveReply { Index = i };
        }
        await Task.CompletedTask;
    }
}

public sealed class GreeterService : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
    {
        // ordinary protobuf-net.Grpc: trailing metadata goes on the ServerCallContext. For a unary
        // Connect call it leaves as a `trailer-` prefixed response header, which is how the protocol
        // avoids HTTP trailers and therefore avoids requiring HTTP/2 - but the contract cannot tell.
        context.ServerCallContext?.ResponseTrailers.Add("greeter-version", "1");

        var name = string.IsNullOrWhiteSpace(request.Name) ? "world" : request.Name;
        var message = string.Concat(Enumerable.Repeat($"hello {name}; ", Math.Max(1, request.Repeat))).TrimEnd(' ', ';');
        return Task.FromResult(new HelloReply { Message = message, Length = message.Length });
    }

    public Task<HelloReply> RefuseAsync(HelloRequest request, CallContext context = default)
        => throw new ConnectException(ConnectCode.PermissionDenied, $"'{request.Name}' may not greet.");

    public Task<HelloReply> ExplodeAsync(HelloRequest request, CallContext context = default)
        => throw new InvalidOperationException("a secret that must not reach the caller");

    public async IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default)
    {
        var count = Math.Max(1, request.Repeat);
        for (var i = 1; i <= count; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var message = $"hello {request.Name} #{i}";
            yield return new HelloReply { Message = message, Length = message.Length };
        }

        // added after the last message and before the terminator - which is where it will travel, since
        // response headers were committed long ago
        context.ServerCallContext?.ResponseTrailers.Add("greeter-count", count.ToString());
        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<HelloReply> SubscribeThenFail(HelloRequest request, CallContext context = default)
    {
        yield return new HelloReply { Message = "first", Length = 5 };
        yield return new HelloReply { Message = "second", Length = 6 };
        await Task.CompletedTask;
        throw new ConnectException(ConnectCode.ResourceExhausted, "the well ran dry");
    }

    public async Task<HelloReply> CollectAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default)
    {
        var names = new List<string>();
        await foreach (var request in requests.WithCancellation(context.CancellationToken))
        {
            names.Add(request.Name ?? "?");
        }

        // Length carries what the server saw for Content-Length: -1 proves the request went out
        // chunked, which is the point of the whole shape. A client-streaming body cannot state a
        // length, because the messages do not exist when the headers are sent.
        var declared = (context.ServerCallContext as ConnectServerCallContext)?.HttpContext.Request.ContentLength;
        return new HelloReply { Message = string.Join("+", names), Length = (int)(declared ?? -1) };
    }

    public async IAsyncEnumerable<HelloReply> Chat(IAsyncEnumerable<HelloRequest> requests, CallContext context = default)
    {
        // echo each message as it arrives rather than draining first: a server that buffered the whole
        // request stream would work over HTTP/1.1 too, and would prove nothing about interleaving
        await foreach (var request in requests.WithCancellation(context.CancellationToken))
        {
            var message = $"echo {request.Name}";
            yield return new HelloReply { Message = message, Length = message.Length };
        }
    }

    public async Task<HelloReply> DawdleAsync(HelloRequest request, CallContext context = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), context.CancellationToken);
        return new HelloReply { Message = "eventually" };
    }
}

[ProtoModel]
[ProtoSerializable(typeof(HelloRequest))]
[ProtoSerializable(typeof(HelloReply))]
[ProtoSerializable(typeof(WaveRequest))]
[ProtoSerializable(typeof(WaveReply))]
public partial class SmokeModel : TypeModel
{
}
