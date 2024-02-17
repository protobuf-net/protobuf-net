#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser
{
    public CodeGenEnumValue ParseEnumValue(IFieldSymbol symbol)
    {
        var codeGenEnumValue = new CodeGenEnumValue(symbol.GetConstantValue(), symbol.Name, symbol);
        Configure(symbol, codeGenEnumValue);
        return codeGenEnumValue;
    }

    private void Configure(IFieldSymbol symbol, CodeGenEnumValue enm)
    {
        foreach (var attrib in symbol.GetAttributes())
        {
            var ac = attrib.AttributeClass;
            if (ac is null) continue;

            switch (ac.Name)
            {
                case nameof(ProtoEnumAttribute) when ac.InProtoBufNamespace():
                    ParseAttribute(symbol, attrib, enm, TryParseProtoEnumAttribute);
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

        static bool TryParseProtoEnumAttribute(string name, bool ctor, CodeGenEnumValue obj, TypedConstant value)
        {
            switch (name)
            {
                case "name":
                case nameof(ProtoEnumAttribute.Name):
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