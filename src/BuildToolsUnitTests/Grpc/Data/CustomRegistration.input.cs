#nullable enable
// [ProtoGrpc(RegistrationMethodName = ...)] renames the generated IServiceCollection extension.
//
// Parsed, defaulted and emitted since the first commit, and never fixtured - so the default
// ("Add" + the declaration's name) was pinned by every other fixture while the override was pinned by
// none. Check the golden emits `MapCalculator` and not `AddCustomRegistrationServices`.
//
// The reason the option exists is worth knowing: the default reads oddly for a type that is not named
// "...Services", and the extension is the one generated member a consumer types by hand, so it is the
// one place a name they dislike is a real annoyance rather than an implementation detail.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.CustomRegistration;

[ProtoContract]
public class Request
{
    [ProtoMember(1)]
    public int Left { get; set; }
}

[ProtoContract]
public class Reply
{
    [ProtoMember(1)]
    public int Result { get; set; }
}

[Service]
public interface ICalculator
{
    Task<Reply> AddAsync(Request request, CallContext context = default);
}

public class CalculatorService : ICalculator
{
    public Task<Reply> AddAsync(Request request, CallContext context = default) => null!;
}

[ProtoModel]
public partial class CustomRegistrationModel : TypeModel
{
    public static CustomRegistrationModel Instance { get; } = new CustomRegistrationModel();
}

[ProtoGrpc(Model = typeof(CustomRegistrationModel), RegistrationMethodName = "MapCalculator")]
[ProtoService(typeof(ICalculator), typeof(CalculatorService))]
public sealed partial class CustomRegistrationServices : ClientFactory
{
}
