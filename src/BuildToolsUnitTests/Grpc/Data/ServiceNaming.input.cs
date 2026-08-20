#nullable enable
// The service name is the wire contract, so it has to agree with ServiceBinder character for
// character: a generated client that computes it differently from a reflection-bound server does not
// find the service, and the failure is an unimplemented-method error at call time with nothing
// pointing back here.
//
// Every name in the golden beside this was checked against the real protobuf-net.Grpc 1.3.6 by
// calling ServiceBinder.Default.IsServiceContract, not by reading the source. Three of the four are
// cases the generator previously got wrong:
//
//   Item                      -> GrpcFixtures.ServiceNaming.tem       (was: ...Item)
//   [Service("tmpl.{0}.svc")] -> tmpl.the_payload.svc                 (was: tmpl.{0}.svc)
//   global namespace          -> .GlobalNaming                        (was: GlobalNaming)
//
// The first and third look like slips in the runtime and are reproduced deliberately: the "I" is
// stripped by a bare StartsWith("I") with no test that what follows is upper-case, and the namespace
// is concatenated unconditionally where Type.Namespace is null in the global namespace. Being
// bug-compatible is the whole requirement here - "more correct" would just mean "does not interop".
//
// The second is not a slip: an explicit Name on a *generic* contract is a format string, filled with
// the same per-argument names the default form uses (so [ProtoContract(Name)] wins over the type's
// own name, which is why Renamed appears as the_payload in both).
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Threading.Tasks;

namespace GrpcFixtures.ServiceNaming
{
    [ProtoContract]
    public class Reply
    {
        [ProtoMember(1)]
        public string? Message { get; set; }
    }

    [ProtoContract(Name = "the_payload")]
    public class Renamed
    {
        [ProtoMember(1)]
        public int Value { get; set; }
    }

    // no leading-I convention, so the bare StartsWith("I") eats the I
    [Service]
    public interface Item
    {
        Task<Reply> GetAsync(Renamed request, CallContext context = default);
    }

    // an explicit name on a non-generic contract is used as-is
    [Service("explicit.svc")]
    public interface IExplicit
    {
        Task<Reply> GetAsync(Renamed request, CallContext context = default);
    }

    // ...and on a generic one it is a format string
    [Service("tmpl.{0}.svc")]
    public interface ITemplated<T> where T : class
    {
        Task<Reply> GetAsync(T request, CallContext context = default);
    }

    [ProtoModel]
    public partial class ServiceNamingModel : TypeModel
    {
        public static ServiceNamingModel Instance { get; } = new ServiceNamingModel();
    }

    [ProtoGrpc(Model = typeof(ServiceNamingModel))]
    [ProtoService(typeof(Item))]
    [ProtoService(typeof(IExplicit))]
    [ProtoService(typeof(ITemplated<Renamed>))]
    [ProtoService(typeof(global::IGlobalNaming))]
    public sealed partial class ServiceNamingServices : ClientFactory
    {
    }
}

// in the global namespace, where Type.Namespace is null and the name picks up a leading dot
[Service]
public interface IGlobalNaming
{
    Task<GrpcFixtures.ServiceNaming.Reply> GetAsync(
        GrpcFixtures.ServiceNaming.Renamed request, CallContext context = default);
}
