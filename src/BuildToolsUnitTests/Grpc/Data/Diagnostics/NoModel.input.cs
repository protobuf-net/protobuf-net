#nullable enable
// PBN4010: proxies are generated, but no Model is named - the one that matters most, and the reason
// the generator has this shape at all.
//
// Note this fixture *does* emit: the proxies are perfectly good AOT-safe code, and the marshallers
// fall back to RuntimeTypeModel.Default, which reflects. So the build succeeds, the JIT run succeeds,
// and the native publish is where it falls over. Read the .output.cs beside this to see the shape
// the warning is describing.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Threading.Tasks;

namespace GrpcFixtures.NoModel;

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
}

[ProtoGrpc]
[ProtoService(typeof(IThing))]
public sealed partial class NoModelServices : ClientFactory
{
}
