using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    /// <summary>
    /// This class exists soley to ensure that the known serializers are registered;
    /// By calling Init(), the static constructor is invoked (once only)
    /// </summary>
    internal static class SimpleSerializers
    {
        /// <summary>
        /// Forces the type initializer to register the simple serializer types
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Init() { }

        /// <summary>
        /// Initializes the KnownSerializers type, registering the simple serializers
        /// </summary>
        static SimpleSerializers()
        {
            SetNullable<int>(
                new Int32SignedVariantSerializer(),
                new Int32VariantSerializer());

            SetNullable<long>(
                new Int64SignedVariantSerializer(),
                new Int64VariantSerializer());

            SetNullable<float>(new SingleSerializer());
            SetNullable<double>(new DoubleSerializer());
            SetNullable<uint>(new UInt32VariantSerializer());
            SetNullable<ulong>(new UInt64VariantSerializer());
            SetNullable<bool>(new BooleanSerializer());
            SetNullable<DateTime>(new DateTimeSerializer());
            SetNullable<decimal>(new DecimalSignedSerializer(), new DecimalSerializer());

            Set<string>(new StringSerializer());
            Set<byte[]>(new BlobSerializer());
        }

        internal static void Set<TValue>(ISerializer<TValue> serializer)
        {
            SerializerCache<TValue>.Set(serializer, serializer);
        }
        static void Set<TValue>(ISerializer<TValue> signed, ISerializer<TValue> unsigned)
        {
            SerializerCache<TValue>.Set(signed, unsigned);
        }
        static void SetNullable<TValue>(ISerializer<TValue> serializer) where TValue : struct
        {
            Set<TValue>(serializer);
            Set<TValue?>(new NullableSerializer<TValue>(serializer));
        }
        static void SetNullable<TValue>(ISerializer<TValue> signed, ISerializer<TValue> unsigned) where TValue : struct
        {
            Set<TValue>(signed, unsigned);
            Set<TValue?>(new NullableSerializer<TValue>(signed),
                new NullableSerializer<TValue>(unsigned));
        }
    }
}
