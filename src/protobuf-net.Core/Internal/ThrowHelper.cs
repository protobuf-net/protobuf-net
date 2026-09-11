using System;
using System.Diagnostics.CodeAnalysis;
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

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null, and
        /// tells the compiler it is not null on return.
        /// </summary>
        /// <remarks>
        /// The <c>[NotNull]</c> post-condition is the whole point, and it is what a bare
        /// <c>if (x is null) ThrowArgumentNullException(...)</c> cannot give: testing a value for
        /// null WIDENS it to maybe-null for the rest of the method, so the guard meant to establish
        /// non-nullness is precisely what destroys it. The throw helpers here deliberately do not
        /// carry <c>[DoesNotReturn]</c> - gap B51/B48 records that it is flow analysis only and that
        /// the void-helper-plus-explicit-return shape is what lets ILC drop a gated body, measured.
        /// This attribute is flow analysis only too, so it emits no IL and costs nothing at runtime;
        /// the actual throw stays behind the NoInlining helper.
        /// </remarks>
#pragma warning disable CS8777 // ThrowArgumentNullException always throws; it just cannot say so
        public static void ThrowIfNull<T>([NotNull] T? value, string paramName)
        {
            if (value is null) ThrowArgumentNullException(paramName);
        }
#pragma warning restore CS8777

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
        internal static void ThrowInvalidOperationException(string? message = null, Exception? innerException = null)
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
                    ex = new InvalidOperationException($"Type '{type.NormalizeName()}' looks like a Google.Protobuf type; it cannot be used directly with protobuf-net without manual configuration; it may be possible to generate a protobuf-net type instead; see https://docs.protobuf-net.dev/contract_first", inner);
                }
            }
            // attempt to detect Google protobuf types, and give a suitable message
            throw ex;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
            => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(string message)
            => throw new NotSupportedException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowProtoException(string message, Exception? inner = null)
            => throw (inner is null ? new ProtoException(message) : new ProtoException(message, inner));

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflowException()
            => throw new OverflowException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotImplementedException([CallerMemberName] string? message = null)
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
