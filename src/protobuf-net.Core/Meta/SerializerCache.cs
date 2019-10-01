using ProtoBuf.Internal;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Meta
{
    internal static class SerializerCache<TProvider>
             where TProvider : class
    {
        internal static readonly TProvider InstanceField = (TProvider)Activator.CreateInstance(typeof(TProvider), nonPublic: true);
        public static ISerializer<T> GetSerializer<T>()
            => SerializerCache<TProvider, T>.InstanceField;
    }

    internal static class SerializerCache<TProvider, T>
         where TProvider : class
    {
        internal static readonly ISerializer<T> InstanceField
            = (SerializerCache<TProvider>.InstanceField as ISerializer<T>)
            ?? SerializerCache.TryGetSecondary<ISerializer<T>>(SerializerCache<TProvider>.InstanceField, typeof(TProvider), typeof(T));

        public static ISerializer<T> Instance
        {
            [MethodImpl(ProtoReader.HotPath)]
            get => InstanceField;
        }
    }

    /// <summary>
    /// Provides access to cached serializers
    /// </summary>
    public static class SerializerCache
    {
        /// <summary>
        /// Gets a cached serializer instance for a type, in the context of a given provider
        /// </summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static ISerializer<T> Get<TProvider, T>()
            where TProvider : class
            => SerializerCache<TProvider, T>.InstanceField;

        internal static object GetInstance(Type providerType, Type type)
            => typeof(SerializerCache<,>).MakeGenericType(providerType, type)
                    .GetField(nameof(SerializerCache<PrimaryTypeProvider, string>.InstanceField),
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .GetValue(null);

        // this T is the entire interface, not the message-type; because we don't need a ton of
        // versions of the IL for each value-type
        internal static T TryGetSecondary<T>(object provider, Type providerType, Type type)
            where T : class
        {
            Type nullableUnderlyingType;
            if (type.IsValueType && (nullableUnderlyingType = Nullable.GetUnderlyingType(type)) != null)
            {
                var parent = GetInstance(providerType, nullableUnderlyingType);
                if (parent is T direct) return direct; // implements both T and T? - that'll work fine
                if (parent is object) // we have an implementation for T, and we're being asked for T? - wrap it
                {
                    var wrapperType = typeof(NullableWrapper<>).MakeGenericType(nullableUnderlyingType);
                    return (T)Activator.CreateInstance(wrapperType, args: new object[] { parent });
                }
            }

            if (provider is ISerializerFactory factory)
            {
                object obj = factory.TryCreate(type);
                if (obj is T typed) return typed; // service returned

                if (obj is Type returnedType)
                {
                    if (returnedType == type)
                    {   // returning the inpt is a special-case; it means the provider wants to claim
                        // something for IsDefined purposes, although we'll always use the inbuilt
                        // serializer; this basically means: "enums"
                        return (T)GetInstance(typeof(PrimaryTypeProvider), type);
                    }
                    else if (returnedType != providerType)
                    {
                        // for any other Type returned, we'll assume that you mean "use this other provider
                        // as a proxy"; obviously this makes no sense if tell us the same thing we're
                        // already looking for!
                        return (T)GetInstance(returnedType, type);
                    }
                }
            }

            return null;
        }

        internal sealed class NullableWrapper<T> : ISerializer<T?>
            where T : struct
        {
            private readonly ISerializer<T> _tail;

            public NullableWrapper(ISerializer<T> tail) => _tail = tail;

            public SerializerFeatures Features => _tail.Features;

            public T? Read(ref ProtoReader.State state, T? value) => _tail.Read(ref state, value.GetValueOrDefault());

            public void Write(ref ProtoWriter.State state, T? value) => _tail.Write(ref state, value.Value);
        }
    }
}
