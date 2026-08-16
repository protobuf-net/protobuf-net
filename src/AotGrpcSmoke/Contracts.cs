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
}

public class GreeterService : IGreeter
{
    public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
        => Task.FromResult(new HelloReply { Message = "hello, " + request.Name, Count = request.Count });

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
