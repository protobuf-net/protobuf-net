using System;
using System.Runtime.CompilerServices;

using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf.Internal
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRangeException(string paramName)
            => throw new ArgumentOutOfRangeException(paramName);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRangeException(string paramName, string message)
            => throw new ArgumentOutOfRangeException(paramName, message);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNullException(string paramName)
            => throw new ArgumentNullException(paramName);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNullException(string paramName, string message)
            => throw new ArgumentNullException(paramName, message);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowIndexOutOfRangeException()
            => throw new IndexOutOfRangeException();

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException(string message, string paramName)
            => throw new ArgumentException(message, paramName);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Format(string message)
            => throw new FormatException(message);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException(string message)
            => throw new ArgumentException(message);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
            => throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException(string message = null, Exception innerException = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                if (innerException is null) throw new InvalidOperationException();
                throw new InvalidOperationException(innerException.Message, innerException);
            }
            else
            {
                if (innerException is null) throw new InvalidOperationException(message);
                throw new InvalidOperationException(message, innerException);
            }
        }

        internal static void NoSerializerDefined(Type type)
        {
            var ex = new InvalidOperationException("No serializer defined for type: " + type.NormalizeName());

            if (type is not null && type.FindInterfaces((i, _) => i.FullName == "Google.Protobuf.IMessage", null).Length > 0)
            {
                try { throw ex; } // this is just to set the stack-trace
                catch (Exception inner)
                {
                    ex = new InvalidOperationException($"Type '{type.NormalizeName()}' looks like a Google.Protobuf type; it cannot be used directly with protobuf-net without manual configuration; it may be possible to generate a protobuf-net type instead; see https://protobuf-net.github.io/protobuf-net/contract_first", inner);
                }
            }
            // attempt to detect Google protobuf types, and give a suitable message
            throw ex;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
            => throw new NotSupportedException();

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(string message)
            => throw new NotSupportedException(message);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowProtoException(string message, Exception inner = null)
            => throw (inner is null ? new ProtoException(message) : new ProtoException(message, inner));

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflowException()
            => throw new OverflowException();

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotImplementedException([CallerMemberName] string message = null)
            => throw new NotImplementedException(message);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNullReferenceException()
            => throw new NullReferenceException();

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNullRepeatedContents<T>()
            => throw new NullReferenceException($"An element of type {typeof(T).NormalizeName()} was null; this might be as contents in a list/array");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTrackedObjects(object obj)
            => throw new NotSupportedException("tracked objects and featured related to stream rewriting are not supported on " + obj.GetType().Name);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNestedDataNotSupported(Type declaringType)
            => throw new NotSupportedException($"Nested or jagged lists, arrays and maps are not supported: {declaringType.NormalizeName()}");

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidPackedOperationException(WireType wireType, Type type)
            => throw new ProtoException($"Invalid wire-type for packed encoding: {wireType}; processing {type.NormalizeName()}");
    }
}
