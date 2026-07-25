// Every operation shape the generator emits, on one contract set.
#nullable enable
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcFixtures.Contracts
{
    public class Request { }

    public class Response { }

    [Service]
    public interface IUnaryService
    {
        // the four response wrappers, with and without a payload
        Task<Response> TaskAsync(Request request, CallContext context = default);

        ValueTask<Response> ValueTaskAsync(Request request, CallContext context = default);

        Response Sync(Request request, CallContext context = default);

        void SyncVoid(Request request, CallContext context = default);

        Task TaskVoidAsync(Request request, CallContext context = default);

        ValueTask ValueTaskVoidAsync(Request request, CallContext context = default);

        // no request payload at all
        Task<Response> NoRequestAsync(CallContext context = default);

        // no context
        Task<Response> NoContextAsync(Request request);

        // CancellationToken instead of CallContext
        Task<Response> CancellableAsync(Request request, CancellationToken cancellationToken);

        // explicitly named operation, and a name that must not be Async-trimmed
        [Operation("Renamed")]
        Task<Response> OriginalNameAsync(Request request, CallContext context = default);

        Task<Response> Async(Request request, CallContext context = default);
    }

    [Service("named.service.v1")]
    public interface IStreamingService
    {
        IAsyncEnumerable<Response> ServerStreaming(Request request, CallContext context = default);

        Task<Response> ClientStreamingAsync(IAsyncEnumerable<Request> requests, CallContext context = default);

        ValueTask<Response> ClientStreamingValueTaskAsync(IAsyncEnumerable<Request> requests, CallContext context = default);

        IAsyncEnumerable<Response> Duplex(IAsyncEnumerable<Request> requests, CallContext context = default);
    }

    // a [SubService] base is bound as part of the inheriting contract, under *its* service name; the
    // same payload type on both sides shares one marshaller. IDisposable is implemented but not bound,
    // matching what the runtime proxy does with it.
    [SubService]
    public interface IBaseService
    {
        Task<Request> EchoAsync(Request request, CallContext context = default);
    }

    [Service]
    public interface IDerivedService : IBaseService, System.IDisposable
    {
        Task<Response> DerivedAsync(Request request, CallContext context = default);
    }
}
