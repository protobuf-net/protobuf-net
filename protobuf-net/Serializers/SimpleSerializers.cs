using System;
using System.Runtime.CompilerServices;
using ProtoBuf.Serializers;

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
            
            

            Set<float>(FixedSerializer.Default, null, null, FixedSerializer.Default);
            Set<double>(FixedSerializer.Default, null, null, FixedSerializer.Default);
            Set<bool>(BooleanSerializer.Default, null, null, BooleanSerializer.Default);
            
            Set<long>(TwosComplementSerializer.Default, ZigZagSerializer.Default, TwosComplementSerializer.Default, FixedSerializer.Default);
            Set<int>(TwosComplementSerializer.Default, ZigZagSerializer.Default, TwosComplementSerializer.Default, FixedSerializer.Default);
            Set<short>(ZigZagSerializer.Default, ZigZagSerializer.Default, TwosComplementSerializer.Default, null);
            Set<sbyte>(ZigZagSerializer.Default, ZigZagSerializer.Default, TwosComplementSerializer.Default, null);

            Set<ulong>(TwosComplementSerializer.Default, null, TwosComplementSerializer.Default, FixedSerializer.Default);
            Set<uint>(TwosComplementSerializer.Default, null, TwosComplementSerializer.Default, FixedSerializer.Default);
            Set<ushort>(TwosComplementSerializer.Default, null, TwosComplementSerializer.Default, null);
            Set<char>(TwosComplementSerializer.Default, null, TwosComplementSerializer.Default, null);
            Set<byte>(TwosComplementSerializer.Default, null, TwosComplementSerializer.Default, null);
            
            Set<string>(StringSerializer.Default);
            Set<byte[]>(BlobSerializer.Default);

            /*
            Set<DateTime>(ZigZagSerializer.Default, ZigZagSerializer.Default, null, null);
            Set<TimeSpan>(ZigZagSerializer.Default, ZigZagSerializer.Default, null, null);
            Set<decimal>(TwosComplementSerializer.Default, ZigZagSerializer.Default, TwosComplementSerializer.Default, null);
            Set<Guid>(GuidSerializer.Default, null, null, GuidSerializer.Default);
             */
            Set<TimeSpan>(BclSerializer.Default, null, null, null);
            Set<DateTime>(BclSerializer.Default, null, null, null);
            Set<decimal>(BclSerializer.Default, null, null, null);
            Set<Guid>(BclSerializer.Default, null, null, null);
            Set<Uri>(UriSerializer.Default);
        }

        internal static void Set<TValue>(ISerializer<TValue> serializer) 
        {
            SerializerCache<TValue>.Set(serializer, null, null, null);
        }

        //private static ISerializer<T?> Wrap<T>(ISerializer<T> serializer) where T : struct
        //{
        //    return serializer == null ? null : new NullableSerializer<T>(serializer);
        //}

        private static void Set<TValue>(
            ISerializer<TValue> @default,
            ISerializer<TValue> zigZag,
            ISerializer<TValue> twosComplement,
            ISerializer<TValue> fixedSize) where TValue : struct
        {
            SerializerCache<TValue>.Set(@default, zigZag, twosComplement, fixedSize);
            //SerializerCache<TValue?>.Set(Wrap(@default), Wrap(zigZag), Wrap(twosComplement), Wrap(fixedSize));
        }
    }
}
