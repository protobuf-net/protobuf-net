using ProtoBuf;
using ProtoBuf.Connect;
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

[Service]
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

    public async Task<HelloReply> DawdleAsync(HelloRequest request, CallContext context = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), context.CancellationToken);
        return new HelloReply { Message = "eventually" };
    }
}

[ProtoModel]
[ProtoSerializable(typeof(HelloRequest))]
[ProtoSerializable(typeof(HelloReply))]
public partial class SmokeModel : TypeModel
{
}
