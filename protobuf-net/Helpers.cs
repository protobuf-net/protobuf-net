
using System;
using System.Reflection;
namespace ProtoBuf
{
    /// <summary>
    /// Not all frameworks are created equal (fx1.1 vs fx2.0,
    /// micro-framework, compact-framework,
    /// silverlight, etc). This class simply wraps up a few things that would
    /// otherwise make the real code unnecessarily messy, providing fallback
    /// implementations if necessary.
    /// </summary>
    internal class Helpers
    {
        private Helpers() { }

        public static bool IsNullOrEmpty(string value)
        { // yes, FX11 lacks this!
            return value == null || value.Length == 0;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugWriteLine(string message, object obj)
        {
            string suffix;
            try
            {
                suffix = obj == null ? "(null)" : obj.ToString();
            }
            catch
            {
                suffix = "(exception)";
            }
            DebugWriteLine(message + ": " + suffix);
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugWriteLine(string message)
        {
#if MF      
            Microsoft.SPOT.Debug.Print(message);
#else
            System.Diagnostics.Debug.WriteLine(message);
#endif
        }
        [System.Diagnostics.Conditional("TRACE")]
        public static void TraceWriteLine(string message)
        {
#if MF
            Microsoft.SPOT.Trace.Print(message);
#elif SILVERLIGHT || MONODROID || CF2
            System.Diagnostics.Debug.WriteLine(message);
#else
            System.Diagnostics.Trace.WriteLine(message);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugAssert(bool condition, string message)
        {
#if MF
            Microsoft.SPOT.Debug.Assert(condition, message);
#else
            System.Diagnostics.Debug.Assert(condition, message);
            
#endif
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugAssert(bool condition)
        {
            
#if MF
            Microsoft.SPOT.Debug.Assert(condition);
#else
            System.Diagnostics.Debug.Assert(condition);
#endif
        }
#if !NO_RUNTIME
        public static void Sort(int[] keys, object[] values)
        {
            // bubble-sort; it'll work on MF, has small code,
            // and works well-enough for our sizes. This approach
            // also allows us to do `int` compares without having
            // to go via IComparable etc, so win:win
            bool swapped;
            do {
                swapped = false;
                for (int i = 1; i < keys.Length; i++) {
                    if (keys[i - 1] > keys[i]) {
                        int tmpKey = keys[i];
                        keys[i] = keys[i - 1];
                        keys[i - 1] = tmpKey;
                        object tmpValue = values[i];
                        values[i] = values[i - 1];
                        values[i - 1] = tmpValue;
                        swapped = true;
                    }
                }
            } while (swapped);
        }
#endif
        public static void BlockCopy(byte[] from, int fromIndex, byte[] to, int toIndex, int count)
        {
#if MF
            Array.Copy(from, fromIndex, to, toIndex, count);
#else
            Buffer.BlockCopy(from, fromIndex, to, toIndex, count);
#endif
        }
        public static bool IsInfinity(float value)
        {
#if MF
            const float inf = (float)1.0 / (float)0.0, minf = (float)-1.0F / (float)0.0;
            return value == inf || value == minf;
#else
            return float.IsInfinity(value);
#endif
        }
        internal static MethodInfo GetInstanceMethod(Type declaringType, string name, Type[] types)
        {
            if(types == null) types = EmptyTypes;
            return declaringType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, types, null);
        }
        public static bool IsInfinity(double value)
        {
#if MF
            const double inf = (double)1.0 / (double)0.0, minf = (double)-1.0F / (double)0.0;
            return value == inf || value == minf;
#else
            return double.IsInfinity(value);
#endif
        }
        public readonly static Type[] EmptyTypes = new Type[0];
//        internal static object CreateInstance(Type forType)
//        {
//#if MF
//            return forType.GetConstructor(EmptyTypes).Invoke(null);
//#else
//            return Activator.CreateInstance(forType);
//#endif
//        }
    }
}
