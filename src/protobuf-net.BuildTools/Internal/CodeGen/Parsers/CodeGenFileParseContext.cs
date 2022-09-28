#nullable enable

using Microsoft.CodeAnalysis;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;

namespace ProtoBuf.Internal.CodeGen.Parsers;

internal readonly struct CodeGenFileParseContext
{
    public CodeGenFile File { get; }
    public CodeGenParseContext Context { get; }
    public CodeGenFileParseContext(CodeGenFile file, CodeGenParseContext context)
    {
        File = file;
        Context = context;
    }

    internal void SaveWarning(string message, ISymbol source)
    {
        //
    }
}
