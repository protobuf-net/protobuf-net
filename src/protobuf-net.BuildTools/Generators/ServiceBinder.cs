using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Text;

namespace ProtoBuf.BuildTools.Generators;

internal static class ServiceBinder // ported from ServiceBinder in the gRPC bits
{
    public static bool IsServiceContract(INamedTypeSymbol contractType, out string name)
    {
        if (contractType.ImplementsInterface(IsIGrpcService))
        {
            name = contractType.Name;
            return true;
        }

        string? serviceName;
        var attribs = contractType.GetAllAttributes(); // GetAllAttributes === inherited: true
        if (attribs.IsDefined(IsServiceAttribute))
        {
            attribs.TryGetAnyNonWhitespaceString(IsServiceAttribute, "Name", out serviceName);
        }
        else if (attribs.IsDefined(IsServiceContractAttribute))
        {
            attribs.TryGetAnyNonWhitespaceString(IsServiceContractAttribute, "Name", out serviceName);
        }
        else
        {
            name = default;
            return false;
        }
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            serviceName = GetDefaultName(contractType);
        }
        else if (contractType.IsGenericType && serviceName.IndexOf('{') >= 0)
        {
            var parts = new string[contractType.Arity];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = GetDataContractName(contractType.TypeArguments[i]);
            }
            serviceName = string.Format(serviceName, parts);
        }
        name = serviceName;
        return !string.IsNullOrWhiteSpace(name);
    }

    private static string GetDefaultName(INamedTypeSymbol contractType)
    {
        var serviceName = contractType.Name;
        var sb = new StringBuilder();
        RecurseFqn(sb, contractType);

        if (contractType.Arity != 0)
        {
            foreach (var t in contractType.TypeArguments)
            {
                sb.Append("_").Append(GetDataContractName(t));
            }
        }
        return sb.ToString();

        static void RecurseFqn(StringBuilder sb, INamespaceOrTypeSymbol obj, int depth = 0)
        {
            if (obj.ContainingSymbol is INamespaceOrTypeSymbol parent &&
                parent is not INamespaceSymbol { IsGlobalNamespace: true })
            {
                RecurseFqn(sb, parent, depth + 1);
            }
            if (depth == 0)
            {
                if (obj is ITypeSymbol { TypeKind: TypeKind.Interface } && obj.Name.StartsWith("I"))
                {
                    sb.Append(obj.Name.Substring(1));
                }
                else
                {
                    sb.Append(obj.Name);
                }
            }
            else
            {
                sb.Append(obj.Name).Append(".");
            }
        }
    }

    static ImmutableArray<AttributeData> GetAllAttributes(this ITypeSymbol type)
    {
        bool hasBaseType = !(type.BaseType is null || type.BaseType.SpecialType == SpecialType.System_Object);
        var thisLevel = type.GetAttributes();
        if (!hasBaseType) return thisLevel; // nowhere else to look

        if (thisLevel.IsDefaultOrEmpty) return GetAllAttributes(type.BaseType);

        // ok, so: we have attributes *and* there's a base type; might need to merge
        var builder = ImmutableArray.CreateBuilder<AttributeData>();
        Collect(builder, type.BaseType);
        builder.AddRange(thisLevel);
        return builder.ToImmutableArray();

        static void Collect(ImmutableArray<AttributeData>.Builder builder, ITypeSymbol type)
        {
            bool hasBaseType = !(type.BaseType is null || type.BaseType.SpecialType == SpecialType.System_Object);
            if (hasBaseType) Collect(builder, type.BaseType);
            builder.AddRange(type.GetAttributes());
        }

    }

    internal static string GetDataContractName(ITypeSymbol contractType)
    {
        var attribs = contractType.GetAttributes();

        if (attribs.TryGetAnyNonWhitespaceString(IsProtoContractAttribute, "Name", out var name)
            || attribs.TryGetAnyNonWhitespaceString(IsDataContractAttribute, "Name", out name))
        {
            return name;
        }
        return contractType.Name;
    }

    static bool IsDefined(this ImmutableArray<AttributeData> attribs, Func<INamedTypeSymbol, bool> predicate)
    {
        foreach (var attrib in attribs)
        {
            if (attrib.AttributeClass is { } ac && predicate(ac))
            {
                return true;
            }
        }
        return false;
    }

    static bool ImplementsInterface(this INamedTypeSymbol type, Func<INamedTypeSymbol, bool> predicate)
    {
        if (type.TypeKind == TypeKind.Interface && predicate(type)) return true;
        foreach (var iType in type.AllInterfaces)
        {
            if (predicate(iType)) return true;
        }
        return false;
    }

    static bool TryGetAnyNonWhitespaceString(this ImmutableArray<AttributeData> attribs, Func<INamedTypeSymbol, bool> predicate, string member, out string value)
    {
        foreach (var attrib in attribs)
        {
            if (attrib.AttributeClass is { } ac && predicate(ac))
            {
                foreach (var arg in attrib.NamedArguments)
                {
                    if (string.Equals(arg.Key, member, StringComparison.InvariantCultureIgnoreCase))
                    {
                        value = arg.Value.Value as string ?? "";
                        return !string.IsNullOrEmpty(value);
                    }
                }
                int index = 0;
                foreach (var arg in attrib.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty)
                {
                    if (string.Equals(arg.Name, member, StringComparison.InvariantCultureIgnoreCase))
                    {
                        value = attrib.ConstructorArguments[index].Value as string ?? "";
                        return !string.IsNullOrEmpty(value);
                    }
                    index++;
                }
            }
        }

        value = "";
        return false;
    }

    private static readonly Func<INamedTypeSymbol, bool> IsIGrpcService = attrib =>
        attrib is
        {
            Name: "IGrpcService",
            TypeKind: TypeKind.Interface,
            ContainingType: null,
            IsGenericType: false,
            ContainingNamespace:
            {
                Name: "Configuration",
                ContainingNamespace:
                {
                    Name: "Grpc",
                    ContainingNamespace:
                    {
                        Name: "ProtoBuf",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            }
        };

    private static readonly Func<INamedTypeSymbol, bool> IsServiceAttribute = attrib =>
        attrib is
        {
            Name: "ServiceAttribute",
            TypeKind: TypeKind.Class,
            ContainingType: null,
            IsGenericType: false,
            ContainingNamespace:
            {
                Name: "Configuration",
                ContainingNamespace:
                {
                    Name: "Grpc",
                    ContainingNamespace:
                    {
                        Name: "ProtoBuf",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            }
        };

    private static readonly Func<INamedTypeSymbol, bool> IsServiceContractAttribute = attrib =>
        attrib is
        {
            Name: "ServiceContractAttribute",
            TypeKind: TypeKind.Class,
            ContainingType: null,
            IsGenericType: false,
            ContainingNamespace:
            {
                Name: "ServiceModel",
                ContainingNamespace:
                {
                    Name: "System",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };

    private static readonly Func<INamedTypeSymbol, bool> IsProtoContractAttribute = attrib =>
        attrib is
        {
            Name: "ProtoContractAttribute",
            TypeKind: TypeKind.Class,
            ContainingType: null,
            IsGenericType: false,
            ContainingNamespace:
            {
                Name: "ProtoBuf",
                ContainingNamespace.IsGlobalNamespace: true
            }
        };

    private static readonly Func<INamedTypeSymbol, bool> IsDataContractAttribute = attrib =>
        attrib is
        {
            Name: "DataContractAttribute",
            TypeKind: TypeKind.Class,
            ContainingType: null,
            IsGenericType: false,
            ContainingNamespace:
            {
                Name: "Serialization",
                ContainingNamespace:
                {
                    Name: "Runtime",
                    ContainingNamespace:
                    {
                        Name: "System",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            }
        };
}
