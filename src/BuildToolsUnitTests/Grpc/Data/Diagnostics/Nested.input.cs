// A nested service contract: reported (PBN3001) and left to the runtime path.
#nullable enable
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Threading.Tasks;

namespace GrpcFixtures.Nested
{
    public class Request { }

    public class Response { }

    public static class Containing
    {
        [Service]
        public interface INestedService
        {
            Task<Response> UnaryAsync(Request request, CallContext context = default);
        }
    }
}
