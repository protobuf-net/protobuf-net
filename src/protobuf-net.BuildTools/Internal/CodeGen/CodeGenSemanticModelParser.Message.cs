#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser
{
    public CodeGenType ParseMessage(INamedTypeSymbol symbol)
    {
        var codeGenMessage = new CodeGenMessage(symbol.Name, symbol.GetFullyQualifiedPrefix(), symbol)
        {
            Package = symbol.GetFullyQualifiedPrefix(trimFinal: true),
            IsValueType = symbol.IsValueType,
            IsReadOnly = symbol.IsReadOnly,
            Emit = CodeGenGenerate.DataContract | CodeGenGenerate.DataSerializer, // everything else is in the existing code
        };
        Configure(symbol, codeGenMessage);
        Add(symbol, codeGenMessage);
        // add data fields
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IPropertySymbol propertySymbol)
            {
                var codeGenField = ParseField(propertySymbol);
                if (codeGenField is not null)
                {
                    codeGenMessage.Fields.Add(codeGenField);
                }
            }
        }
        return codeGenMessage;
    }

    private void Configure(INamedTypeSymbol symbol, CodeGenMessage msg)
    {
        foreach (var attrib in symbol.GetAttributes())
        {
            var ac = attrib.AttributeClass;
            if (ac is null) continue;

            switch (ac.Name)
            {
                case nameof(ProtoContractAttribute) when ac.InProtoBufNamespace():
                    ParseAttribute(symbol, attrib, msg, TryParseProtoContractAttribute);
                    break;
                case nameof(ObsoleteAttribute) when ac.InNamespace("System"):
                    msg.IsDeprecated = true;
                    break;
                default:
                    if (ac.InProtoBufNamespace())
                    {
                        ReportDiagnostic(ParseUtils.UnhandledAttribute, symbol, ac.Name);
                    }
                    break;
            }
        }

        static bool TryParseProtoContractAttribute(string name, bool ctor, CodeGenMessage obj, TypedConstant value)
        {
            switch (name)
            {
                case "name":
                case nameof(ProtoContractAttribute.Name):
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