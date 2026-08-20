#nullable enable
// Two operations with the same *method* name, given distinct names on the wire by [Operation]. A legal
// contract, and one that used to compile perfectly and then fail at server startup:
//
//   System.Reflection.AmbiguousMatchException
//
// ...because the server binding looked its metadata up with typeof(IThing).GetMethod("SendAsync"), and
// the by-name overload of GetMethod throws as soon as there is more than one match. The emit now passes
// the parameter types, so each binding names exactly the method it is binding.
//
// Note what the fixture has to include to be a fair test. Without [Operation] both overloads would
// reduce to the same gRPC operation name and the contract would be invalid for reasons that have
// nothing to do with reflection - so the disambiguation on the wire is part of the shape, not decoration.
//
// The nullable parameter is here on purpose too: the typeof list cannot reuse the signature rendering,
// because `typeof(HelloRequest?)` is CS8639 for a reference type while the annotation is part of the
// type for Nullable<T>. Check the golden emits typeof(...HelloRequest) and typeof(int?).
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.Overloads;

[ProtoContract]
public class HelloRequest
{
    [ProtoMember(1)]
    public string? Name { get; set; }
}

[ProtoContract]
public class HelloReply
{
    [ProtoMember(1)]
    public string? Message { get; set; }
}

[Service]
public interface IThing
{
    [Operation("SendOne")]
    Task<HelloReply> SendAsync(HelloRequest request, CallContext context = default);

    // same method name, different parameter type, distinct name on the wire
    [Operation("SendMany")]
    Task<HelloReply> SendAsync(HelloReply request, CallContext context = default);

    // a nullable reference parameter, which must not be rendered as typeof(HelloRequest?)
    Task<HelloReply> MaybeAsync(HelloRequest? request, CallContext context = default);

    // ...and a nullable value type, where the annotation IS part of the type
    Task<HelloReply> CountAsync(int? request, CallContext context = default);
}

public class ThingService : IThing
{
    public Task<HelloReply> SendAsync(HelloRequest request, CallContext context = default) => null!;
    public Task<HelloReply> SendAsync(HelloReply request, CallContext context = default) => null!;
    public Task<HelloReply> MaybeAsync(HelloRequest? request, CallContext context = default) => null!;
    public Task<HelloReply> CountAsync(int? request, CallContext context = default) => null!;
}

[ProtoModel]
public partial class OverloadsModel : TypeModel
{
    public static OverloadsModel Instance { get; } = new OverloadsModel();
}

[ProtoGrpc(Model = typeof(OverloadsModel))]
[ProtoService(typeof(IThing), typeof(ThingService))]
public sealed partial class OverloadsServices : ClientFactory
{
}
