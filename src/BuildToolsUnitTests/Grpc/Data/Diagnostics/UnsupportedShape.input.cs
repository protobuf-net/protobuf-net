// One unsupported operation takes the whole contract out (PBN3002): a proxy missing an interface
// member wouldn't compile, and the runtime path handles more shapes than this generator emits.
#nullable enable
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Threading.Tasks;

namespace GrpcFixtures.UnsupportedShape
{
    public class Request { }

    public class Response { }

    [Service]
    public interface IMixedService
    {
        Task<Response> SupportedAsync(Request request, CallContext context = default);

        // two data parameters: the supported patterns are (), (context), (request) and
        // (request, context), so this one is left to the runtime
        Task<Response> TwoPayloadsAsync(Request first, Request second);
    }
}
