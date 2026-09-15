#nullable enable
// PBN4002: an operation in a shape the generator does not emit.
//
// The contract below is mostly fine - GetAsync is an ordinary unary call - and that is the point:
// one unsupported operation takes the *whole* contract out, so the two good members go to the runtime
// path with it. The alternative, emitting a proxy with some members missing, would turn a contract
// that works today into a startup failure.
//
// The refusals here are deliberate rather than incidental. A generic method has no fixed request or
// response type to build a Method<,> from, and IObservable<T> is a runtime-path shape that no
// build-time generator can express.
//
// Note Task<Stream> is NOT refused and produces no diagnostic: byte streaming was implemented, and it
// is kept here as the contrast - a shape that once belonged on this list and no longer does.
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

    // pins the "not a shape the generator can express" wording, which no fixture covered before
    System.IObservable<Reply> Watch(Request request, CallContext context = default);
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
