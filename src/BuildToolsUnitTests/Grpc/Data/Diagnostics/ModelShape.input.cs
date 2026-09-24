#nullable enable
// PBN4014, from both causes: a [ProtoGrpc] declaration that is nested, and one that is generic.
//
// This is the mirror of PBN4001 for the declaration rather than the contract, and it was a build break
// rather than a missing feature. The generated half is emitted as `partial class X` directly inside the
// namespace, so a nested declaration produced *two* problems at once: a stray top-level type of that
// name, and the consumer's real nested class left without ClientFactory's two abstract members. Four
// compile errors, none of which named the generator.
//
// Both classes below still have to satisfy ClientFactory themselves for this fixture to compile at all,
// which is why they implement the two members by hand - the same reason NotPartial.input.cs does. A real
// consumer would not have written those, so they would see CS0534 as well as this warning.
//
// Supporting either shape means emitting the enclosing chain (and requiring every enclosing type to be
// partial); ProtoModelGenerator has the same case open as a TODO, so if it is ever built it should be
// built for both.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.ModelShape;

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

[ProtoModel]
public partial class ModelShapeModel : TypeModel
{
    public static ModelShapeModel Instance { get; } = new ModelShapeModel();
}

public static class Outer
{
    [ProtoGrpc(Model = typeof(ModelShapeModel))]
    [ProtoService(typeof(IThing))]
    public sealed partial class NestedServices : ClientFactory
    {
        protected override BinderConfiguration BinderConfiguration => null!;
        public override TService CreateClient<TService>(Grpc.Core.CallInvoker channel) => null!;
    }
}

[ProtoGrpc(Model = typeof(ModelShapeModel))]
[ProtoService(typeof(IThing))]
public sealed partial class GenericServices<T> : ClientFactory
    where T : class
{
    protected override BinderConfiguration BinderConfiguration => null!;
    public override TService CreateClient<TService>(Grpc.Core.CallInvoker channel) => null!;
}
