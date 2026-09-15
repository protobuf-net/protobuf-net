using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;

namespace ProtoBuf.ConnectLoad;

// One contract, one implementation, both transports - the same shape AotDualHostSmoke proves works,
// reused here so the only variable between the two sides of the comparison is the protocol.
[Service("connectload.v1.Greeter")]
public interface IGreeter
{
    Task<Reply> UnaryAsync(Request request, CallContext context = default);

    IAsyncEnumerable<Reply> StreamAsync(Request request, CallContext context = default);
}

[ProtoContract]
public class Request
{
    [ProtoMember(1)] public string? Name { get; set; }
    [ProtoMember(2)] public int Count { get; set; }
    [ProtoMember(3)] public byte[]? Blob { get; set; }
}

[ProtoContract]
public class Reply
{
    [ProtoMember(1)] public string? Message { get; set; }
    [ProtoMember(2)] public int Index { get; set; }
    [ProtoMember(3)] public byte[]? Blob { get; set; }
}

public class GreeterService : IGreeter
{
    public Task<Reply> UnaryAsync(Request request, CallContext context)
        => Task.FromResult(new Reply { Message = request.Name, Blob = request.Blob });

    public async IAsyncEnumerable<Reply> StreamAsync(Request request, CallContext context)
    {
        for (var i = 0; i < request.Count; i++)
        {
            yield return new Reply { Message = request.Name, Index = i, Blob = request.Blob };
        }
        await Task.CompletedTask;
    }
}

[ProtoModel]
public partial class LoadModel : TypeModel { }

[ProtoGrpc(Model = typeof(LoadModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
public sealed partial class GrpcSide : ClientFactory { }

[ProtoConnect(Model = typeof(LoadModel))]
[ProtoService(typeof(IGreeter), typeof(GreeterService))]
internal static partial class ConnectSide { }
