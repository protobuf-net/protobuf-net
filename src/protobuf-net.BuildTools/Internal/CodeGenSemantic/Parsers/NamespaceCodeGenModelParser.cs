#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGenSemantic.Abstractions;
using ProtoBuf.Internal.CodeGenSemantic.Models;
using ProtoBuf.Internal.CodeGenSemantic.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGenSemantic.Parsers;

internal sealed class NamespaceCodeGenModelParser : ISymbolCodeGenModelParser<INamespaceSymbol, CodeGenFile>
{
    /// <summary>
    /// Dives into members of a Roslyn defined symbol to parse the internals and output a codeGenModel representation of a symbol
    /// </summary>
    /// <param name="symbol">namespace symbol of a file - the upper element in Roslyn API available for a file</param>
    /// <param name="parseContext">includes other information about current parse execution</param>
    /// <returns></returns>
    public CodeGenFile Parse(INamespaceSymbol symbol, NamespaceParseContext parseContext)
    {
        var codeGenFile = InitializeFile(symbol);
        
        var childSymbols = symbol.GetMembers();
        foreach (var childSymbol in childSymbols)
        {
            if (childSymbol.IsType)
            {
                var typeParser = SymbolCodeGenModelParserProvider.GetTypeParser();
                var parsedCodeGenType = typeParser.Parse((ITypeSymbol)childSymbol, parseContext);
                if (parsedCodeGenType is null)
                {
                    // throw some kind of exception here
                }
                
                // attach parsed codeGen to appropriate collection
                switch (parsedCodeGenType)
                {
                    case CodeGenMessage codeGenMessage:
                        codeGenFile.Messages.Add(codeGenMessage);
                        break;
                    case CodeGenEnum codeGenEnum:
                        codeGenFile.Enums.Add(codeGenEnum);
                        break;
                }
            }
            
            if (childSymbol.IsNamespace)
            {
                // inner namespace - can it happen ?
                // I guess 1 namespace = 1 CodeGenFile so no
            }
        }

        return codeGenFile;
    }

    private static CodeGenFile InitializeFile(ISymbol symbol)
    {
        var firstPath = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree?.FilePath;
        if (string.IsNullOrWhiteSpace(firstPath)) firstPath = "unknown";

        return new CodeGenFile(firstPath!);
    }
}