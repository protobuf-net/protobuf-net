using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;

namespace ProtoBuf.AotDualHostSmoke;

// ONE contract. Not a gRPC one and a Connect one that happen to look alike - the same interface, the
// same [Service] attribute, the same implementation instance shape, bound twice.
//
// This is the claim the whole branch rests on ("an existing protobuf-net.Grpc contract is served over
// Connect with no edit at all"), and until this project existed it was asserted in three places and
// demonstrated in none: every other check here runs one transport at a time.
[Service("dualhost.v1.Greeter")]
public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

    IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context = default);
}

[ProtoContract]
public class HelloRequest
{
    [ProtoMember(1)] public string? Name { get; set; }
    [ProtoMember(2)] public int Repeat { get; set; }
}

[ProtoContract]
public class HelloReply
{
    [ProtoMember(1)] public string? Message { get; set; }
    [ProtoMember(2)] public string? Transport { get; set; }
}

/// <summary>One implementation, resolved from one DI container, serving both transports.</summary>
public class GreeterService : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context)
        => Task.FromResult(new HelloReply
        {
            Message = $"hello {request.Name}",
            // the server reports what it was actually called over, so the checks cannot pass by
            // accidentally talking to the same endpoint twice
            Transport = Describe(context),
        });

    public async IAsyncEnumerable<HelloReply> Subscribe(HelloRequest request, CallContext context)
    {
        for (var i = 1; i <= request.Repeat; i++)
        {
            yield return new HelloReply { Message = $"hello {request.Name} #{i}", Transport = Describe(context) };
            await Task.Yield();
        }
    }

    /// <summary>
    /// The content-type as the handler sees it - which is <b>not</b> the same question on the two
    /// transports, and that is a finding rather than a nuisance.
    /// </summary>
    /// <remarks>
    /// grpc-dotnet filters protocol headers out of <c>ServerCallContext.RequestHeaders</c>: a gRPC
    /// call here surfaces <c>user-agent</c> and nothing else, so <c>content-type</c> is simply absent.
    /// The Connect implementation passes headers through, so it is present and says
    /// <c>application/proto</c> or <c>application/connect+proto</c>.
    /// <para>
    /// Both are defensible - a content-type is a transport detail rather than application metadata -
    /// but the sets are not equal, so a handler that reads a header must not assume parity across the
    /// two. That is exactly the kind of thing "one contract, both transports" could otherwise be taken
    /// to promise, which is why the smoke pins it.
    /// </para>
    /// </remarks>
    private static string Describe(CallContext context)
        => context.ServerCallContext?.RequestHeaders
            ?.FirstOrDefault(h => h.Key == "content-type")?.Value ?? NotSurfaced;

    internal const string NotSurfaced = "(not surfaced by this transport)";
}

[ProtoModel]
public partial class DualModel : TypeModel { }

/// <summary>The gRPC half: client proxies and the server's method provider.</summary>
[ProtoGrpc(Model = typeof(DualModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public sealed partial class GrpcSide : ClientFactory { }

/// <summary>
/// The Connect half - the same <c>[ProtoService]</c> declaration, verbatim.
/// </summary>
/// <remarks>
/// Two containers rather than one because the two generators each own their container attribute and
/// emit same-named entry points; nothing stops a consumer keeping them in one file, as here.
/// </remarks>
[ProtoConnect(Model = typeof(DualModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
internal static partial class ConnectSide { }
