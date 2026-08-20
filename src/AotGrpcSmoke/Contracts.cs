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

/// <summary>
/// Endpoint metadata, at each of the three levels the runtime collects from.
/// </summary>
/// <remarks>
/// These exist to <em>measure</em> something that was previously unmeasured: the generated server
/// binding reaches metadata through <c>__cfg.Binder.GetMetadata</c>, which is reflective, and until
/// now nothing here carried an attribute - so it returned an empty list twelve times and proved
/// nothing about whether ILC keeps attribute metadata alive. A missing <c>[Authorize]</c> is a more
/// permissive endpoint with no error anywhere, so "probably fine" is not good enough.
/// </remarks>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class SmokeTagAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[Service]
public interface IGreeter
{
    Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default);

    /// <summary>
    /// Bound, and deliberately never called: its <c>[Authorize]</c> is real, and ASP.NET Core enforces
    /// it on any request that arrives (the endpoint middleware demands <c>UseAuthorization()</c>) - so
    /// invoking it would need an auth stack that has nothing to do with what is being measured. Binding
    /// is enough, because binding is when metadata is collected.
    /// </summary>
    /// <summary>
    /// The byte-stream shape: a server-stream of <c>BytesValue</c> under the covers, with a bespoke
    /// marshaller that <c>MarshallerCache</c> pre-seeds rather than one the model supplies.
    /// </summary>
    Task<System.IO.Stream> DownloadAsync(HelloRequest request, CallContext context = default);

    [SmokeTag("contract-method")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    Task<HelloReply> SecureAsync(HelloRequest request, CallContext context = default);

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

    /// <summary>Client-streaming: an IAsyncEnumerable in, a single value out.</summary>
    /// <remarks>
    /// Here because the golden fixtures <em>compile</em> all five method shapes but only run three -
    /// and "not covered" is not the same as "fine", it is unmeasured. Client-streaming and duplex each
    /// reach a distinct Reshape helper and a distinct AddXxxMethod on the server, so neither was
    /// exercised under ILC until now.
    /// </remarks>
    Task<HelloReply> SumAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);

    /// <summary>Duplex: streams both ways.</summary>
    IAsyncEnumerable<HelloReply> EchoAsync(IAsyncEnumerable<HelloRequest> requests, CallContext context = default);
}

[SmokeTag("service-type")]
public class GreeterService : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
        => Task.FromResult(new HelloReply { Message = "hello, " + request.Name, Count = request.Count });

    public Task<System.IO.Stream> DownloadAsync(HelloRequest request, CallContext context = default)
        => Task.FromResult<System.IO.Stream>(new System.IO.MemoryStream(
            System.Text.Encoding.UTF8.GetBytes("stream:" + request.Name)));

    [SmokeTag("service-method")]
    public Task<HelloReply> SecureAsync(HelloRequest request, CallContext context = default)
        => Task.FromResult(new HelloReply { Message = "secret", Count = 0 });

    public async Task<HelloReply> SumAsync(IAsyncEnumerable<HelloRequest> requests,
        CallContext context = default)
    {
        var total = 0;
        var names = new List<string>();
        await foreach (var request in requests)
        {
            total += request.Count;
            if (request.Name is not null) names.Add(request.Name);
        }
        return new HelloReply { Message = string.Join("+", names), Count = total };
    }

    public async IAsyncEnumerable<HelloReply> EchoAsync(IAsyncEnumerable<HelloRequest> requests,
        CallContext context = default)
    {
        await foreach (var request in requests)
        {
            yield return new HelloReply { Message = request.Name, Count = request.Count * 2 };
        }
    }

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
