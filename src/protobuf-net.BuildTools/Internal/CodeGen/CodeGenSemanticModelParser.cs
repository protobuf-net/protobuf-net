using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Models;
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
        if (symbol is INamespaceSymbol namespaceSymbol)
        {
            var namespaceParser = SymbolCodeGenModelParserProvider.GetFileParser();
            var parseContext = new NamespaceParseContext();
            var codeGenFile = namespaceParser.Parse(namespaceSymbol, parseContext);
            
            set.Files.Add(codeGenFile);
        }

        return set;
    }
}