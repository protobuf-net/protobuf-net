#nullable enable
// [NoSideEffects] is the code-first spelling of `option idempotency_level = NO_SIDE_EFFECTS;`, and the
// gate on Connect GET: the server binds GET as well as POST for such a method, and the client's
// `useGet` has something to act on. Before it existed the code-first path could not reach Connect GET
// at all - the descriptor was always emitted with `idempotent` at its false default.
//
// The streaming case is here as a fixture rather than only as a diagnostic test: Connect GET is
// unary-only, and what matters is that the attribute is BOTH reported (PBN5009) and ignored in the
// emitted descriptor. Reporting without ignoring would bind a GET that cannot work.
using ProtoBuf;
using ProtoBuf.Connect;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConnectFixtures.Idempotent;

[ProtoContract]
public class LookupRequest
{
    [ProtoMember(1)]
    public string? Key { get; set; }
}

[ProtoContract]
public class LookupReply
{
    [ProtoMember(1)]
    public string? Value { get; set; }
}

[Service("connectfixtures.v1.Lookup")]
public interface ILookup
{
    // cacheable: GET as well as POST
    [NoSideEffects]
    Task<LookupReply> GetAsync(LookupRequest request, CallContext context = default);

    // the ordinary case, for contrast - without it "idempotent: true" everywhere would look the same
    Task<LookupReply> PutAsync(LookupRequest request, CallContext context = default);

    // PBN5009: reported, and the descriptor stays POST-only
    [NoSideEffects]
    IAsyncEnumerable<LookupReply> WatchAsync(LookupRequest request, CallContext context = default);
}

public class LookupService : ILookup
{
    public Task<LookupReply> GetAsync(LookupRequest request, CallContext context = default) => null!;
    public Task<LookupReply> PutAsync(LookupRequest request, CallContext context = default) => null!;
    public IAsyncEnumerable<LookupReply> WatchAsync(LookupRequest request, CallContext context = default) => null!;
}

// Stands in for the [ProtoModel]-generated model; see Basic.input.cs.
public partial class IdempotentModel : TypeModel
{
    public static IdempotentModel Instance { get; } = new IdempotentModel();
}

[ProtoConnect(Model = typeof(IdempotentModel))]
[ProtoService(typeof(ILookup), typeof(LookupService))]
internal partial class IdempotentServices
{
}
