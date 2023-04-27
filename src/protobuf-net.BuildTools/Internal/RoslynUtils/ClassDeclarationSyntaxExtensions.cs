using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal.RoslynUtils;

internal static class ClassDeclarationSyntaxExtensions
{
    public static bool ContainsAttribute(this ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation, Type attribute)
    {
        var attributeLists = classDeclarationSyntax.AttributeLists;
        if (!attributeLists.Any()) return false;

        foreach (var attributeList in attributeLists)
        {
            if (!attributeList.Attributes.Any()) continue;
            var attributeDatas = attributeList.GetAttributeDatas(compilation);
            var protoUnionAttributeDefined = attributeDatas.FirstOrDefault(data => data.AttributeClass!.Name + "Attribute" == attribute.Name);
            if (protoUnionAttributeDefined is not null) return true;
        }

        return false;
    }

}