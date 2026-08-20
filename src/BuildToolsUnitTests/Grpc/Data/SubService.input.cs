#nullable enable
// Inherited interfaces, all three cases the runtime distinguishes - and until this fixture existed
// only the refusal was covered (Diagnostics/BaseInterface.input.cs, PBN4004).
//
// What to check in the golden, because each is a rule rather than an accident:
//
//   * a [SubService] base IS bound, and bound under the *inheriting* contract's service name -
//     "GrpcFixtures.SubService.Thing", not anything derived from IAudited. That is what makes
//     [SubService] a composition mechanism rather than a second service;
//   * IDisposable and IAsyncDisposable are NOT bound, and their members get no-op implementations on
//     the client proxy. A gRPC contract cannot express them, and refusing the contract over a base
//     that a great deal of real code carries would be unhelpful;
//   * everything else carrying operations takes the contract out, which is PBN4004's fixture.
//
// This is also the fixture protobuf-net.Grpc#369 needs, and the golden shows exactly where it lands:
// the server binding for WhoAmIAsync passes `typeof(IAudited).GetMethod("WhoAmIAsync")` to
// __cfg.Binder.GetMetadata - the *declaring* interface, not IThing. #369 changes which type-level
// attributes GetMetadata then collects for such an operation; as proposed it swaps them, and the likely
// landing shape is a union of both.
//
// While we delegate to GetMetadata we inherit whatever it decides for free, so nothing here needs to
// change - but there was previously no contract in the suite with a [SubService] base at all, so
// nothing would have noticed if it did. If compile-time metadata is ever built, this is the contract to
// run the differential against, with an attribute on IAudited, on IThing, and on the implementation
// method.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System;
using System.Threading.Tasks;

namespace GrpcFixtures.SubService;

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

// bound as part of whatever inherits it, under *that* contract's service name
[SubService]
public interface IAudited
{
    Task<Reply> WhoAmIAsync(Request request, CallContext context = default);
}

[Service]
public interface IThing : IAudited, IDisposable, IAsyncDisposable
{
    Task<Reply> GetAsync(Request request, CallContext context = default);
}

public class ThingService : IThing
{
    public Task<Reply> GetAsync(Request request, CallContext context = default) => null!;
    public Task<Reply> WhoAmIAsync(Request request, CallContext context = default) => null!;
    public void Dispose() { }
    public ValueTask DisposeAsync() => default;
}

[ProtoModel]
public partial class SubServiceModel : TypeModel
{
    public static SubServiceModel Instance { get; } = new SubServiceModel();
}

[ProtoGrpc(Model = typeof(SubServiceModel))]
[ProtoService(typeof(IThing), typeof(ThingService))]
public sealed partial class SubServiceServices : ClientFactory
{
}
