#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProtoBuf.Internal.CodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen.Models;
using ProtoBuf.Internal.CodeGen.Providers;
using ProtoBuf.Reflection.Internal.CodeGen;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal sealed class NamespaceCodeGenModelParser : SymbolCodeGenModelParserBase<INamespaceOrTypeSymbol, CodeGenFile>
{
    public NamespaceCodeGenModelParser(SymbolCodeGenModelParserProvider parserProvider) : base(parserProvider)
    {
    }
    
    /// <summary>
    /// Dives into members of a Roslyn defined symbol to parse the internals and output a codeGenModel representation of a symbol
    /// </summary>
    /// <param name="symbol">namespace symbol of a file - the upper element in Roslyn API available for a file</param>
    /// <returns></returns>
    public override CodeGenFile Parse(INamespaceOrTypeSymbol symbol)
    {
        var codeGenFile = InitializeFile(symbol);

        // loading namespace data as well
        var namespaceContext = new CodeGenNamespaceParseContext(symbol.Name);

        if (symbol is INamespaceSymbol ns)
        {
            var childSymbols = ns.GetMembers();
            foreach (var childSymbol in childSymbols)
            {
                Parse(codeGenFile, namespaceContext, childSymbol);
            }
        }
        else if (symbol is ITypeSymbol type)
        {
            Parse(codeGenFile, namespaceContext, type);
        }

        return codeGenFile;
    }

    private void Parse(CodeGenFile codeGenFile, CodeGenNamespaceParseContext namespaceContext, INamespaceOrTypeSymbol childSymbol)
    {
        if (childSymbol is ITypeSymbol typedChildSymbol)
        {
            switch (typedChildSymbol.TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Class:
                    var messageParser = ParserProvider.GetMessageParser(namespaceContext);
                    var codeGenMessage = messageParser.Parse(typedChildSymbol);
                    codeGenFile.Messages.Add(codeGenMessage);
                    break;

                case TypeKind.Enum:
                    var enumParser = ParserProvider.GetEnumParser();
                    var codeGenEnum = enumParser.Parse(typedChildSymbol);
                    codeGenFile.Enums.Add(codeGenEnum);
                    break;

                case TypeKind.Interface:
                    var serviceParser = ParserProvider.GetServiceParser(namespaceContext);
                    var codeGenService = serviceParser.Parse(typedChildSymbol);
                    codeGenFile.Services.Add(codeGenService);
                    break;
            }
        }
        else if (childSymbol is INamespaceSymbol ns)
        {
            foreach (var child in ns.GetMembers())
            {
                Parse(codeGenFile, namespaceContext, child);
            }
        }
    }

    private static CodeGenFile InitializeFile(ISymbol symbol)
    {
        var firstPath = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree?.FilePath;
        if (string.IsNullOrWhiteSpace(firstPath)) firstPath = "unknown";

        return new CodeGenFile(firstPath!);
    }
}