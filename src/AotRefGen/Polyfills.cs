namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// The marker the compiler requires for an <c>init</c> accessor. This project is net472, which
    /// predates it, and the shared fixtures use <c>init</c>; the type is a pure compile-time marker,
    /// so declaring it here is enough.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
