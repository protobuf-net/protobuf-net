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

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static Func<ISerializationContext, T> Create<T>()
        {
            if (typeof(T).IsValueType) return ctx => default;
            return (Func<ISerializationContext, T>)typeof(ObjectFactory<>).MakeGenericType(
                typeof(T)).GetField(nameof(ObjectFactory<string>.Factory)).GetValue(null);
        }
    }
    internal static class ObjectFactory<T> where T : class
    {
        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx);

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
