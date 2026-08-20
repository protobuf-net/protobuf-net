#nullable enable
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProtoBuf.BuildTools.Internal.Grpc
{
    /// <summary>
    /// Reproduces, over Roslyn symbols, the list that
    /// <c>ServiceBinder.GetMetadata(MethodInfo, Type, Type)</c> computes reflectively at run time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is semantic, not incidental: the consumer treats <em>later as higher priority</em>, so
    /// reproducing the set without the order would produce endpoints that differ only when two
    /// attributes disagree - which is exactly the case that matters for authorization.
    /// </para>
    /// <para>
    /// This targets the behaviour on protobuf-net.Grpc's <c>main</c> - i.e. including
    /// <see href="https://github.com/protobuf-net/protobuf-net.Grpc/pull/369">#369</see>, which fixed
    /// implementation-side attributes being missed for an operation declared on a <c>[SubService]</c>
    /// base. That fix is in no released package; <c>src/AotGrpcMetadataDiff</c> compares against the
    /// pinned one and reports the difference rather than hiding it.
    /// </para>
    /// </remarks>
    internal static class MetadataGather
    {
        /// <summary>
        /// Whether the referenced protobuf-net.Grpc resolves an inherited operation's implementation
        /// method - i.e. whether it carries
        /// <see href="https://github.com/protobuf-net/protobuf-net.Grpc/pull/369">#369</see>, which is
        /// everything after 1.3.6.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read off <c>AssemblyFileVersionAttribute</c> rather than the assembly identity, and that is
        /// not a preference: Nerdbank.GitVersioning pins <c>AssemblyVersion</c> to <c>1.0.0.0</c> so
        /// references keep binding across releases, and puts the real number on the file and
        /// informational versions. Identity reports <c>1.0.0.0</c> for every release ever shipped.
        /// </para>
        /// <para>
        /// Only the first three components are the version: NBGV puts the git height in the fourth, so
        /// 1.3.6 arrives as <c>1.3.6.5978</c> and a plain <see cref="Version"/> comparison against
        /// <c>1.3.6</c> would call it newer than itself.
        /// </para>
        /// <para>
        /// Where no version can be read - an assembly built without NBGV, or the stubs the unit-test
        /// harnesses declare - this answers <c>true</c>, matching current protobuf-net.Grpc rather than
        /// a release that is already behind.
        /// </para>
        /// </remarks>
        public static bool ResolvesInheritedImplementation(Compilation compilation)
        {
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly) continue;
                if (assembly.Name != "protobuf-net.Grpc") continue;

                foreach (var attribute in assembly.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString()
                        != "System.Reflection.AssemblyFileVersionAttribute")
                    {
                        continue;
                    }
                    if (attribute.ConstructorArguments.Length == 1
                        && attribute.ConstructorArguments[0].Value is string text
                        && Version.TryParse(text, out var version))
                    {
                        var release = new Version(version.Major, version.Minor,
                            version.Build < 0 ? 0 : version.Build);
                        return release > new Version(1, 3, 6);
                    }
                }
                return true;
            }
            return true;
        }

        /// <summary>
        /// Gathers endpoint metadata for one operation.
        /// </summary>
        /// <param name="contractType">The <em>top-level</em> contract, as passed to <c>GetMetadata</c>.</param>
        /// <param name="contractMethod">
        /// The operation, on its <em>declaring</em> interface - which for a <c>[SubService]</c> base is
        /// not <paramref name="contractType"/>, and is what the emitted binding already passes.
        /// </param>
        /// <param name="serviceType">The implementing class, or null when there is none.</param>
        /// <param name="resolveInheritedImplementation">
        /// <para>
        /// When false, an operation whose declaring interface is not <paramref name="contractType"/>
        /// contributes no implementation-method attributes - which is what protobuf-net.Grpc did
        /// <em>before</em> #369, since the interface map was keyed on the top-level contract and the
        /// lookup simply failed.
        /// </para>
        /// <para>
        /// Present so the oracle can compare against the pinned package, which predates that fix, and
        /// report the difference rather than hide it. Emit uses the default.
        /// </para>
        /// </param>
        public static List<AttributeData> Gather(INamedTypeSymbol contractType,
            IMethodSymbol contractMethod, INamedTypeSymbol? serviceType,
            bool resolveInheritedImplementation = true)
        {
            // GetMetadata builds [serviceMethod, serviceType, contractMethod, contractType] and then
            // reverses the whole thing, so the result is each group reversed, in the opposite order.
            List<AttributeData>
                contractTypeAtt = TypeAttributes(contractType),
                contractMethodAtt = MethodAttributes(contractMethod),
                serviceTypeAtt = new List<AttributeData>(),
                serviceMethodAtt = new List<AttributeData>();

            if (serviceType is not null
                && !SymbolEqualityComparer.Default.Equals(contractType, serviceType)
                && contractType.TypeKind == TypeKind.Interface
                && serviceType.TypeKind == TypeKind.Class)
            {
                serviceTypeAtt = TypeAttributes(serviceType);

                // #369: the interface map is keyed on the method's *declaring* type, so an operation
                // inherited from a [SubService] base resolves. FindImplementationForInterfaceMember is
                // keyed the same way, so asking it with the declaring interface's member is equivalent.
                var inherited = !SymbolEqualityComparer.Default.Equals(contractMethod.ContainingType,
                    contractType);
                if ((resolveInheritedImplementation || !inherited)
                    && serviceType.FindImplementationForInterfaceMember(contractMethod) is IMethodSymbol impl)
                {
                    serviceMethodAtt = MethodAttributes(impl);
                }
            }

            var result = new List<AttributeData>(contractTypeAtt.Count + contractMethodAtt.Count
                + serviceTypeAtt.Count + serviceMethodAtt.Count);
            AddReversed(result, contractTypeAtt);
            AddReversed(result, contractMethodAtt);
            AddReversed(result, serviceTypeAtt);
            AddReversed(result, serviceMethodAtt);
            return result;
        }

        private static void AddReversed(List<AttributeData> into, List<AttributeData> from)
        {
            for (int i = from.Count - 1; i >= 0; i--) into.Add(from[i]);
        }

        /// <summary>
        /// <c>Type.GetCustomAttributes(inherit: true)</c>, most-derived first.
        /// </summary>
        /// <remarks>
        /// This walks base <em>classes</em> but not base <em>interfaces</em>, which is why a
        /// <c>[SubService]</c> interface's own type-level attributes are not collected for the contract
        /// that inherits it - existing semantics, and not something #369 changed.
        /// </remarks>
        private static List<AttributeData> TypeAttributes(INamedTypeSymbol type)
        {
            var result = new List<AttributeData>();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            var declared = true;
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                Add(result, seen, current.GetAttributes(), inherited: !declared);
                if (type.TypeKind == TypeKind.Interface) break;
                declared = false;
            }
            return result;
        }

        /// <summary>
        /// <c>MethodInfo.GetCustomAttributes(inherit: true)</c>, most-derived first: the method's own,
        /// then anything it overrides.
        /// </summary>
        private static List<AttributeData> MethodAttributes(IMethodSymbol method)
        {
            var result = new List<AttributeData>();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            var declared = true;
            for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
            {
                Add(result, seen, current.GetAttributes(), inherited: !declared);
                declared = false;
            }
            return result;
        }

        /// <summary>
        /// Appends one level of the chain, honouring the two <c>[AttributeUsage]</c> rules reflection
        /// applies: <c>Inherited = false</c> opts a base's attribute out, and a non-<c>AllowMultiple</c>
        /// attribute is deduplicated most-derived-wins.
        /// </summary>
        /// <remarks>
        /// The <c>Inherited</c> rule is load-bearing for <em>every</em> class-implemented endpoint, not
        /// just for types that happen to use it: the class walk ends at <see cref="object"/>, which
        /// carries <c>[TypeForwardedFrom]</c>, <c>[ComVisible]</c> and <c>[ClassInterface]</c>, and it is
        /// only their <c>Inherited = false</c> that keeps all three out of the metadata. Established by
        /// disabling this test in the oracle and reading what appeared.
        /// </remarks>
        private static void Add(List<AttributeData> into, HashSet<string> seen,
            ImmutableArray<AttributeData> attributes, bool inherited)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass is not INamedTypeSymbol type) continue;

                GetUsage(type, out var allowMultiple, out var isInherited);
                if (inherited && !isInherited) continue;

                // keyed on the attribute type: reflection deduplicates on type, not on arguments
                if (!allowMultiple && !seen.Add(type.ToDisplayString())) continue;

                into.Add(attribute);
            }
        }

        /// <summary>
        /// Reads <c>[AttributeUsage]</c> off an attribute type, with the framework defaults
        /// (<c>Inherited = true</c>, <c>AllowMultiple = false</c>) where it says nothing.
        /// </summary>
        /// <remarks>
        /// <c>[AttributeUsage]</c> is itself inherited, so a base attribute class supplies it - which is
        /// how the usage declared on <c>[Authorize]</c> reaches an attribute deriving from it.
        /// </remarks>
        private static void GetUsage(INamedTypeSymbol attributeType, out bool allowMultiple,
            out bool inherited)
        {
            allowMultiple = false;
            inherited = true;
            for (INamedTypeSymbol? current = attributeType; current is not null; current = current.BaseType)
            {
                foreach (var usage in current.GetAttributes())
                {
                    if (usage.AttributeClass?.ToDisplayString() != "System.AttributeUsageAttribute") continue;

                    foreach (var named in usage.NamedArguments)
                    {
                        switch (named.Key)
                        {
                            case nameof(System.AttributeUsageAttribute.AllowMultiple):
                                if (named.Value.Value is bool multiple) allowMultiple = multiple;
                                break;
                            case nameof(System.AttributeUsageAttribute.Inherited):
                                if (named.Value.Value is bool inherit) inherited = inherit;
                                break;
                        }
                    }
                    return;
                }
            }
        }
    }
}
