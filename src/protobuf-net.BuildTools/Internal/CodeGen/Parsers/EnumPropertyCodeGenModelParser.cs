using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Parsers.Common;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class EnumPropertyCodeGenModelParser : PropertyCodeGenModelParserBase<CodeGenEnum>
{
    public override CodeGenEnum Parse(IPropertySymbol symbol, NamespaceParseContext parseContext)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            var (_, originalName, dataFormat) = GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenEnum(symbol.Name, symbol.GetFullyQualifiedPrefix())
            {
                OriginalName = originalName,
                Type = symbol.GetCodeGenType(dataFormat),
            };
        
            return codeGenField;
        }

        // throw exception here ?
        return null;
    }
}