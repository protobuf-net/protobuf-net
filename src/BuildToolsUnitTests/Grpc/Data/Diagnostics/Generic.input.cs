// A generic service contract: reported (PBN3003) and left to the runtime path.
#nullable enable
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Threading.Tasks;

namespace GrpcFixtures.Generic
{
    public class Request { }

    public class Response { }

    [Service]
    public interface IGenericService<T> where T : class
    {
        Task<Response> UnaryAsync(Request request, CallContext context = default);
    }
}
