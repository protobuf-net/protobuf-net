// The nullability post-condition attributes, polyfilled for the target frameworks whose reference
// assemblies do not carry them.
//
// This matters more than it looks. net462 and netstandard2.0 reference assemblies are entirely
// OBLIVIOUS, so `string.IsNullOrWhiteSpace(x)` does not narrow `x` and every guarded use of it
// warns - the single largest source of NRT noise in this repo (gap B51). The fix is a helper that
// states the post-condition the BCL method cannot, and that needs these attributes to exist.
// MemberNotNullWhen and AllowNull are separately needed by Descriptor.cs, which is protogen output
// and so carries whatever protogen emits.
//
// The compiler matches them BY NAME, so an internal copy works exactly as the real thing.
//
// IT HAS TO LIVE IN THE ASSEMBLY THAT USES IT, and that is not a style preference - it was tried in
// protobuf-net.Core, shared by [InternalsVisibleTo], and it FAILS AT RUNTIME. This project has no
// net8.0 target, so a net8.0 app loads its netstandard2.0 build against Core's *net8.0* build - in
// which the polyfill is compiled out. Every attribute usage baked in here then points at a type
// that does not exist, and the first thing to reflect over those members throws
// TypeLoadException: 'System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute' from assembly
// 'protobuf-net.Core'. RuntimeTypeModel.FindOrAddAuto does exactly that, so it is every consumer's
// first serialize rather than an edge case.
//
// Note protobuf-net.BuildTools and .Legacy compile these sources in, so they get one copy from
// here; if protobuf-net.Core ever needs its own (gap B51 stage 4) it must be Compile Removed from
// one of the two in those projects, or it is CS0101 there.
#if !NETSTANDARD2_1_OR_GREATER && !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
    /// <remarks>
    /// This is what protogen emits onto a generated property whose getter substitutes a default -
    /// <c>get =&gt; __pbn__X ?? ""</c> never returns null, but assigning null is exactly how the
    /// member is UNSET, so the two halves genuinely differ.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute
    {
    }

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

#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies that the method or property will ensure that the listed field and property members have non-null values when returning with the specified return value condition.</summary>
    /// <remarks>
    /// This is what protogen emits onto a generated <c>ShouldSerializeX()</c>, so
    /// <c>Descriptor.cs</c> - our own protogen output, compiled into this repository - needs it on
    /// every target below .NET 5. A CONSUMER regenerating their own DTOs below .NET 5 needs their
    /// own copy; the generated file says so in a comment, and the compiler matches these by name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
        public MemberNotNullWhenAttribute(bool returnValue, string member)
        {
            ReturnValue = returnValue;
            Members = new[] { member };
        }

        /// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }
}
#endif
