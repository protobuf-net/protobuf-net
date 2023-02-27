using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GrpcTestClient
{
    internal static class Program
    {
        private const int ThreadNum = 1;
        private const int BatchSize = 3500;
        private const int RequestContextSize = 32 * 1024;
        private const int ItemContextSize = 64;

        enum TestMode
        {
            GoogleCodeGen,
            HandWrittenNoPool,
        }
        private static void Main(string[] args)
        {
            var mode = TestMode.GoogleCodeGen;
            foreach (var arg in args)
            {
                if (Enum.TryParse<TestMode>(arg, true, out var tmp))
                {
                    mode = tmp;
                }
            }
            Console.WriteLine($"Test mode: {mode}");
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < ThreadNum; i++)
            {
                switch (mode)
                {
                    case TestMode.GoogleCodeGen:
                        threads.Add(new Thread(() => RunCoreGoogle()));
                        break;
                    case TestMode.HandWrittenNoPool:
                        threads.Add(new Thread(() => RunCoreHandWrittenNoPool()));
                        break;
                    default:
                        throw new NotSupportedException($"Mode not supported: {mode}");
                }
            }

            foreach (var th in threads)
            {
                th.Start();
            }

            foreach (var th in threads)
            {
                th.Join();
            }
        }

        private static GrpcChannel CreateChannel()
        {
            return GrpcChannel.ForAddress(
                $"http://localhost:81",
                new GrpcChannelOptions
                {
                    Credentials = ChannelCredentials.Insecure,
                    MaxReceiveMessageSize = 1 * 1024 * 1024,
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(1000),
                        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                        PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
                        InitialHttp2StreamWindowSize = 1 * 1024 * 1024,
                    },
                });
        }

        private static long RunCoreGoogle()
        {
            using (var channel = CreateChannel())
            {
                var client = new Benchmark.Nano.GoogleCodeGen.TestProxy.TestProxyClient(channel);
                var request = new Benchmark.Nano.GoogleCodeGen.ForwardRequest { TraceId = Guid.NewGuid().ToString("N") };
                for (int i = 0; i < BatchSize; ++i)
                {
                    var itemRequest = new Benchmark.Nano.GoogleCodeGen.ForwardPerItemRequest { ItemId = i.ToString() };
                    var bytes = new string('b', ItemContextSize);
                    itemRequest.ItemContext = ByteString.CopyFrom(bytes, System.Text.Encoding.ASCII);
                    request.ItemRequests.Add(itemRequest);
                }

                request.RequestContextInfo = ByteString.CopyFrom(new string('a', RequestContextSize) + "end", System.Text.Encoding.ASCII);

                // warm up like establishing connection
                while (true)
                {
                    request.TraceId = Guid.NewGuid().ToString("N");
                    var ret = SendRequest(client, request, true);
                    if (ret > 0)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }

                Console.WriteLine("Ready to send simulated requests");

                var latenciesInUs = new long[10000];
                Array.Clear(latenciesInUs);
                int startIndex = 0;
                int totalCount = 0;
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    totalCount++;
                    request.TraceId = Guid.NewGuid().ToString("N");
                    var latencyInUs = SendRequest(client, request, false);
                    if (latencyInUs > 0)
                    {
                        latenciesInUs[startIndex++] = latencyInUs;
                    }

                    if (startIndex == latenciesInUs.Length)
                    {
                        Array.Sort(latenciesInUs);
                        DateTime endTime = DateTime.Now;
                        var qps = totalCount * 1.0 / (endTime - startTime).TotalSeconds;
                        var successRate = latenciesInUs.Length * 1.0 / totalCount;

                        Console.WriteLine($"successful rate {successRate} = {latenciesInUs.Length}/{totalCount}");
                        Console.WriteLine($"qps {qps} = {totalCount}/({endTime.ToString("yyyy-MM-dd HH:mm:ss")} - {startTime.ToString("yyyy-MM-dd HH:mm:ss")})");
                        Console.WriteLine($"min   latency in Us: {latenciesInUs.First()}");
                        Console.WriteLine($"max   latency in Us: {latenciesInUs.Last()}");
                        Console.WriteLine($"50%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.5)]}");
                        Console.WriteLine($"90%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.9)]}");
                        Console.WriteLine($"95%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.95)]}");
                        Console.WriteLine($"99%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.99)]}");
                        Console.WriteLine($"99.9% latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.999)]}");

                        startIndex = 0;
                        totalCount = 0;
                        startTime = DateTime.Now;
                        Array.Clear(latenciesInUs);
                    }
                }
            }
        }

        private static long RunCoreHandWrittenNoPool()
        {
            using (var channel = CreateChannel())
            {
                var client = new Benchmark.Nano.HandWrittenNoPool.TestProxy.TestProxyClient(channel);
                var itemRequests = new List<Benchmark.Nano.HandWrittenNoPool.ForwardPerItemRequest>(BatchSize);
                for (int i = 0; i < BatchSize; ++i)
                {
                    var bytes = Encoding.ASCII.GetBytes(new string('b', ItemContextSize));
                    var itemRequest = new Benchmark.Nano.HandWrittenNoPool.ForwardPerItemRequest(Encoding.ASCII.GetBytes(i.ToString()), bytes);
                    itemRequests.Add(itemRequest);
                }
                var contextInfo = Encoding.ASCII.GetBytes(new string('a', RequestContextSize) + "end");
                var request = new Benchmark.Nano.HandWrittenNoPool.ForwardRequest(Guid.NewGuid().ToString("N"), itemRequests, contextInfo);

                // warm up like establishing connection
                while (true)
                {
                    request.TraceId = Guid.NewGuid().ToString("N");
                    var ret = SendRequest(client, request, true);
                    if (ret > 0)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }

                Console.WriteLine("Ready to send simulated requests");

                var latenciesInUs = new long[10000];
                Array.Clear(latenciesInUs);
                int startIndex = 0;
                int totalCount = 0;
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    totalCount++;
                    request.TraceId = Guid.NewGuid().ToString("N");
                    var latencyInUs = SendRequest(client, request, false);
                    if (latencyInUs > 0)
                    {
                        latenciesInUs[startIndex++] = latencyInUs;
                    }

                    if (startIndex == latenciesInUs.Length)
                    {
                        Array.Sort(latenciesInUs);
                        DateTime endTime = DateTime.Now;
                        var qps = totalCount * 1.0 / (endTime - startTime).TotalSeconds;
                        var successRate = latenciesInUs.Length * 1.0 / totalCount;

                        Console.WriteLine($"successful rate {successRate} = {latenciesInUs.Length}/{totalCount}");
                        Console.WriteLine($"qps {qps} = {totalCount}/({endTime.ToString("yyyy-MM-dd HH:mm:ss")} - {startTime.ToString("yyyy-MM-dd HH:mm:ss")})");
                        Console.WriteLine($"min   latency in Us: {latenciesInUs.First()}");
                        Console.WriteLine($"max   latency in Us: {latenciesInUs.Last()}");
                        Console.WriteLine($"50%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.5)]}");
                        Console.WriteLine($"90%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.9)]}");
                        Console.WriteLine($"95%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.95)]}");
                        Console.WriteLine($"99%   latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.99)]}");
                        Console.WriteLine($"99.9% latency in Us: {latenciesInUs[(int)(latenciesInUs.Length * 0.999)]}");

                        startIndex = 0;
                        totalCount = 0;
                        startTime = DateTime.Now;
                        Array.Clear(latenciesInUs);
                    }
                }
            }
        }

        public static long ElapsedInUs(this Stopwatch watch) => (long)(watch.Elapsed.TotalMilliseconds * 1000);
        private static long SendRequest(Benchmark.Nano.GoogleCodeGen.TestProxy.TestProxyClient client, Benchmark.Nano.GoogleCodeGen.ForwardRequest request, bool logResponse)
        {
            long latencyInUs = -1;
            try
            {
                var gen0Before = GC.CollectionCount(0);
                var gen1Before = GC.CollectionCount(1);
                var gen2Before = GC.CollectionCount(2);
                var watch = Stopwatch.StartNew();
                watch.Start();
                var response = client.ForwardAsync(request).ResponseAsync.Result;
                watch.Stop();

                if (logResponse)
                {
                    Console.WriteLine($"Response is {JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true })}");
                }

                var gapLatencyInUs = watch.ElapsedInUs() - response.RouteLatencyInUs;

                /*
                if (gapLatencyInUs > 5000)
                {
                    var gen0After = GC.CollectionCount(0);
                    var gen1After = GC.CollectionCount(1);
                    var gen2After = GC.CollectionCount(2);
#pragma warning disable SA1118 // Parameter should not span multiple lines
                    Console.WriteLine(
                        $"It takes {gapLatencyInUs}us to send {request.ItemRequests.Count} items on network by trace id {request.TraceId}. " +
                        $"ClientSendingTime={watch.StartTime.ToString("HH:mm:ss:fff")}, RouteStartTimeOnServer={new DateTime(response.RouteStartTimeInTicks).ToString("HH:mm:ss:fff")}, " +
                        $"RouteLatencyOnServerInUs={response.RouteLatencyInUs}, RouteClientLatencyInUs={watch.ElapsedInUs}. " +
                        $"Gen0/1/2 Before {gen0Before}/{gen1Before}/{gen2Before} After {gen0After}/{gen1After}/{gen2After}.");
#pragma warning restore SA1118 // Parameter should not span multiple lines
                }
                */

                latencyInUs = watch.ElapsedInUs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed to send a request. Exception : {ex}");
            }

            return latencyInUs;
        }

        private static long SendRequest(Benchmark.Nano.HandWrittenNoPool.TestProxy.TestProxyClient client, Benchmark.Nano.HandWrittenNoPool.ForwardRequest request, bool logResponse)
        {
            long latencyInUs = -1;
            try
            {
                var gen0Before = GC.CollectionCount(0);
                var gen1Before = GC.CollectionCount(1);
                var gen2Before = GC.CollectionCount(2);
                var watch = Stopwatch.StartNew();
                watch.Start();
                var response = client.ForwardAsync(request).ResponseAsync.Result;
                watch.Stop();

                if (logResponse)
                {
                    Console.WriteLine($"Response is {JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true })}");
                }

                var gapLatencyInUs = watch.ElapsedInUs() - response.RouteLatencyInUs;

                /*
                if (gapLatencyInUs > 5000)
                {
                    var gen0After = GC.CollectionCount(0);
                    var gen1After = GC.CollectionCount(1);
                    var gen2After = GC.CollectionCount(2);
#pragma warning disable SA1118 // Parameter should not span multiple lines
                    Console.WriteLine(
                        $"It takes {gapLatencyInUs}us to send {request.ItemRequests.Count} items on network by trace id {request.TraceId}. " +
                        $"ClientSendingTime={watch.StartTime.ToString("HH:mm:ss:fff")}, RouteStartTimeOnServer={new DateTime(response.RouteStartTimeInTicks).ToString("HH:mm:ss:fff")}, " +
                        $"RouteLatencyOnServerInUs={response.RouteLatencyInUs}, RouteClientLatencyInUs={watch.ElapsedInUs}. " +
                        $"Gen0/1/2 Before {gen0Before}/{gen1Before}/{gen2Before} After {gen0After}/{gen1After}/{gen2After}.");
#pragma warning restore SA1118 // Parameter should not span multiple lines
                }
                */

                latencyInUs = watch.ElapsedInUs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed to send a request. Exception : {ex}");
            }

            return latencyInUs;
        }
    }
}