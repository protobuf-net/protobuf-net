using System;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    internal static class Pool<T> where T : class
    {
        [ThreadStatic]
        private static T ts_local;

        internal static T TryGet()
        {
            var tmp = ts_local;
            if (tmp is object)
            {
                ts_local = null;
                return tmp;
            }
            return GetShared();
        }
        internal static void Put(T obj)
        {
            if (obj is object)
            {
                if (ts_local is null)
                {
                    ts_local = obj;
                    return;
                }
                PutShared(obj);
            }
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