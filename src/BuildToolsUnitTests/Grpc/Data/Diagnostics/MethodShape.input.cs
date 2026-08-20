#nullable enable
// PBN4002: an operation in a shape the generator does not emit.
//
// The contract below is mostly fine - GetAsync is an ordinary unary call - and that is the point:
// one unsupported operation takes the *whole* contract out, so the two good members go to the runtime
// path with it. The alternative, emitting a proxy with some members missing, would turn a contract
// that works today into a startup failure.
//
// Both refused shapes here are deliberate rather than incidental. A Stream payload is a runtime-path
// shape (protobuf-net.Grpc reshapes it), and a generic method has no fixed request or response type
// to build a Method<,> from.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.IO;
using System.Threading.Tasks;

namespace GrpcFixtures.MethodShape;

[ProtoContract]
public class Request
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[ProtoContract]
public class Reply
{
    [ProtoMember(1)]
    public string? Message { get; set; }
}

[Service]
public interface IThing
{
    Task<Reply> GetAsync(Request request, CallContext context = default);

    Task<Stream> DownloadAsync(Request request, CallContext context = default);

    Task<T> EchoAsync<T>(T request, CallContext context = default) where T : class;
}

[ProtoModel]
public partial class MethodShapeModel : TypeModel
{
    public static MethodShapeModel Instance { get; } = new MethodShapeModel();
}

[ProtoGrpc(Model = typeof(MethodShapeModel))]
[ProtoService(typeof(IThing))]
public sealed partial class MethodShapeServices : ClientFactory
{
}
