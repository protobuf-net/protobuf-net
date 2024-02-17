#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.ServiceModel;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser
{

    public CodeGenType ParseService(ITypeSymbol symbol)
    {
        var service = new CodeGenService(symbol.Name, symbol.GetFullyQualifiedPrefix(), symbol)
        {
            Package = symbol.GetFullyQualifiedPrefix(trimFinal: true),
            Emit = CodeGenGenerate.ServiceContract | CodeGenGenerate.ServiceProxy, // everything else is in the existing code
        };
        Configure(symbol, service);
        Add(symbol, service);

        // add operations
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IMethodSymbol methodSymbol)
            {
                var codeGenServiceMethod = ParseOperation(methodSymbol);
                if (codeGenServiceMethod is not null)
                {
                    service.ServiceMethods.Add(codeGenServiceMethod);
                }
            }
        }
        return service;
    }

    private void Configure(ITypeSymbol symbol, CodeGenService svc)
    {
        foreach (var attrib in symbol.GetAttributes())
        {
            var ac = attrib.AttributeClass;
            if (ac is null) continue;

            switch (ac.Name)
            {
                case nameof(ServiceContractAttribute) when ac.InNamespace("System", "ServiceModel"):
                case "ServiceAttribute" when ac.InNamespace("ProtoBuf", "Grpc", "Configuration"):
                    ParseAttribute(symbol, attrib, svc, TryParseServiceAttribute);
                    break;
                case nameof(ObsoleteAttribute) when ac.InNamespace("System"):
                    svc.IsDeprecated = true;
                    break;
                default:
                    if (ac.InProtoBufNamespace() || ac.InNamespace("ProtoBuf", "Grpc", "Configuration"))
                    {
                        ReportDiagnostic(ParseUtils.UnhandledAttribute, symbol, ac.Name);
                    }
                    break;
            }
        }

        static bool TryParseServiceAttribute(string name, bool ctor, CodeGenService obj, TypedConstant value)
        {
            switch (name)
            {
                case "name":
                case nameof(ServiceContractAttribute.Name):
                    if (value.TryGetString(out var s))
                    {
                        obj.OriginalName = s;
                        return true;
                    }
                    break;
            }
            return false;
        }
    }
}