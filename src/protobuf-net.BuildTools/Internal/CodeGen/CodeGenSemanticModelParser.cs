#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProtoBuf.Internal.CodeGen;

internal class CodeGenSemanticModelParser : IDiagnosticSink
{
    //public static CodeGenSet Parse(ISymbol symbol)
    //{
    //    var codeGenSet = new CodeGenSet();
    //    return Parse(codeGenSet, symbol);
    //}

    public CodeGenSemanticModelParser(IDiagnosticSink? diagnosticSink = null) => set = new CodeGenSet(diagnosticSink);
    public CodeGenSemanticModelParser(in GeneratorExecutionContext executionContext)
    {
        _executionContext = executionContext;
        set = new CodeGenSet(this);
    }

    private readonly GeneratorExecutionContext? _executionContext;

    private readonly CodeGenParseContext codeGenParseContext = new CodeGenParseContext();
    internal CodeGenParseContext Context => codeGenParseContext;
    private readonly CodeGenSet set;
    private CodeGenFile? defaultFile;
    private CodeGenFile DefaultFile
    {
        get
        {
            if (defaultFile is null)
            {
                defaultFile = new CodeGenFile("protogen.generated.cs");
                _defaultContext = new CodeGenFileParseContext(defaultFile, this);
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
        var ext = Path.GetExtension(syntaxTree.FilePath);
        var file = new CodeGenFile(Path.ChangeExtension(syntaxTree.FilePath, "generated" + ext));
        var ctx = new CodeGenFileParseContext(file, this);
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
        if (!file.IsEmpty) set.Files.Add(file);
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

    // is this a new symbol?
    internal bool HasConsidered(ISymbol symbol)
        => !(_seen ??= new HashSet<ISymbol>(SymbolEqualityComparer.Default)).Add(symbol);

    internal void ReportDiagnostic(DiagnosticDescriptor diagnostic, ISymbol source, params object[] messageArgs)
    {
        if (_executionContext.HasValue)
        {
            var loc = source.Locations.FirstOrDefault();
            _executionContext.GetValueOrDefault().ReportDiagnostic(Diagnostic.Create(diagnostic, loc, messageArgs));
        }
        else if (set.DiagnosticSink is { } sink)
        {
            // intended for debug
            var mapped = new CodeGenDiagnostic(diagnostic.Id,
                (string?)diagnostic.Title ?? "", (string ?)diagnostic.MessageFormat ?? "",
                (CodeGenDiagnostic.DiagnosticSeverity)diagnostic.DefaultSeverity);
            sink.ReportDiagnostic(mapped, LocationWrapper.Create(source), messageArgs);
        }
    }
    sealed private class LocationWrapper : ILocated
    {
        public object? Origin { get; }
        private LocationWrapper(object? origin) => Origin = origin;
        internal static ILocated? Create(ISymbol? loc)  => loc is null ? null : new LocationWrapper(loc);
    }

    void IDiagnosticSink.ReportDiagnostic(CodeGenDiagnostic diagnostic, ILocated? located, params object[] messageArgs)
    {
        if (_executionContext.HasValue)
        {
            var loc = (located?.Origin as ISymbol)?.Locations.FirstOrDefault();
            var mapped = MapDescriptor(diagnostic);
            _executionContext.GetValueOrDefault().ReportDiagnostic(Diagnostic.Create(mapped, loc, messageArgs));
        }
    }

    private DiagnosticDescriptor MapDescriptor(CodeGenDiagnostic diagnostic)
    {
        if (!s_MappedDescriptors.TryGetValue(diagnostic.Id, out var found))
        {
            found = new DiagnosticDescriptor(diagnostic.Id, diagnostic.Title, diagnostic.MessageFormat, Literals.CategoryUsage, (DiagnosticSeverity)diagnostic.Severity, true);
            s_MappedDescriptors[diagnostic.Id] = found;
        }
        return found;
    }
    private static readonly ConcurrentDictionary<string, DiagnosticDescriptor> s_MappedDescriptors = new();

    private HashSet<ISymbol>? _seen;
}