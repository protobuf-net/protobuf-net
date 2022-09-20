using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGenSemantic.Abstractions;
using ProtoBuf.Internal.CodeGenSemantic.Models;
using ProtoBuf.Internal.CodeGenSemantic.Parsers.Common;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic.Parsers;

internal sealed class EnumPropertyCodeGenModelParser : PropertyCodeGenModelParserBase<CodeGenEnum>
{
    public override CodeGenEnum Parse(IPropertySymbol symbol, NamespaceParseContext parseContext)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            var (_, originalName) = GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenEnum(symbol.Name, symbol.GetFullyQualifiedPrefix())
            {
                OriginalName = originalName,
                Type = symbol.GetCodeGenType()
            };
        
            return codeGenField;
        }

        // throw exception here ?
        return null;
    }
}