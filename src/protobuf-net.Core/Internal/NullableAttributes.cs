// The nullability post-condition attributes, polyfilled for the target frameworks whose reference
// assemblies do not carry them.
//
// This matters more than it looks. net462 and netstandard2.0 reference assemblies are entirely
// OBLIVIOUS, so `string.IsNullOrWhiteSpace(x)` does not narrow `x` and every guarded use of it
// warns - the single largest source of NRT noise in this repo (gap B51). The fix is a helper that
// states the post-condition the BCL method cannot, and that needs these attributes to exist.
//
// The compiler matches them BY NAME, so an internal copy works exactly as the real thing.
//
// Declared here, in Core, rather than per-project, and shared by [InternalsVisibleTo] - because
// protobuf-net.BuildTools does not REFERENCE Core and Reflection, it compiles their sources in, so
// a copy in each would be two declarations in one compilation (CS0101). One copy, reached from the
// others, is the only arrangement that survives that. (BuildTools additionally sees an INACCESSIBLE
// NotNullWhenAttribute from the Roslyn packages; a copy compiled into the same assembly wins over
// it, which is the other reason this cannot simply be a package reference.)
#if !NETSTANDARD2_1_OR_GREATER && !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }
}
#endif
