namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// net472 predates <c>init</c>, which needs this marker to compile at all - a pure compile-time
    /// type, so declaring it here is the usual workaround. Note this only makes the *fixture* legal;
    /// the generator still declines an init-only member here, for want of <c>[UnsafeAccessor]</c>.
    /// </summary>
    internal static class IsExternalInit { }
}
