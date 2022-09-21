using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers.Common;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class EnumPropertyCodeGenModelParser : PropertyCodeGenModelParserBase<CodeGenEnum>
{
    public EnumPropertyCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }
    
    public override CodeGenEnum Parse(IPropertySymbol symbol)
    {
        var propertyAttributes = symbol.GetAttributes();
        if (IsProtoMember(propertyAttributes, out var protoMemberAttribute))
        {
            var fullyQualifiedPrefix = symbol.GetFullyQualifiedPrefix();
            var fullyQualifiedName = fullyQualifiedPrefix + symbol.Name;

            var (_, originalName) = GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenEnum(symbol.Name, fullyQualifiedPrefix)
            {
                OriginalName = originalName,
                Type = ParseContext.GetContractType(fullyQualifiedName)
            };
        
            return codeGenField;
        }

        // throw exception here ?
        return null;
    }
}