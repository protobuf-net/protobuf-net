#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        /// <summary>
        /// A type's fully-qualified name as it has to appear in generated code, honouring any
        /// <c>extern alias</c> on the assembly that declares it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two referenced assemblies may declare the same full type name. C# has exactly one way to
        /// tell them apart — an <c>extern alias</c>, set as <c>&lt;Aliases&gt;</c> metadata on the
        /// reference in the consumer's project — and <c>global::</c> then means "the un-aliased one".
        /// So a contract living in an aliased assembly cannot be named with <c>global::</c> at all,
        /// and <see cref="SymbolDisplayFormat.FullyQualifiedFormat"/> always produces exactly that.
        /// </para>
        /// <para>
        /// The substitution is per *constituent* type rather than on the leading prefix, because a
        /// single name can span assemblies: <c>List&lt;Thing&gt;</c> renders as
        /// <c>global::System.Collections.Generic.List&lt;global::Ns.Thing&gt;</c>, and it is the
        /// inner one that may need the alias. Longest name first, so a nested type is rewritten
        /// before its container and the two cannot overlap.
        /// </para>
        /// <para>
        /// A generator cannot *create* an alias — that is a property of the reference, and only the
        /// consumer's project file can set it — but it can read one and it can emit
        /// <c>extern alias</c> in its own file. Both were probed rather than assumed;
        /// <c>ExternAliasTests</c> pins them.
        /// </para>
        /// </remarks>
        internal static string Qualified(Compilation compilation, ITypeSymbol type)
        {
            var text = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // the overwhelmingly common case: nothing in the compilation is aliased, so there is
            // nothing to rewrite and no reason to walk the type
            if (!HasAliases(compilation)) return text;

            var rewrites = new List<KeyValuePair<string, string>>();
            foreach (var part in Constituents(type))
            {
                if (AliasOf(compilation, part.ContainingAssembly) is not { } alias) continue;
                var bare = BareName(part);
                rewrites.Add(new KeyValuePair<string, string>("global::" + bare, alias + "::" + bare));
            }

            foreach (var rewrite in rewrites.OrderByDescending(static x => x.Key.Length))
            {
                text = text.Replace(rewrite.Key, rewrite.Value);
            }
            return text;
        }

        /// <summary>Whether any reference in this compilation carries an alias at all.</summary>
        private static bool HasAliases(Compilation compilation)
        {
            foreach (var reference in compilation.References)
            {
                if (!reference.Properties.Aliases.IsDefaultOrEmpty) return true;
            }
            return false;
        }

        /// <summary>Every alias declared on any reference — what the emitter declares up front.</summary>
        internal static IEnumerable<string> DeclaredAliases(Compilation compilation)
            => compilation.References
                .SelectMany(static x => x.Properties.Aliases.IsDefault
                    ? Enumerable.Empty<string>() : x.Properties.Aliases)
                .Distinct(System.StringComparer.Ordinal)
                .OrderBy(static x => x, System.StringComparer.Ordinal);

        /// <summary>The alias to use for a type from this assembly, or null for <c>global::</c>.</summary>
        private static string? AliasOf(Compilation compilation, IAssemblySymbol? assembly)
        {
            if (assembly is null) return null;
            var reference = compilation.GetMetadataReference(assembly);
            var aliases = reference?.Properties.Aliases ?? default;
            return aliases.IsDefaultOrEmpty ? null : aliases[0];
        }

        /// <summary>
        /// The named types a display string is built from: the type itself, its containing types,
        /// its type arguments, and the element types of arrays and pointers — recursively.
        /// </summary>
        private static IEnumerable<INamedTypeSymbol> Constituents(ITypeSymbol type)
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    foreach (var part in Constituents(array.ElementType)) yield return part;
                    break;
                case IPointerTypeSymbol pointer:
                    foreach (var part in Constituents(pointer.PointedAtType)) yield return part;
                    break;
                case INamedTypeSymbol named:
                    yield return named;
                    for (var outer = named.ContainingType; outer is not null; outer = outer.ContainingType)
                    {
                        yield return outer;
                    }
                    foreach (var argument in named.TypeArguments)
                    {
                        foreach (var part in Constituents(argument)) yield return part;
                    }
                    break;
            }
        }

        /// <summary>The namespace-qualified name with no type arguments, as it appears in a display string.</summary>
        private static string BareName(INamedTypeSymbol type)
        {
            var names = new List<string>();
            for (var current = type; current is not null; current = current.ContainingType)
            {
                names.Insert(0, current.Name);
            }
            var ns = type.ContainingNamespace;
            if (ns is { IsGlobalNamespace: false })
            {
                names.Insert(0, ns.ToDisplayString());
            }
            return string.Join(".", names);
        }

        /// <summary>
        /// Whether C# has no way at all to name this type, which is the case the consumer has to fix.
        /// </summary>
        /// <remarks>
        /// If two references declare the name and neither is aliased, there is no syntax that
        /// selects one — this is a limitation of C#, not of the generator, and emitting anything at
        /// all would produce CS0433 in a file the consumer did not write. Saying so, and naming the
        /// fix, is the whole value: <c>&lt;Aliases&gt;</c> is not widely known.
        ///
        /// Aliasing one of them is enough, since <c>global::</c> then unambiguously means the other.
        /// </remarks>
        internal static bool CannotBeNamed(Compilation compilation, INamedTypeSymbol type, out string detail)
        {
            detail = "";
            if (!IsAmbiguous(compilation, type, out var candidates)) return false;

            // ours is aliased, so we can name it outright
            if (AliasOf(compilation, type.ContainingAssembly) is not null) return false;

            // ...otherwise global:: works only if ours is the sole *un-aliased* candidate
            var unaliased = candidates
                .Where(x => AliasOf(compilation, x.ContainingAssembly) is null)
                .ToList();
            if (unaliased.Count == 1 && SymbolEqualityComparer.Default.Equals(unaliased[0], type))
            {
                return false;
            }

            var assemblies = string.Join("' and '", candidates
                .Select(static x => x.ContainingAssembly?.Name ?? "?")
                .Distinct(System.StringComparer.Ordinal)
                .OrderBy(static x => x, System.StringComparer.Ordinal));
            detail = $"its name is declared by '{assemblies}', and none of them is aliased, so C# "
                + "cannot name it; add <Aliases> metadata to one of those references (an extern alias) "
                + "to disambiguate";
            return true;
        }

        /// <summary>Whether more than one referenced assembly declares this type's name.</summary>
        private static bool IsAmbiguous(Compilation compilation, INamedTypeSymbol type,
            out IReadOnlyList<INamedTypeSymbol> candidates)
        {
            var found = compilation.GetTypesByMetadataName(FullMetadataName(type));
            candidates = found;
            return found.Length > 1;
        }

        /// <summary>
        /// The metadata name, which nests with <c>+</c> rather than <c>.</c>.
        /// </summary>
        /// <remarks>
        /// Distinct from the <c>MetadataName</c> in the parse file, which is deliberately built from
        /// <c>OriginalDefinition</c> to match the repeated-provider table's keys and does not spell
        /// nesting — right for that job, wrong for asking "who else declares this exact type".
        /// </remarks>
        private static string FullMetadataName(INamedTypeSymbol type)
        {
            var names = new List<string>();
            for (var current = type; current is not null; current = current.ContainingType)
            {
                names.Insert(0, current.MetadataName);
            }
            var ns = type.ContainingNamespace;
            var prefix = ns is { IsGlobalNamespace: false } ? ns.ToDisplayString() + "." : "";
            return prefix + string.Join("+", names);
        }
    }
}
