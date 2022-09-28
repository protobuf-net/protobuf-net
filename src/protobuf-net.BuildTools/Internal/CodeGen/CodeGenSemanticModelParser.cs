using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen;

internal class CodeGenSemanticModelParser
{
    //public static CodeGenSet Parse(ISymbol symbol)
    //{
    //    var codeGenSet = new CodeGenSet();
    //    return Parse(codeGenSet, symbol);
    //}

    private readonly SymbolCodeGenModelParserProvider symbolCodeGenModelParserProvider = new SymbolCodeGenModelParserProvider();
    private readonly CodeGenSet set = new CodeGenSet();
    private CodeGenFile? defaultFile;
    private CodeGenFile DefaultFile
    {
        get
        {
            if (defaultFile is null)
            {
                defaultFile = new CodeGenFile("protogen.generated.cs");
                set.Files.Add(defaultFile);
            }
            return defaultFile;
        }
    }

    public int Parse(Compilation compilation, SyntaxTree syntaxTree)
    {
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        int count = 0;
        foreach (var symbol in semanticModel.LookupNamespacesAndTypes(0))
        {
            count += ParseAll(symbol, DefaultFile);
        }
        return count;
    }
    private int ParseAll(ISymbol symbol, CodeGenFile file)
    {
        int count = 0;
        if (symbol is INamedTypeSymbol type)
        {
            if (Parse(type, file)) count++;
        }
        if (symbol is INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                count += ParseAll(member, file);
            }
        }
        return count;
    }

    public bool Parse(INamedTypeSymbol type) => Parse(type, DefaultFile);

    private bool Parse(INamedTypeSymbol type, CodeGenFile file)
    {
        var namespaceParser = symbolCodeGenModelParserProvider.GetNamedTypeParser();
        switch (namespaceParser.Parse(type))
        {
            case CodeGenMessage msg:
                file.Messages.Add(msg);
                return true;
            case CodeGenEnum enm:
                file.Enums.Add(enm);
                return true;
            case CodeGenService svc:
                file.Services.Add(svc);
                return true;
        }
        return false;
    }
    public CodeGenSet Process()
    {
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