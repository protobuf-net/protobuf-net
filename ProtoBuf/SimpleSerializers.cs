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
            Set<int>(
                new Int32SignedVariantSerializer(),
                new Int32SignedVariantSerializer(),
                new Int32VariantSerializer(),
                null);

            Set<long>(
                new Int64SignedVariantSerializer(),
                new Int64SignedVariantSerializer(),
                new Int64VariantSerializer(),
                null);

            Set<float>(new SingleSerializer(),null,null,new SingleSerializer());
            Set<double>(new DoubleSerializer(),null,null,new DoubleSerializer());
            Set<uint>(new UInt32VariantSerializer(), null, null, null);
            Set<ulong>(new UInt64VariantSerializer(), null, null, null);
            Set<bool>(new BooleanSerializer(), null, null,new BooleanSerializer());
            Set<DateTime>(new DateTimeSerializer(), new DateTimeSerializer(), null, null);
            Set<decimal>(new DecimalSignedSerializer(),
                new DecimalSignedSerializer(),
                new DecimalSerializer(),
                null);

            Set<string>(new StringSerializer());
            Set<byte[]>(new BlobSerializer());
        }

        internal static void Set<TValue>(ISerializer<TValue> serializer) 
        {
            SerializerCache<TValue>.Set(serializer, null, null, null);
        }
        static ISerializer<T?> Wrap<T>(ISerializer<T> serializer) where T : struct
        {
            return serializer == null ? null : new NullableSerializer<T>(serializer);
        }
        static void Set<TValue>(ISerializer<TValue> @default, ISerializer<TValue> zigZag,
            ISerializer<TValue> twosComplement, ISerializer<TValue> fixedSize) where TValue : struct
        {
            SerializerCache<TValue>.Set(@default, zigZag, twosComplement, fixedSize);
            SerializerCache<TValue?>.Set(Wrap(@default), Wrap(zigZag), Wrap(twosComplement), Wrap(fixedSize));
        }
    }
}
