#nullable enable
// The WCF markers. protobuf-net.Grpc honours [ServiceContract] as a service-contract marker alongside
// its own [Service], and [OperationContract(Name = ...)] alongside [Operation] - and until this fixture
// existed that entire path had *no* coverage: the two names appeared in the suite only inside comments.
//
// It is also the only place the named-argument route through GetServiceName and TryGetOperationName is
// reachable, because ProtoBuf.Grpc's own ServiceAttribute and OperationAttribute have get-only Name
// properties and can only be given a name positionally. So this fixture covers the other half of both
// lookups.
//
// What to check in the golden:
//
//   * the contract binds under the explicit Name, "wcf.Calculator", not the derived default;
//   * the operation named by [OperationContract(Name = "Sum")] binds as "Sum", while the unnamed one
//     falls back to the method name with the Async suffix stripped;
//   * [ServiceContract(Namespace = ...)] has no effect - protobuf-net.Grpc reads only Name, so a WCF
//     namespace is silently ignored rather than folded into the service name. Worth pinning precisely
//     because it looks like it ought to matter;
//   * a bare [ServiceContract] falls through to the same derived default a [Service] would get, so
//     IPlain binds as "GrpcFixtures.WcfContract.Plain".
//
// All four were checked against protobuf-net.Grpc 1.3.6 by calling ServiceBinder.Default's
// IsServiceContract and IsOperationContract, not by reading the source - the same way the three
// wire-name bugs in ServiceNaming.input.cs were found.
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.ServiceModel;
using System.Threading.Tasks;

namespace GrpcFixtures.WcfContract;

[ProtoContract]
public class Request
{
    [ProtoMember(1)]
    public int Left { get; set; }

    [ProtoMember(2)]
    public int Right { get; set; }
}

[ProtoContract]
public class Reply
{
    [ProtoMember(1)]
    public int Result { get; set; }
}

[ServiceContract(Name = "wcf.Calculator", Namespace = "http://example.com/ignored")]
public interface ICalculator
{
    [OperationContract(Name = "Sum")]
    Task<Reply> AddAsync(Request request, CallContext context = default);

    // no [OperationContract] name, so the method name less "Async"
    [OperationContract]
    Task<Reply> MultiplyAsync(Request request, CallContext context = default);
}

// a WCF marker with no name at all: the derived default applies, exactly as for [Service]
[ServiceContract]
public interface IPlain
{
    [OperationContract]
    Task<Reply> GoAsync(Request request, CallContext context = default);
}

public class PlainService : IPlain
{
    public Task<Reply> GoAsync(Request request, CallContext context = default) => null!;
}

public class CalculatorService : ICalculator
{
    public Task<Reply> AddAsync(Request request, CallContext context = default) => null!;
    public Task<Reply> MultiplyAsync(Request request, CallContext context = default) => null!;
}

[ProtoModel]
public partial class WcfContractModel : TypeModel
{
    public static WcfContractModel Instance { get; } = new WcfContractModel();
}

[ProtoGrpc(Model = typeof(WcfContractModel))]
[ProtoService(typeof(ICalculator), typeof(CalculatorService))]
[ProtoService(typeof(IPlain), typeof(PlainService))]
public sealed partial class WcfContractServices : ClientFactory
{
}
