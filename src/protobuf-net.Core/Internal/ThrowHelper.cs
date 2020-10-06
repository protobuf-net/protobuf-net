using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRangeException(string paramName)
            => throw new ArgumentOutOfRangeException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRangeException(string paramName, string message)
            => throw new ArgumentOutOfRangeException(paramName, message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNullException(string paramName)
            => throw new ArgumentNullException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNullException(string paramName, string message)
            => throw new ArgumentNullException(paramName, message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowIndexOutOfRangeException()
            => throw new IndexOutOfRangeException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException(string message, string paramName)
            => throw new ArgumentException(message, paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Format(string message)
            => throw new FormatException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException(string message)
            => throw new ArgumentException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
            => throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");


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

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
            => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(string message)
            => throw new NotSupportedException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowProtoException(string message, Exception inner = null)
            => throw (inner is null ? new ProtoException(message) : new ProtoException(message, inner));

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflowException()
            => throw new OverflowException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotImplementedException([CallerMemberName] string message = null)
            => throw new NotImplementedException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNullReferenceException()
            => throw new NullReferenceException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNullRepeatedContents<T>()
            => throw new NullReferenceException($"An element of type {typeof(T).NormalizeName()} was null; this might be as contents in a list/array");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTrackedObjects(object obj)
            => throw new NotSupportedException("tracked objects and featured related to stream rewriting are not supported on " + obj.GetType().Name);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNestedDataNotSupported(Type declaringType)
            => throw new NotSupportedException($"Nested or jagged lists, arrays and maps are not supported: {declaringType.NormalizeName()}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidPackedOperationException(WireType wireType, Type type)
            => throw new ProtoException($"Invalid wire-type for packed encoding: {wireType}; processing {type.NormalizeName()}");
    }
}
