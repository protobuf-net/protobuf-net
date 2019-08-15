using System;

namespace ProtoBuf.Internal
{
    internal static class Pool<T> where T : class
    {
        //#if !PLAT_NO_THREADSTATIC
        //        [ThreadStatic]
        //        private static T ts_local;
        //        public static T TryGet()
        //        {
        //            var obj = ts_local;
        //            ts_local = null;
        //            return obj;
        //        }
        //        public static void Put(T obj)
        //            => ts_local = obj;

// #elif !PLAT_NO_INTERLOCKED
#if !PLAT_NO_INTERLOCKED
        private static object s_global;
        public static T TryGet()
            => (T)System.Threading.Interlocked.Exchange(ref s_global, null);
        public static void Put(T obj)
            => System.Threading.Interlocked.Exchange(ref s_global, obj);
#else
        public static T TryGet() => null;
        public static void Put(T _) {}
#endif
    }
}
