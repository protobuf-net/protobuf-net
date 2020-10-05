using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.NetCoreApp50), MemoryDiagnoser]
    public class PoolBenchmarks // investigating #668/#669
    {
        [Params(1, 5, 20)]
        public int Threads { get; set; }

        private const int TotalCount = 10000;

        [Benchmark(OperationsPerInvoke = TotalCount)]
        public Task Concurrent()
        {
            var arr = new Task[Threads];
            int perThreadCount = TotalCount / Threads;
            Action work = InnerLoop;
            for (int i = 0; i < Threads; i++)
            {
                arr[i] = Task.Run(work);
            }
            return Task.WhenAll(arr);

            void InnerLoop()
            {
                for(int i = 0; i < perThreadCount; i++)
                {
                    var obj = ConcurrentQueuePool<object>.TryGet() ?? new object();
                    ConcurrentQueuePool<object>.Put(obj);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = TotalCount)]
        public Task Locked()
        {
            var arr = new Task[Threads];
            int perThreadCount = TotalCount / Threads;
            Action work = InnerLoop;
            for (int i = 0; i < Threads; i++)
            {
                arr[i] = Task.Run(work);
            }
            return Task.WhenAll(arr);

            void InnerLoop()
            {
                for (int i = 0; i < perThreadCount; i++)
                {
                    var obj = LockedQueuePool<object>.TryGet() ?? new object();
                    LockedQueuePool<object>.Put(obj);
                }
            }
        }

        internal static class ConcurrentQueuePool<T> where T : class
        {
            internal static T TryGet() => GetShared();
            internal static void Put(T obj)
            {
                if (obj != null) PutShared(obj);
            }

            const int POOL_SIZE = 20;
            private static readonly ConcurrentQueue<T> s_pool = new ConcurrentQueue<T>();

            private static T GetShared()
            {
                var pool = s_pool;
                return pool.TryDequeue(out var next) ? next : null;
            }
            private static void PutShared(T obj)
            {
                var pool = s_pool;
                // there is an inherent race here - we may occasionally go slightly over,
                // but that isn't itself a problem - limit is just to prevent explosion
                if (pool.Count < POOL_SIZE)
                    pool.Enqueue(obj);
            }
        }
        internal static class LockedQueuePool<T> where T : class
        {
            internal static T TryGet() => GetShared();
            internal static void Put(T obj)
            {
                if (obj != null) PutShared(obj);
            }

            const int POOL_SIZE = 20;
            private static readonly Queue<T> s_pool = new Queue<T>(POOL_SIZE);

            private static T GetShared()
            {
                var pool = s_pool;
                lock (pool)
                {
                    return pool.Count == 0 ? null : pool.Dequeue();
                }
            }
            private static void PutShared(T obj)
            {
                var pool = s_pool;
                lock (pool)
                {
                    if (pool.Count < POOL_SIZE) pool.Enqueue(obj);
                }
            }
        }
    }
}
