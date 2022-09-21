using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Parsers.Common;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class FieldPropertyCodeGenModelParser : PropertyCodeGenModelParserBase<CodeGenField>
{
    public override CodeGenField Parse(IPropertySymbol symbol, NamespaceParseContext parseContext)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            var (fieldNumber, originalName) = GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenField(fieldNumber, symbol.Name)
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