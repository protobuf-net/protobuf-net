#nullable enable
using Microsoft.CodeAnalysis;

namespace ProtoBuf.Internal.RoslynUtils;

internal static class SyntaxNodeExtensions
{
    public static ISymbol? GetDeclaredSymbol(this SyntaxNode node, Compilation compilation)
    {
        var model = compilation.GetSemanticModel(node.SyntaxTree);
        return model.GetDeclaredSymbol(node);
    }
}