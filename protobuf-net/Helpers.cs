
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
#elif SILVERLIGHT || MONODROID || CF2 || WINRT || IOS
            System.Diagnostics.Debug.WriteLine(message);
#else
            System.Diagnostics.Trace.WriteLine(message);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugAssert(bool condition, string message)
        {
            if (!condition)
            {
#if MF
                Microsoft.SPOT.Debug.Assert(false, message);
#else
                System.Diagnostics.Debug.Assert(false, message);
            }
#endif
        }
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugAssert(bool condition, string message, params object[] args)
        {
            if (!condition) DebugAssert(false, string.Format(message, args));
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
#if MF || WINRT
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
#if WINRT
        internal static MethodInfo GetInstanceMethod(TypeInfo declaringType, string name)
        {
            foreach (MethodInfo method in declaringType.DeclaredMethods)
            {
                if (!method.IsStatic && method.Name == name)
                {
                    return method;
                }
            }
            return null;
        }
        internal static MethodInfo GetStaticMethod(TypeInfo declaringType, string name)
        {
            foreach (MethodInfo method in declaringType.DeclaredMethods)
            {
                if (method.IsStatic && method.Name == name)
                {
                    return method;
                }
            }
            return null;
        }
        internal static MethodInfo GetInstanceMethod(TypeInfo declaringType, string name, Type[] types)
        {
            if (types == null) types = EmptyTypes;
            foreach (MethodInfo method in declaringType.DeclaredMethods)
            {
                if (!method.IsStatic && method.Name == name)
                {
                    if(IsMatch(method.GetParameters(), types)) return method;
                }
            }
            return null;
        }
#else
        internal static MethodInfo GetInstanceMethod(Type declaringType, string name)
        {
            return declaringType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        internal static MethodInfo GetStaticMethod(Type declaringType, string name)
        {
            return declaringType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        internal static MethodInfo GetInstanceMethod(Type declaringType, string name, Type[] types)
        {
            if(types == null) types = EmptyTypes;
            return declaringType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, types, null);
        }
#endif
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

#if WINRT
        private static readonly Type[] knownTypes = new Type[] {
                typeof(bool), typeof(char), typeof(sbyte), typeof(byte),
                typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(float), typeof(double),
                typeof(decimal), typeof(string),
                typeof(DateTime), typeof(TimeSpan), typeof(Guid), typeof(Uri),
                typeof(byte[])};
        private static readonly ProtoTypeCode[] knownCodes = new ProtoTypeCode[] {
            ProtoTypeCode.Boolean, ProtoTypeCode.Char, ProtoTypeCode.SByte, ProtoTypeCode.Byte,
            ProtoTypeCode.Int16, ProtoTypeCode.UInt16, ProtoTypeCode.Int32, ProtoTypeCode.UInt32,
            ProtoTypeCode.Int64, ProtoTypeCode.UInt64, ProtoTypeCode.Single, ProtoTypeCode.Double,
            ProtoTypeCode.Decimal, ProtoTypeCode.String,
            ProtoTypeCode.DateTime, ProtoTypeCode.TimeSpan, ProtoTypeCode.Guid, ProtoTypeCode.Uri,
            ProtoTypeCode.ByteArray
        };

#endif
        public static ProtoTypeCode GetTypeCode(Type type)
        {
#if WINRT
            
            int idx = Array.IndexOf<Type>(knownTypes, type);
            if (idx >= 0) return knownCodes[idx];
            return type == null ? ProtoTypeCode.Empty : ProtoTypeCode.Unknown;
#else
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Empty:
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String:
                    return (ProtoTypeCode)code;
            }
            if (type == typeof(TimeSpan)) return ProtoTypeCode.TimeSpan;
            if (type == typeof(Guid)) return ProtoTypeCode.Guid;
            if (type == typeof(Uri)) return ProtoTypeCode.Uri;
            if (type == typeof(byte[])) return ProtoTypeCode.ByteArray;

            return ProtoTypeCode.Unknown;
#endif
        }



        internal static bool IsValueType(Type type)
        {
#if WINRT
            return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

        internal static MethodInfo GetGetMethod(PropertyInfo property, bool nonPublic)
        {
#if WINRT
            MethodInfo method = property.GetMethod;
            if (!nonPublic && method != null && !method.IsPublic) method = null;
            return method;
#else
            return property.GetGetMethod(nonPublic);
#endif
        }
        internal static MethodInfo GetSetMethod(PropertyInfo property, bool nonPublic)
        {
#if WINRT
            MethodInfo method = property.SetMethod;
            if (!nonPublic && method != null && !method.IsPublic) method = null;
            return method;
#else
            return property.GetSetMethod(nonPublic);
#endif
        }

#if WINRT
        private static bool IsMatch(ParameterInfo[] parameters, Type[] parameterTypes)
        {
            if (parameterTypes == null) parameterTypes = EmptyTypes;
            if (parameters.Length != parameterTypes.Length) return false;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i]) return false;
            }
            return true;
        }
        internal static ConstructorInfo GetConstructor(TypeInfo type, Type[] parameterTypes, bool nonPublic)
        {
            foreach (ConstructorInfo ctor in type.DeclaredConstructors)
            {
                if (!nonPublic && !ctor.IsPublic) continue;
                if (IsMatch(ctor.GetParameters(), parameterTypes)) return ctor;
            }
            return null;
        }
        internal static ConstructorInfo[] GetConstructors(TypeInfo typeInfo, bool nonPublic)
        {
            if (nonPublic) return System.Linq.Enumerable.ToArray(typeInfo.DeclaredConstructors);
            return System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Where(typeInfo.DeclaredConstructors, x => x.IsPublic));
        }
#else
        internal static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes, bool nonPublic)
        {
            return type.GetConstructor(
                nonPublic ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                          : BindingFlags.Instance | BindingFlags.Public,
                    null, parameterTypes, null);

        }
        internal static ConstructorInfo[] GetConstructors(Type type, bool nonPublic)
        {
            return type.GetConstructors(
                nonPublic ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                          : BindingFlags.Instance | BindingFlags.Public);
        }
#endif

    }
    /// <summary>
    /// Intended to be a direct map to regular TypeCode, but:
    /// - with missing types
    /// - existing on WinRT
    /// </summary>
    internal enum ProtoTypeCode
    {
        Empty = 0,
        Unknown = 1, // maps to TypeCode.Object
        Boolean = 3,
        Char = 4,
        SByte = 5,
        Byte = 6,
        Int16 = 7,
        UInt16 = 8,
        Int32 = 9,
        UInt32 = 10,
        Int64 = 11,
        UInt64 = 12,
        Single = 13,
        Double = 14,
        Decimal = 15,
        DateTime = 16,
        String = 18,

        // additions
        TimeSpan = 100,
        ByteArray = 101,
        Guid = 102,
        Uri = 103
    }
}
