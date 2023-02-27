﻿#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal static partial class ParseUtils
{   
    public static CodeGenField? ParseField(in CodeGenFileParseContext ctx, IPropertySymbol symbol)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (ParseUtils.IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            var (fieldNumber, originalName, dataFormat, isRequired) = ParseUtils.GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenField(fieldNumber, symbol.Name, symbol)
            {
                OriginalName = originalName,
                Type = symbol.Type.ResolveCodeGenType(dataFormat, ctx.Context, out var repeated, symbol),
                Repeated = repeated,
                IsRequired = isRequired,
            };

            return codeGenField;
        }

        // throw exception here ?
        return null;
    }
}