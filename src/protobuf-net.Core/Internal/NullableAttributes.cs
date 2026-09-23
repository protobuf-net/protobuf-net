// The nullability post-condition attributes, polyfilled for the target frameworks whose reference
// assemblies do not carry them. net462 and netstandard2.0 have neither; netstandard2.1 has
// NotNullWhen/AllowNull but NOT MemberNotNullWhen, hence the two guards below.
//
// The compiler matches them BY NAME, so an internal copy works exactly as the real thing.
//
// THERE IS A SECOND COPY, in protobuf-net.Reflection, and that duplication is deliberate: a
// polyfill has to live in the assembly that USES it. Sharing one by [InternalsVisibleTo] compiles
// and passes that project's own tests, then fails at RUNTIME - Reflection has no net8.0 target, so
// a net8.0 app loads its netstandard2.0 build against Core's *net8.0* build, where the polyfill is
// compiled out, and the first thing to reflect over the annotated members throws
// TypeLoadException from RuntimeTypeModel.FindOrAddAuto. See gap B51.
//
// The cost of that duplication is paid in protobuf-net.BuildTools and .Legacy, which compile BOTH
// projects' sources in and would therefore see two declarations (CS0101). Each of them Compile
// Removes the Reflection copy and keeps this one.
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

    /// <summary>Specifies that the output will be non-null if the named parameter is non-null.</summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    internal sealed class NotNullIfNotNullAttribute : Attribute
    {
        public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;
        public string ParameterName { get; }
    }

    /// <summary>Specifies that an output is not null even if the corresponding type allows it.</summary>
    /// <remarks>
    /// On a parameter this is a POST-condition: after the call, the argument is not null. That is
    /// what lets a null-guard helper establish non-nullness for its caller, which a bare
    /// <c>if (x is null) Throw...</c> cannot - testing for null widens the value to maybe-null for
    /// the rest of the method, so the guard destroys exactly what it was meant to prove.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute
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
