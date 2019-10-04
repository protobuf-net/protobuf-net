using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    internal static class TypeHelper
    {
        internal static string NormalizeName(this Type type)
        {
            return type?.ToString();
            //if (type == null) return null;
            //if (type.IsEnum) return type.Name;

            //var nullable = Nullable.GetUnderlyingType(type);
            //if (nullable != null) return CSName(nullable) + "?";

            //if (!type.IsGenericType)
            //{
            //    return (Type.GetTypeCode(type)) switch
            //    {
            //        TypeCode.Boolean => "bool",
            //        TypeCode.Char => "char",
            //        TypeCode.SByte => "sbyte",
            //        TypeCode.Byte => "byte",
            //        TypeCode.Int16 => "short",
            //        TypeCode.UInt16 => "ushort",
            //        TypeCode.Int32 => "int",
            //        TypeCode.UInt32 => "uint",
            //        TypeCode.Int64 => "long",
            //        TypeCode.UInt64 => "ulong",
            //        TypeCode.Single => "float",
            //        TypeCode.Double => "double",
            //        TypeCode.Decimal => "decimal",
            //        TypeCode.String => "string",
            //        _ => type.Name,
            //    };
            //}

            //var withTicks = type.Name;
            //var index = withTicks.IndexOf('`');
            //if (index < 0) return type.Name;

            //var sb = new StringBuilder();
            //sb.Append(type.Name.Substring(0, index)).Append('<');
            //var args = type.GetGenericArguments();
            //for (int i = 0; i < args.Length; i++)
            //{
            //    if (i != 0) sb.Append(',');
            //    sb.Append(CSName(args[i]));
            //}
            //return sb.Append('>').ToString();
        }

        internal static bool CanBePacked(Type type)
        {
            if (type.IsEnum) return true;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Boolean:
                case TypeCode.Char:
                    return true;
            }
            return false;
        }

        internal static bool ResolveUniqueEnumerableT(Type type, out Type t)
        {
            static bool IsEnumerableT(Type type, out Type t)
            {
                if (type.IsInterface && type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    t = type.GetGenericArguments()[0];
                    return true;
                }
                t = null;
                return false;
            }

            if (type == null
                || type == typeof(string) || type == typeof(byte[]) || type == typeof(object))
            {
                t = null; // don't need that kind of confusion
                return false;
            }

            if (type.IsArray)
            {
                t = type.GetElementType();
                return type == t.MakeArrayType(); // rules out multi-dimensional etc
            }

            bool haveMatch = false;
            t = null;
            try
            {
                if (IsEnumerableT(type, out t))
                    return true;

                foreach (var iType in type.GetInterfaces())
                {
                    if (IsEnumerableT(iType, out var tmp))
                    {
                        if (haveMatch && tmp != t)
                        {
                            haveMatch = false;
                            break;
                        }
                        else
                        {
                            haveMatch = true;
                            t = tmp;
                        }
                    }
                }
            }
            catch { }

            if (haveMatch)
            {
                if (type.IsValueType) CheckValidValueTypeCollections(type, t);
                return true;
            }
            // if it isn't a good fit; don't use "map"
            t = null;
            return false;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckValidValueTypeCollections(Type type, Type t)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            {
                return; //fine, we handle that
            }
            ThrowHelper.ThrowNotSupportedException("Value-type collection types are not currently supported");
        }
    }
    internal static class ObjectFactory
    {
        internal static Func<T> TryCreate<T>(out object singletonOrConstructType) where T : class
        {
            singletonOrConstructType = null;
            try
            {
                Type requstedType = typeof(T);
                if (requstedType.IsGenericType)
                {
                    if (requstedType.IsInterface)
                    {
                        var generic = requstedType.GetGenericTypeDefinition();
                        Type parentType = null;
                        if (generic == typeof(IList<>) || generic == typeof(ICollection<T>))
                        {
                            singletonOrConstructType = typeof(List<>).MakeGenericType(requstedType.GetGenericArguments());
                            return null;
                        }
                        if (generic == typeof(IDictionary<,>))
                        {
                            singletonOrConstructType = typeof(Dictionary<,>).MakeGenericType(requstedType.GetGenericArguments());
                            return null;
                        }

                        if (generic == typeof(IImmutableDictionary<,>)) parentType = typeof(ImmutableDictionary<,>);
                        else if (generic == typeof(IReadOnlyDictionary<,>)) parentType = typeof(ImmutableDictionary<,>);
                        else if (generic == typeof(IReadOnlyList<>)) parentType = typeof(ImmutableList<>);
                        else if (generic == typeof(IReadOnlyCollection<>)) parentType = typeof(ImmutableList<>);
                        else if (generic == typeof(IImmutableList<>)) parentType = typeof(ImmutableList<>);
                        else if (generic == typeof(IImmutableQueue<>)) parentType = typeof(ImmutableQueue<>);
                        else if (generic == typeof(IImmutableSet<>)) parentType = typeof(ImmutableHashSet<>);
                        else if (generic == typeof(IImmutableStack<>)) parentType = typeof(ImmutableStack<>);

                        if (parentType != null) // move from the interface to something more concrete
                        {
                            requstedType = parentType.MakeGenericType(requstedType.GetGenericArguments());
                        }
                    }

                    if (requstedType.IsClass && requstedType.Name.StartsWith("Immutable"))
                    {   // look for a {type}.Empty
                        var field = requstedType.GetField(nameof(ImmutableList<string>.Empty), BindingFlags.Public | BindingFlags.Static);
                        if (field != null && field.IsInitOnly)
                        {
                            singletonOrConstructType = field.GetValue(null) as T;
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }
    }
    internal static class ObjectFactory<T> where T : class
    {
        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx, null);

        private static readonly object _singletonOrConstructType;
        private static readonly Func<T> _specialFactory = ObjectFactory.TryCreate<T>(out _singletonOrConstructType);

        public static T Create()
        {
            try
            {
                return _singletonOrConstructType as T
                    ?? _specialFactory?.Invoke()
                    ?? (T)Activator.CreateInstance(_singletonOrConstructType as Type ?? typeof(T), nonPublic: true);
            }
            catch (MissingMethodException mme)
            {
                TypeModel.ThrowCannotCreateInstance(typeof(T), mme);
                return default;
            }
        }
    }

    internal static class TypeHelper<T>
    {
        public static readonly bool IsReferenceType = !typeof(T).IsValueType;

        public static readonly bool CanBeNull = default(T) == null;

        public static readonly bool CanBePacked = !IsReferenceType && TypeHelper.CanBePacked(typeof(T));

        // make sure we don't cast null value-types to NREs
        [MethodImpl(ProtoReader.HotPath)]
        public static T FromObject(object value) => value == null ? default : (T)value;
    }
}
