#nullable enable
// protobuf-net.Grpc's byte-stream shape, which is much narrower than "Stream support" suggests: it is
// only Task<Stream> / ValueTask<Stream> as the RETURN, with an optional single request and optional
// context. There is no Stream request, and no Stream element type.
//
// What to check in the golden, because each is a rule taken from the runtime rather than a choice:
//
//   * the call is server-streaming, and the response type on the wire is BytesValue - never the
//     contract's own payload, and never the TypeModel's business. MarshallerCache pre-seeds a bespoke
//     marshaller for it, exactly as it does for Empty, so __cfg.GetMarshaller<BytesValue>() resolves it
//     with no reflection and no model;
//   * the client calls Reshape.ServerByteStreaming{Task,ValueTask}Async, not ServerStreamingAsync;
//   * the server calls Reshape.WriteStream, which is NOT generic and takes Task<Stream> - so the
//     ValueTask<Stream> operation is converted with .AsTask(), which is what ServerInvokerLookup does
//     through ToTaskT. writeTrailer is true, matching the runtime, which always sends the total-length
//     trailer even though the client does not demand it;
//   * a void request still gets Empty as its request type, as anywhere else.
//
// Deliberately absent: Task<FileStream>. The runtime matches `type == typeof(Task<Stream>)` exactly, so
// a derived stream type is NOT this shape - it stays a data payload, and stays refused as PBN4002.
// Diagnostics/ByteStreamDerived.input.cs pins that.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcFixtures.ByteStream;

[ProtoContract]
public class FileRequest
{
    [ProtoMember(1)]
    public string? Path { get; set; }
}

[Service]
public interface IFiles
{
    // request + context
    Task<Stream> ReadAsync(FileRequest request, CallContext context = default);

    // no request at all, so Empty goes out
    ValueTask<Stream> ReadDefaultAsync(CallContext context = default);

    // request only
    Task<Stream> ReadBareAsync(FileRequest request);

    // a CancellationToken context, which is the other supported spelling
    ValueTask<Stream> ReadCancellableAsync(FileRequest request, CancellationToken cancellationToken);
}

public class FilesService : IFiles
{
    public Task<Stream> ReadAsync(FileRequest request, CallContext context = default) => null!;

    public ValueTask<Stream> ReadDefaultAsync(CallContext context = default) => default;

    public Task<Stream> ReadBareAsync(FileRequest request) => null!;

    public ValueTask<Stream> ReadCancellableAsync(FileRequest request, CancellationToken cancellationToken) => default;
}

[ProtoModel]
public partial class ByteStreamModel : TypeModel
{
    public static ByteStreamModel Instance { get; } = new ByteStreamModel();
}

[ProtoGrpc(Model = typeof(ByteStreamModel))]
[ProtoService(typeof(IFiles), typeof(FilesService))]
public sealed partial class ByteStreamServices : ClientFactory
{
}
