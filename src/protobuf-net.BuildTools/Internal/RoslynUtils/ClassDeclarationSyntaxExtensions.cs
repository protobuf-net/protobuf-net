using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal.RoslynUtils;

internal static class ClassDeclarationSyntaxExtensions
{
    public static bool TryParseClassNamespaceName(this ClassDeclarationSyntax classSyntax, out string @namespace)
    {
        var namespaceSyntax = classSyntax.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if (namespaceSyntax is not null)
        {
            @namespace = namespaceSyntax.Name.ToString();
            return true;
        }

        @namespace = string.Empty;
        return false;
    }

    public static bool ContainsAttribute(this ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation, Type attribute)
    {
        var attributeLists = classDeclarationSyntax.AttributeLists;
        if (!attributeLists.Any()) return false;
        
        var attributeDatas = attributeLists.First().GetAttributeDatas(compilation);
        var searchedAttributeDefined = attributeDatas.FirstOrDefault(data => data.AttributeClass!.MetadataName == attribute.Name);
        if (searchedAttributeDefined is not null) return true;

        return false;
    }

    public static ICollection<AttributeSyntax> GetAttributeSyntaxesOfType(this ClassDeclarationSyntax classDeclarationSyntax, Type searchedAttributeType)
    {
        var attributeLists = classDeclarationSyntax.AttributeLists;
        if (!attributeLists.Any()) return Array.Empty<AttributeSyntax>();

        var result = new List<AttributeSyntax>();
        foreach (var attributeList in attributeLists.Where(attributeList => attributeList.Attributes.Any()))
        {
            foreach (var attr in attributeList.Attributes)
            {
                if (attr.Name.Arity == 0)
                {
                    if (attr.Name.ToString() == searchedAttributeType.Name)
                        result.Add(attr);
                }
                else if (attr.Name.Arity == 1)
                {
                    var attributeUsageName = attr.Name.ToString();
                    var searchedAttributeName = searchedAttributeType.Name;

                    if (attributeUsageName.Substring(0, attributeUsageName.IndexOf('<')) + "Attribute" == searchedAttributeName.Substring(0, searchedAttributeName.IndexOf('`')))
                        result.Add(attr);
                }
            }
        }

        return result;
    }

    public static ICollection<AttributeData> GetAttributesOfType(this ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation, Type attribute)
    {
        var attributeLists = classDeclarationSyntax.AttributeLists;
        if (!attributeLists.Any()) return Array.Empty<AttributeData>();
        
        var allAttributeDatas = attributeLists.First().GetAttributeDatas(compilation);
        return allAttributeDatas.Where(data => data.AttributeClass!.Name + "Attribute" == attribute.Name).ToList();
    }
}