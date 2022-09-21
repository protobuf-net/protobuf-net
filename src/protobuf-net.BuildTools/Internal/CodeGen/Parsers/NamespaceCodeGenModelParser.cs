#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

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
            if (childSymbol is ITypeSymbol typedChildSymbol)
            {
                switch (typedChildSymbol.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Class:
                        var messageParser = SymbolCodeGenModelParserProvider.GetMessageParser();
                        var codeGenMessage = messageParser.Parse(typedChildSymbol, parseContext);
                        codeGenFile.Messages.Add(codeGenMessage);
                        break;
                    
                    case TypeKind.Enum:
                        var enumParser = SymbolCodeGenModelParserProvider.GetEnumParser();
                        var codeGenEnum = enumParser.Parse(typedChildSymbol, parseContext);
                        codeGenFile.Enums.Add(codeGenEnum);
                        break; 
                        
                    default:
                        throw new ArgumentOutOfRangeException();
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