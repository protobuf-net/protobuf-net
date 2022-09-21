using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen;

internal static class CodeGenSemanticModelParser
{
    public static CodeGenSet Parse(ISymbol symbol)
    {
        var codeGenSet = new CodeGenSet();
        return Parse(codeGenSet, symbol);
    }

    public static CodeGenSet Parse(CodeGenSet set, ISymbol symbol)
    {
        var symbolCodeGenModelParserProvider = new SymbolCodeGenModelParserProvider();

        if (symbol is INamespaceSymbol namespaceSymbol)
        {
            var namespaceParser = symbolCodeGenModelParserProvider.GetNamespaceParser();
            var codeGenFile = namespaceParser.Parse(namespaceSymbol);
            
            set.Files.Add(codeGenFile);
        }

        return set;
    }
}