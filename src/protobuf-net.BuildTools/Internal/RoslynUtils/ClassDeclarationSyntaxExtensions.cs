using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal.RoslynUtils;

internal static class ClassDeclarationSyntaxExtensions
{
    public static bool ContainsAttribute<TAttribute>(this ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation)
        where TAttribute : Attribute
    {
        var attributeLists = classDeclarationSyntax.AttributeLists;
        if (!attributeLists.Any()) return false;

        foreach (var attributeList in attributeLists)
        {
            if (attributeList.Attributes.Any()) continue;
            foreach (var attributeSyntax in attributeList.Attributes)
            {
                // var attributeData = attributeSyntax. Get AttributeData to check type
            }
        }

        return false;
    }
}