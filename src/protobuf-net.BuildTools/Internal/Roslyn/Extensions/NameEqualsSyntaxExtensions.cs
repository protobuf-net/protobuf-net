using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal.Roslyn.Extensions
{
    internal static class NameEqualsSyntaxExtensions
    {
        public static bool EqualsByIdentifierText(this NameEqualsSyntax syntax, NameEqualsSyntax other)
        {
            if (syntax is null) return false;
            if (other is null) return false;

            return string.Equals(syntax.Name.Identifier.Text, other.Name.Identifier.Text);
        }
    }
}