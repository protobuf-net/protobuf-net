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
            var (_, originalName, dataFormat, _) = GetProtoMemberAttributeData(protoMemberAttribute);
            var codeGenField = new CodeGenEnum(symbol.Name, symbol.GetFullyQualifiedPrefix())
            {
                OriginalName = originalName,
                Type = symbol.ResolveKnownCodeGenType(dataFormat)
            };
        
            return codeGenField;
        }

        // throw exception here ?
        return null;
    }
}