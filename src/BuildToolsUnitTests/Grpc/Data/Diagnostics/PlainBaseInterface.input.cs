// A base interface that is neither [SubService] nor a contract of its own: the runtime implements its
// members as throwing stubs and binds nothing, so the whole contract goes to the runtime (PBN3005)
// rather than this generator inventing a binding the runtime wouldn't produce.
#nullable enable
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Threading.Tasks;

namespace GrpcFixtures.PlainBaseInterface
{
    public class Request { }

    public class Response { }

    public interface IPlainBase
    {
        Task<Request> EchoAsync(Request request, CallContext context = default);
    }

    [Service]
    public interface IDerivedService : IPlainBase
    {
        Task<Response> DerivedAsync(Request request, CallContext context = default);
    }
}
