// PBN4000: pinned below the generator's C# 9 floor by the sidecar LowLangVer.langver.
//
// Pinned at 8.0 - below the C# 9 floor, which is what matters here. 7.3 would be the more pointed
// choice, being the *default* for net4x and netstandard2.0 and so the version these consumers
// actually have; it cannot be pinned in this harness, because Roslyn rejects a compilation whose
// trees disagree on language version and _ContractSurface.cs is nullable-annotated. The source below
// is nonetheless written 7.3-clean, since the emitted code uses nothing above C# 6.
//
// The generated file is parsed at the same pinned version, so this fixture does prove the down-level
// shape compiles below the floor rather than merely looking like it should.
//
// What it emits is the reduced shape: the two ClientFactory members, with the client half delegating
// to ClientFactory.Default, which is the reflective runtime factory - so the warning's promise that
// "the runtime proxy will be used" is literally true. Emitting nothing (what this used to do) left
// ClientFactory's two abstract members unimplemented and the consumer with CS0534 twice, which is a
// failed build rather than a fallback. No registration method is emitted, deliberately: there is no
// server-side equivalent of ClientFactory.Default to delegate to, and an AddLowLangVerServices() that
// bound nothing would give a server that starts and serves nothing.
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
        public string Name { get; set; }
    }

    [ProtoContract]
    public class Reply
    {
        [ProtoMember(1)]
        public string Message { get; set; }
    }

    [Service]
    public interface IThing
    {
        Task<Reply> GetAsync(Request request, CallContext context = default);
    }

    [ProtoModel]
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
