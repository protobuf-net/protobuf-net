using System;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    internal static class Pool<T> where T : class
    {
#if !PLAT_NO_THREADSTATIC
        [ThreadStatic]
        private static T ts_local;
#endif
        internal static T TryGet()
        {
#if !PLAT_NO_THREADSTATIC
            var tmp = ts_local;
            if (tmp != null)
            {
                ts_local = null;
                return tmp;
            }
#endif
            return GetShared();
        }
        internal static void Put(T obj)
        {
            if (obj != null)
            {
#if !PLAT_NO_THREADSTATIC
                if (ts_local == null)
                {
                    ts_local = obj;
                    return;
                }
#endif
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