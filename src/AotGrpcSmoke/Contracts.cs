using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Runtime.CompilerServices;

namespace AotGrpcSmoke;

[ProtoContract]
public class HelloRequest
{
    [ProtoMember(1)]
    public string? Name { get; set; }

    [ProtoMember(2)]
    public int Count { get; set; }
}

[ProtoContract]
public class HelloReply
{
    [ProtoMember(1)]
    public string? Message { get; set; }

    [ProtoMember(2)]
    public int Count { get; set; }
}

[Service]
public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

    IAsyncEnumerable<HelloReply> CountAsync(HelloRequest request, CallContext context = default);

    /// <summary>
    /// A void response, so this is the only member that puts <c>ProtoBuf.Grpc.Internal.Empty</c> on the
    /// wire - and it earns its place twice over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Empty</c> is the one payload that must <em>not</em> reach the serializer model: it has no
    /// contract, a private constructor, and its own hand-written marshaller that writes zero bytes. So
    /// this member is also the check that seeding excludes it - the model here carries no
    /// <c>[ProtoSerializable]</c> at all, so if the exclusion were dropped, a <c>PBN3002</c> would appear
    /// for a type nobody in this project ever named.
    /// </para>
    /// <para>
    /// And it is the only member whose marshaller comes from <c>MarshallerCache</c>'s pre-seeded entry
    /// rather than from the generated <c>SetMarshaller</c> block, which is a distinct path under ILC.
    /// </para>
    /// </remarks>
    Task NudgeAsync(HelloRequest request, CallContext context = default);
}

public class GreeterService : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
        => Task.FromResult(new HelloReply { Message = "hello, " + request.Name, Count = request.Count });

    public Task NudgeAsync(HelloRequest request, CallContext context = default)
    {
        Nudged = request.Name;
        return Task.CompletedTask;
    }

    /// <summary>Set by <see cref="NudgeAsync"/>, so a void call can be seen to have arrived.</summary>
    public static string? Nudged { get; private set; }

    public async IAsyncEnumerable<HelloReply> CountAsync(HelloRequest request,
        CallContext context = default)
    {
        for (var i = 0; i < request.Count; i++)
        {
            yield return new HelloReply { Message = request.Name, Count = i };
            await Task.Yield();
        }
    }
}
