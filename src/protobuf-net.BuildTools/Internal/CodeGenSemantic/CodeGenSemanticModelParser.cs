using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGenSemantic.Models;
using ProtoBuf.Internal.CodeGenSemantic.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic;

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
            var namespaceParser = SymbolCodeGenModelParserProvider.GetNamespaceParser();
            var parseContext = new NamespaceParseContext();
            var codeGenFile = namespaceParser.Parse(namespaceSymbol, parseContext);
            
            set.Files.Add(codeGenFile);
        }

        return set;
    }
}