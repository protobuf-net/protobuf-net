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
        internal static void ThrowArgumentException(string message)
            => throw new ArgumentException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
            => throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException()
            => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException(string message)
            => throw new InvalidOperationException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
            => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(string message)
            => throw new NotSupportedException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowProtoException(string message)
            => throw new ProtoException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflowException()
            => throw new OverflowException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotImplementedException(string message)
            => throw new NotImplementedException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNullReferenceException()
            => throw new NullReferenceException();
    }
}
