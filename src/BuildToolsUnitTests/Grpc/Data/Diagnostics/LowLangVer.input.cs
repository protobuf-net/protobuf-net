#nullable enable
// PBN4000: pinned below the generator's C# 9 floor by the sidecar LowLangVer.langver.
//
// Everything here is deliberately C# 8-compatible - a block namespace rather than a file-scoped one,
// and no target-typed `new` - since the fixture is parsed at the pinned version like any real
// consumer's source would be.
//
// Worth reading the .txt golden beside this rather than assuming: declining to emit for a type that
// derives ClientFactory leaves its two abstract members unimplemented, so the consumer does not get
// a soft fall back to the runtime proxy - they get CS0534 and a build that fails.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.LowLangVer
{
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

    public partial class LowLangVerModel : TypeModel
    {
        public static LowLangVerModel Instance { get; } = new LowLangVerModel();
    }

    [ProtoGrpc(Model = typeof(LowLangVerModel))]
    [ProtoService(typeof(IThing))]
    public sealed partial class LowLangVerServices : ClientFactory
    {
    }
}
