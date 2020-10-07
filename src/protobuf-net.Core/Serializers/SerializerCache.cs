using ProtoBuf.Internal;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    internal static class SerializerCache<[DynamicallyAccessedMembers(DynamicAccess.Serializer)] TProvider>
             where TProvider : class
    {
        internal static readonly TProvider InstanceField = (TProvider)Activator.CreateInstance(typeof(TProvider), nonPublic: true);
        public static ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<TProvider, T>.InstanceField;
    }

    //internal static class SerializerSingleton<TSerializer, T>
    //    where TSerializer : class, ISerializer<T>, new()
    //{
    //    public static readonly TSerializer InstanceField = new TSerializer();
    //}

    internal static class SerializerCache<[DynamicallyAccessedMembers(DynamicAccess.Serializer)] TProvider, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>
         where TProvider : class
    {
        internal static readonly ISerializer<T> InstanceField
            = SerializerCache.Verify(
                SerializerCache<TProvider>.InstanceField as ISerializer<T>
                ?? (SerializerCache<TProvider>.InstanceField as ISerializerProxy<T>)?.Serializer);
                // ?? SerializerCache.TryGetSecondary<ISerializer<T>>(SerializerCache<TProvider>.InstanceField, typeof(TProvider), typeof(T)));

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
        ///// <summary>
        ///// Provides access to a singleton instance of a given serializer
        ///// </summary>
        //[MethodImpl(ProtoReader.HotPath)]
        //public static TSerializer GetSingleton<TSerializer, T>()
        //    where TSerializer : class, ISerializer<T>, new()
        //    => SerializerSingleton<TSerializer, T>.InstanceField;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowInvalidSerializer<T>(ISerializer<T> serializer, string message, Exception innerException = null)
        {
            var tName = typeof(T).NormalizeName();
            var sName = serializer.GetType().NormalizeName();
            var fName = serializer.Features.ToString();
            try
            {
                ThrowHelper.ThrowInvalidOperationException(
                    $"The serializer {sName} for type {tName} has invalid features: {message} ({fName})", innerException);
            }
            catch(InvalidOperationException ex)
            {
                ex.Data.Add("type", tName);
                ex.Data.Add("serializer", sName);
                ex.Data.Add("features", fName);
                throw;
            }
        }

        // check a few things that should be true for valid serializers
        internal static ISerializer<T> Verify<T>(ISerializer<T> serializer)
        {
            if (serializer is null) return null;

            try
            {
                var features = serializer.Features;
                if (serializer is IRepeatedSerializer<T>)
                {
                    const SerializerFeatures PermittedRepeatedFeatures = SerializerFeatures.CategoryRepeated;
                    if ((features & ~PermittedRepeatedFeatures) != 0) ThrowInvalidSerializer(serializer, $"repeated serializers may only specify {PermittedRepeatedFeatures}");
                    return serializer;
                }

                var wireType = features.GetWireType(); // this also asserts that a wire-type is set
                switch (wireType)
                {
                    case WireType.Varint:
                    case WireType.Fixed32:
                    case WireType.Fixed64:
                    case WireType.SignedVarint:
                    case WireType.String:
                    case WireType.StartGroup:
                        break;
                    default:
                        ThrowInvalidSerializer(serializer, $"invalid wire-type {wireType}");
                        break;
                }

                switch (features.GetCategory())
                {
                    case SerializerFeatures.CategoryMessage:
                    case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        if (TypeHelper<T>.CanBePacked) ThrowInvalidSerializer(serializer, "message serializer specified for a type that can be 'packed'");
                        break;
                    case SerializerFeatures.CategoryScalar:
                        
                        if (TypeHelper<T>.CanBePacked)
                        {
                            switch (wireType)
                            {
                                case WireType.Varint:
                                case WireType.Fixed32:
                                case WireType.Fixed64:
                                case WireType.SignedVarint:
                                    break;
                                default:
                                    ThrowInvalidSerializer(serializer, "invalid wire-type for a type that can be 'packed'");
                                    break;
                            }
                        }
                        break;
                    default:
                        features.ThrowInvalidCategory();
                        break;
                }

                // some features should only be specified by the caller
                const SerializerFeatures InvalidFeatures = SerializerFeatures.OptionClearCollection | SerializerFeatures.OptionPackedDisabled;
                if ((features & InvalidFeatures) != 0) ThrowInvalidSerializer(serializer, $"serializers should not specify {InvalidFeatures}");

                return serializer;
            }
            catch(InvalidOperationException ex) when (!ex.Data.Contains("serializer"))
            {
                ThrowInvalidSerializer(serializer, ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a cached serializer instance for a type, in the context of a given provider
        /// </summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static ISerializer<T> Get<TProvider, T>()
            where TProvider : class
            => SerializerCache<TProvider, T>.InstanceField;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object GetInstance(Type providerType, Type type)
            => typeof(SerializerCache<,>).MakeGenericType(providerType, type)
                    .GetField(nameof(SerializerCache<PrimaryTypeProvider, string>.InstanceField),
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .GetValue(null);

        //// this T is the entire interface, not the message-type; because we don't need a ton of
        //// versions of the IL for each value-type
        //internal static T TryGetSecondary<T>(object provider, Type providerType, Type type)
        //    where T : class
        //{
        //    Type nullableUnderlyingType;
        //    if (type.IsValueType && (nullableUnderlyingType = Nullable.GetUnderlyingType(type)) is object)
        //    {
        //        var parent = GetInstance(providerType, nullableUnderlyingType);
        //        if (parent is T direct) return direct; // implements both T and T? - that'll work fine
        //        if (parent is object) // we have an implementation for T, and we're being asked for T? - wrap it
        //        {
        //            var wrapperType = typeof(NullableWrapper<>).MakeGenericType(nullableUnderlyingType);
        //            return (T)Activator.CreateInstance(wrapperType, args: new object[] { parent });
        //        }
        //    }

        //    if (provider is ILegacySerializerFactory factory)
        //    {
        //        object obj = factory.TryCreate(type);
        //        if (obj is T typed) return typed; // service returned

        //        if (obj is Type returnedType)
        //        {
        //            if (returnedType == type)
        //            {   // returning the inpt is a special-case; it means the provider wants to claim
        //                // something for IsDefined purposes, although we'll always use the inbuilt
        //                // serializer; this basically means: "enums"
        //                return (T)GetInstance(typeof(PrimaryTypeProvider), type);
        //            }
        //            else if (returnedType != providerType)
        //            {
        //                // for any other Type returned, we'll assume that you mean "use this other provider
        //                // as a proxy"; obviously this makes no sense if tell us the same thing we're
        //                // already looking for!
        //                return (T)GetInstance(returnedType, type);
        //            }
        //        }
        //    }

        //    return null;
        //}

        //internal sealed class NullableWrapper<T> : ISerializer<T?>
        //    where T : struct
        //{
        //    private readonly ISerializer<T> _tail;

        //    public NullableWrapper(ISerializer<T> tail) => _tail = tail;

        //    public SerializerFeatures Features => _tail.Features;

        //    public T? Read(ref ProtoReader.State state, T? value) => _tail.Read(ref state, value.GetValueOrDefault());

        //    public void Write(ref ProtoWriter.State state, T? value) => _tail.Write(ref state, value.Value);
        //}
    }
}
