#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser
{
    public CodeGenType ParseEnum(INamedTypeSymbol symbol)
    {
        var codeGenEnum = new CodeGenEnum(symbol.Name, symbol.GetFullyQualifiedPrefix(), symbol)
        {
            Emit = CodeGenGenerate.None, // nothing to emit
            Type = symbol.EnumUnderlyingType.TryResolveKnownCodeGenType(DataFormat.Default) ?? CodeGenType.Unknown,
        };
        Configure(symbol, codeGenEnum);
        Add(symbol, codeGenEnum);

        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol is IFieldSymbol fieldSymbol)
            {
                var parsedEnumValue = ParseEnumValue(fieldSymbol);
                if (parsedEnumValue is not null)
                {
                    codeGenEnum.EnumValues.Add(parsedEnumValue);
                }
            }
        }
        return codeGenEnum;
    }

    private void Configure(INamedTypeSymbol symbol, CodeGenEnum enm)
    {
        foreach (var attrib in symbol.GetAttributes())
        {
            var ac = attrib.AttributeClass;
            if (ac is null) continue;

            switch (ac.Name)
            {
                case nameof(ProtoContractAttribute) when ac.InProtoBufNamespace():
                    ParseAttribute(symbol, attrib, enm, TryParseProtoContractAttribute);
                    break;
                case nameof(ObsoleteAttribute) when ac.InNamespace("System"):
                    enm.IsDeprecated = true;
                    break;
                default:
                    if (ac.InProtoBufNamespace())
                    {
                        ReportDiagnostic(ParseUtils.UnhandledAttribute, symbol, ac.Name);
                    }
                    break;
            }
        }

        static bool TryParseProtoContractAttribute(string name, bool ctor, CodeGenEnum obj, TypedConstant value)
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