using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf.Reflection.Internal
{
    /// <summary>
    /// <see cref="string.IsNullOrEmpty(string)"/> and <see cref="string.IsNullOrWhiteSpace(string)"/>,
    /// carrying the nullability post-condition that this project's reference assemblies do not.
    /// </summary>
    /// <remarks>
    /// This library targets net462 and netstandard2.0, whose reference assemblies are oblivious - so
    /// the BCL methods declare nothing about their argument and the compiler cannot narrow through
    /// them. The guarded use that always follows therefore warns, once per site, and the only ways
    /// out are a null-forgiving operator at every one of them or this. The attribute itself is
    /// polyfilled in protobuf-net.Core and reached by <c>[InternalsVisibleTo]</c>.
    /// </remarks>
    internal static class StringNullability
    {
        /// <inheritdoc cref="string.IsNullOrEmpty(string)"/>
        public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
            => string.IsNullOrEmpty(value);

        /// <inheritdoc cref="string.IsNullOrWhiteSpace(string)"/>
        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
            => string.IsNullOrWhiteSpace(value);
    }
}
