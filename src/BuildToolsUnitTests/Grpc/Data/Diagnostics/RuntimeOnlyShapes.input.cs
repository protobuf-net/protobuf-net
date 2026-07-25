// Shapes the runtime path supports (ResultKind.Observable / Stream / Grpc) but this generator does
// not emit. Treating them as ordinary payloads would compile and then fail at bind time looking for
// a marshaller, so each one has to take its contract out (PBN3002).
#nullable enable
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GrpcFixtures.RuntimeOnlyShapes
{
    public class Request { }

    public class Response { }

    [Service]
    public interface IObservableService
    {
        IObservable<Response> Observed(Request request, CallContext context = default);
    }

    [Service]
    public interface IObservableRequestService
    {
        Task<Response> ObservedRequestAsync(IObservable<Request> requests, CallContext context = default);
    }

    [Service]
    public interface IStreamService
    {
        Task<MemoryStream> DownloadAsync(Request request, CallContext context = default);
    }

    [Service]
    public interface IStreamElementService
    {
        IAsyncEnumerable<Stream> ChunksAsync(Request request, CallContext context = default);
    }
}
