#nullable enable

using Microsoft.CodeAnalysis;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal readonly struct CodeGenFileParseContext
{
    public CodeGenFile File { get; }
    public CodeGenParseContext Context => Parser.Context;
    public CodeGenSemanticModelParser Parser { get; }

    public bool HasConsidered(ISymbol symbol) => Parser.HasConsidered(symbol);
    public CodeGenFileParseContext(CodeGenFile file, CodeGenSemanticModelParser parser)
    {
        File = file;
        Parser = parser;
    }

    internal void SaveWarning(string message, ISymbol source)
    {
        //
    }
}
