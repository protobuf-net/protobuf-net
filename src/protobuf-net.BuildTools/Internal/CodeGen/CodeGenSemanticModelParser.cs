#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen;

internal class CodeGenSemanticModelParser
{
    //public static CodeGenSet Parse(ISymbol symbol)
    //{
    //    var codeGenSet = new CodeGenSet();
    //    return Parse(codeGenSet, symbol);
    //}
    private readonly CodeGenParseContext codeGenParseContext = new CodeGenParseContext();
    private readonly CodeGenSet set = new CodeGenSet();
    private CodeGenFile? defaultFile;
    private CodeGenFile DefaultFile
    {
        get
        {
            if (defaultFile is null)
            {
                defaultFile = new CodeGenFile("protogen.generated.cs");
                _defaultContext = new CodeGenFileParseContext(defaultFile, codeGenParseContext);
                set.Files.Add(defaultFile);
            }
            return defaultFile;
        }
    }

    private CodeGenFileParseContext _defaultContext;
    public ref readonly CodeGenFileParseContext DefaultContext
    {
        get
        {
            if (defaultFile is null) _ = DefaultFile;
            return ref _defaultContext;
        }
    }

    public int Parse(Compilation compilation, SyntaxTree syntaxTree)
    {
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        int count = 0;
        var file = new CodeGenFile(syntaxTree.FilePath);
        set.Files.Add(file);
        var ctx = new CodeGenFileParseContext(file, codeGenParseContext);
        foreach (var symbol in semanticModel.LookupNamespacesAndTypes(0))
        {
            bool fromTree = false;
            foreach (var src in symbol.DeclaringSyntaxReferences)
            {
                if (src.SyntaxTree == syntaxTree)
                {
                    fromTree = true;
                    break;
                }
            }
            if (fromTree)
            {
                count += ParseAll(symbol, in ctx);
            }
        }
        return count;
    }
    private int ParseAll(ISymbol symbol, in CodeGenFileParseContext ctx)
    {
        int count = 0;
        if (symbol is INamedTypeSymbol type)
        {
            if (Parse(type, in ctx)) count++;
        }
        if (symbol is INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                count += ParseAll(member, in ctx);
            }
        }
        return count;
    }

    public bool Parse(INamedTypeSymbol type) => Parse(type, in DefaultContext);

    private bool Parse(INamedTypeSymbol type, in CodeGenFileParseContext ctx)
    {
        switch (ParseUtils.ParseNamedType(in ctx, type))
        {
            case CodeGenMessage msg:
                ctx.File.Messages.Add(msg);
                return true;
            case CodeGenEnum enm:
                ctx.File.Enums.Add(enm);
                return true;
            case CodeGenService svc:
                ctx.File.Services.Add(svc);
                return true;
        }
        return false;
    }
    public CodeGenSet Process()
    {
        // note: if message/enum type is consumed before it is defined, we simplify things
        // by using a place-holder initially (via the protobuf FQN); we need to go back over the
        // tree, and substitute out any such place-holders for the final types
        codeGenParseContext.FixupPlaceholders();

        //// throwing errors, which happened during parsing
        //// warnings will be available later for logging output
        //symbolCodeGenModelParserProvider.ErrorContainer.Throw();

        //set.ErrorContainer = symbolCodeGenModelParserProvider.ErrorContainer;
        return set;
    }
}