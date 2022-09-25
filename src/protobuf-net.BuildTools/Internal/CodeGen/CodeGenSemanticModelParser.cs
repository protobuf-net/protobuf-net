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
        
        // note: if message/enum type is consumed before it is defined, we simplify things
        // by using a place-holder initially (via the protobuf FQN); we need to go back over the
        // tree, and substitute out any such place-holders for the final types
        symbolCodeGenModelParserProvider.ParseContext.FixupPlaceholders();
        
        // throwing errors, which happened during parsing
        // warnings will be available later for logging output
        symbolCodeGenModelParserProvider.ErrorContainer.Throw();
        
        set.ErrorContainer = symbolCodeGenModelParserProvider.ErrorContainer;
        return set;
    }
}