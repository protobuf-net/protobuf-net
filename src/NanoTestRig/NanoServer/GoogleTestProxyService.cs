using Benchmark.Nano.GoogleCodeGen;
using Google.Protobuf;
using Grpc.Core;
using System.Diagnostics;

namespace GrpcTestService
{
    internal class GoogleTestProxyService : TestProxy.TestProxyBase
    {
        private static int gen0 = 0;
        private static int gen1 = 0;
        private static int gen2 = 0;

        private const int ExtraResultSize = 32;
        private static ByteString extraResult = ByteString.CopyFrom(new string('b', ExtraResultSize), System.Text.Encoding.ASCII);
        public override Task<ForwardResponse> Forward(ForwardRequest request, ServerCallContext context)
        {
            var gen0After = GC.CollectionCount(0);
            var gen1After = GC.CollectionCount(1);
            var gen2After = GC.CollectionCount(2);

            /*
            if (gen0After != gen0 || gen1After != gen1 || gen2After != gen2)
            {
                Console.WriteLine(
                    $"CurrentTime={DateTime.Now.ToString("HH:mm:ss:fff")}, TraceId={request.TraceId}" +
                    $"Gen0/1/2 Before {gen0}/{gen1}/{gen2} After {gen0After}/{gen1After}/{gen2After}.");

                gen0 = gen0After;
                gen1 = gen1After;
                gen2 = gen2After;
            }
            */

            var startTicks = DateTime.UtcNow.Ticks;
            var e2eWatch = Stopwatch.StartNew();
            var response = new ForwardResponse();
            foreach (var itemRequest in request.ItemRequests)
            {
                var itemResponse = new ForwardPerItemResponse();
                itemResponse.Result = 100;
                itemResponse.ExtraResult = extraResult;
                response.ItemResponses.Add(itemResponse);
            }
            e2eWatch.Stop();
            response.RouteLatencyInUs = ElapsedInUs(e2eWatch);
            response.RouteStartTimeInTicks = startTicks;
            return Task.FromResult(response);
        }

        private static long ElapsedInUs(Stopwatch timer) => (long)(timer.Elapsed.TotalMilliseconds * 1000);
    }
}
