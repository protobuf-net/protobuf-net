#nullable enable
// PBN4003: an *open* generic service contract, named as `typeof(IBox<>)`.
//
// Only the open form is refused. A closed construction is an ordinary contract and is emitted like
// any other - see ClosedGeneric.input.cs - so the line is open-versus-closed, the same one the
// serializer generator draws for [ProtoSerializable].
//
// The unbound symbol needs its own test, and this fixture is what pins it: `typeof(IBox<>)` arrives
// with type *arguments* that are not type parameters, so a recursive contains-a-type-parameter check
// alone returns false and the contract would fall through to be refused for some unrelated-sounding
// reason. Exactly the trap ProtoModelGenerator hit with IsUnboundGenericType.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.GenericInterface;

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
public interface IBox<T> where T : class
{
    Task<Reply> GetAsync(T request, CallContext context = default);
}

[ProtoModel]
public partial class GenericInterfaceModel : TypeModel
{
    public static GenericInterfaceModel Instance { get; } = new GenericInterfaceModel();
}

[ProtoGrpc(Model = typeof(GenericInterfaceModel))]
[ProtoService(typeof(IBox<>))]
public sealed partial class GenericInterfaceServices : ClientFactory
{
}
