using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal;

namespace ProtoBuf.Internal.RoslynUtils;

internal static class ClassDeclarationSyntaxExtensions
{
    public static bool ContainsAttribute(this ClassDeclarationSyntax classDeclarationSyntax, Compilation compilation, Type attribute)
    {
        var attributeLists = classDeclarationSyntax.AttributeLists;
        if (!attributeLists.Any()) return false;
        
        var attributeDatas = attributeLists.First().GetAttributeDatas(compilation);
        var searchedAttributeDefined = attributeDatas.FirstOrDefault(data => data.AttributeClass!.Name == attribute.Name);
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
                    if (attributeUsageName.Substring(0, attributeUsageName.IndexOf('<')) + "Attribute" == searchedAttributeType.Name)
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