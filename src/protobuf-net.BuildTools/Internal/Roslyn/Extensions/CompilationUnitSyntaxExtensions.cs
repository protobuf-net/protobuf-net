#nullable enable
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal.Roslyn.Extensions
{
    internal static class CompilationUnitSyntaxExtensions
    {
        public static CompilationUnitSyntax AddUsingsIfNotExist(
            this CompilationUnitSyntax compilationUnitSyntax,
            params string[]? usingDirectiveNames)
        {
            if (usingDirectiveNames is null || usingDirectiveNames.Length == 0) return compilationUnitSyntax;

            // build a hashset for efficient lookup
            // comparison is done based on string value, because different usings can have different types of identifiers:
            // - IdentifierName
            // - QualifiedNameSyntax
            var existingUsingDirectiveNames = compilationUnitSyntax.Usings
                .Select(x => x.Name.ToString().Trim())
                .ToImmutableHashSet();

            foreach (var directive in usingDirectiveNames)
            {
                var directiveTrimmed = directive.Trim();
                if (!existingUsingDirectiveNames.Contains(directiveTrimmed))
                {
                    compilationUnitSyntax = compilationUnitSyntax.AddUsings(
                        SyntaxFactory.UsingDirective(
                            SyntaxFactory.ParseName(" " + directiveTrimmed)));
                }
            }

            return compilationUnitSyntax;
        }
    }
}