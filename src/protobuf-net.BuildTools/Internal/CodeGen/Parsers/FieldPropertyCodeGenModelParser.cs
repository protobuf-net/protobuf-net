using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Parsers.Common;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class FieldPropertyCodeGenModelParser : PropertyCodeGenModelParserBase<CodeGenField>
{
    public FieldPropertyCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }
    
    public override CodeGenField Parse(IPropertySymbol symbol)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            var (fieldNumber, originalName, dataFormat, isRequired) = GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenField(fieldNumber, symbol.Name)
            {
                OriginalName = originalName,
                Type = symbol.ResolveCodeGenType(dataFormat, ParseContext),
                IsRequired = isRequired,
            };

            return codeGenField;
        }

        // throw exception here ?
        return null;
    }
}