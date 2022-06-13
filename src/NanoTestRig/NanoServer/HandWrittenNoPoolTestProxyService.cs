using Benchmark.Nano.HandWrittenNoPool;
using Google.Protobuf;
using Grpc.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GrpcTestService
{
    internal class HandWrittenNoPoolTestProxyService : TestProxy.TestProxyBase
    {
        private static int gen0 = 0;
        private static int gen1 = 0;
        private static int gen2 = 0;

        private const int ExtraResultSize = 32;
        private static byte[] extraResult = Encoding.ASCII.GetBytes(new string('b', ExtraResultSize));
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

            var itemResponses = new List<ForwardPerItemResponse>(request.ItemRequests.Count);
            foreach (ref readonly var itemRequest in CollectionsMarshal.AsSpan(request.ItemRequests))
            {
                var itemResponse = new ForwardPerItemResponse(100, extraResult);
                itemResponses.Add(itemResponse);
            }
            e2eWatch.Stop();
            var response = new ForwardResponse(itemResponses, ElapsedInUs(e2eWatch), startTicks);

            //var httpContext = context.GetHttpContext();
            //httpContext.Response.RegisterForDispose(response);

            return Task.FromResult(response);
        }

        private static long ElapsedInUs(Stopwatch timer) => (long)(timer.Elapsed.TotalMilliseconds * 1000);
    }
}
