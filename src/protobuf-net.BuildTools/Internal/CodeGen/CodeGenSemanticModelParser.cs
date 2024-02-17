#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Internal.CodeGen.Parsers;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ProtoBuf.Internal.CodeGen;

internal partial class CodeGenSemanticModelParser : CodeGenParseContext
{
    //public static CodeGenSet Parse(ISymbol symbol)
    //{
    //    var codeGenSet = new CodeGenSet();
    //    return Parse(codeGenSet, symbol);
    //}

    public CodeGenSemanticModelParser(IDiagnosticSink? diagnostics = null)
    {
        set = new(diagnostics);
    }
    public CodeGenSemanticModelParser(in GeneratorExecutionContext executionContext)
    {
        _executionContext = executionContext;
        set = new(null);
    }

    private readonly GeneratorExecutionContext? _executionContext;

    private readonly CodeGenSet set;

    public int Parse(Compilation compilation, SyntaxTree syntaxTree)
    {
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        int count = 0;
        var ext = Path.GetExtension(syntaxTree.FilePath);
        var file = new CodeGenFile(Path.ChangeExtension(syntaxTree.FilePath, "generated" + ext));
        var candidates = semanticModel.LookupNamespacesAndTypes(0);
        if (candidates.Length != 0)
        {
            foreach (var symbol in candidates)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Namespace when symbol is INamespaceSymbol ns && FromTree(symbol, syntaxTree):
                        RecurseNamespace(ns);
                        break;
                    case SymbolKind.NamedType when symbol is INamedTypeSymbol type && FromTree(symbol, syntaxTree):
                        RecurseNamedType(type);
                        break;
                }
            }
        }
        if (!file.IsEmpty) set.Files.Add(file);
        return count;

        static bool FromTree(ISymbol symbol, SyntaxTree tree)
        {
            foreach (var src in symbol.DeclaringSyntaxReferences)
            {
                if (src.SyntaxTree == tree)
                {
                    return true;
                }
            }
            return false;
        }

    }

    private void RecurseNamespace(INamespaceSymbol symbol)
    {
        foreach (var type in symbol.GetTypeMembers())
        {
            RecurseNamedType(type);
        }
        foreach (var ns in symbol.GetNamespaceMembers())
        {
            RecurseNamespace(ns);
        }
    }

    static bool HasProtoGenerate(ISymbol symbol)
    {
        return HasLocal(symbol) || HasLocal(symbol.ContainingModule) || HasLocal(symbol.ContainingAssembly);
        static bool HasLocal(ISymbol symbol)
        {
            foreach (var attrib in symbol.GetAttributes())
            {
                var ac = attrib.AttributeClass;
                if (ac is not null && ac.InProtoBufNamespace() && ac.Name == "ProtoGenerateAttribute") return true;
            }
            return false;
        }
    }

    private void RecurseNamedType(INamedTypeSymbol type)
    {
        ParseNamedType(type);
        foreach (var inner in type.GetTypeMembers())
        {
            RecurseNamedType(inner);
        }
    }
    public CodeGenSet Process()
    {
        // note: if message/enum type is consumed before it is defined, we simplify things
        // by using a place-holder initially (via the protobuf FQN); we need to go back over the
        // tree, and substitute out any such place-holders for the final types
        FixupPlaceholders();

        //// throwing errors, which happened during parsing
        //// warnings will be available later for logging output
        //symbolCodeGenModelParserProvider.ErrorContainer.Throw();

        //set.ErrorContainer = symbolCodeGenModelParserProvider.ErrorContainer;
        return set;
    }

    // is this a new symbol?
    internal bool TryGetType(ISymbol symbol, out CodeGenType type)
    {
        if (!_knownSymbols.TryGetValue(symbol, out type))
        {
            type = CodeGenType.Unknown;
            return false;
        }
        return true;
    }
    internal void Add(ISymbol symbol, CodeGenType type)
    {
        _knownSymbols.Add(symbol, type);
        var parent = symbol.ContainingSymbol;
        if (parent is null)
        {

        }
        else if (parent is INamedTypeSymbol named)
        {
            ParseNamedType(named);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected parent: {parent.GetType().FullName}");
        }
    }

    private void ParseAttribute<T>(ISymbol symbol, AttributeData attrib, T obj, Func<string, bool, T, TypedConstant, bool> handler) where T : class
    {
        var ctorArgs = attrib.ConstructorArguments;
        if (!ctorArgs.IsDefaultOrEmpty)
        {
            var paramDefs = attrib.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
            var lim = Math.Min(ctorArgs.Length, paramDefs.Length);
            for (int i = 0; i < lim; i++)
            {
                if (!handler(paramDefs[i].Name, true, obj, ctorArgs[i]))
                {
                    ReportDiagnostic(ParseUtils.UnhandledAttributeValue, symbol,
                        attrib.AttributeClass?.Name ?? "", paramDefs[i].Name);
                }
            }
        }
        foreach (var pair in attrib.NamedArguments)
        {
            if (!handler(pair.Key, false, obj, pair.Value))
            {
                ReportDiagnostic(ParseUtils.UnhandledAttributeValue, symbol,
                    attrib.AttributeClass?.Name ?? "", pair.Key);
            }
        }
    }

    internal void ReportDiagnostic(DiagnosticDescriptor diagnostic, ISymbol source, params object[] messageArgs)
    {
        if (_executionContext.HasValue)
        {
            var loc = source.Locations.FirstOrDefault();
            _executionContext.GetValueOrDefault().ReportDiagnostic(Diagnostic.Create(diagnostic, loc, messageArgs));
        }

        if (set.DiagnosticSink is { } diagnostics)
        {
            // intended for debug
            var mapped = new CodeGenDiagnostic(diagnostic.Id,
                (string?)diagnostic.Title ?? "", (string ?)diagnostic.MessageFormat ?? "",
                (CodeGenDiagnostic.DiagnosticSeverity)diagnostic.DefaultSeverity);
            diagnostics.ReportDiagnostic(mapped, LocationWrapper.Create(source), messageArgs);
        }
    }

    internal override void ReportDiagnostic(CodeGenDiagnostic diagnostic, ILocated located, params object[] messageArgs)
    {
        if (_executionContext.HasValue)
        {
            var mapped = MapDescriptor(diagnostic);
            var loc = (located.Origin as ISymbol)?.Locations.FirstOrDefault();
            if (_executionContext.HasValue)
            {
                _executionContext.GetValueOrDefault().ReportDiagnostic(Diagnostic.Create(mapped, loc, messageArgs));
            }
        }
        set.DiagnosticSink?.ReportDiagnostic(diagnostic, located, messageArgs);
    }

    sealed private class LocationWrapper : ILocated
    {
        public object? Origin { get; }
        private LocationWrapper(object? origin) => Origin = origin;
        internal static ILocated? Create(ISymbol? loc)  => loc is null ? null : new LocationWrapper(loc);
    }

    //void IDiagnosticSink.ReportDiagnostic(CodeGenDiagnostic diagnostic, ILocated located, params object[] messageArgs)
    //{
    //    if (_executionContext.HasValue)
    //    {
    //        var loc = (located.Origin as ISymbol)?.Locations.FirstOrDefault();
    //        var mapped = MapDescriptor(diagnostic);
    //        _executionContext.GetValueOrDefault().ReportDiagnostic(Diagnostic.Create(mapped, loc, messageArgs));
    //    }
    //}

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

    private readonly Dictionary<ISymbol, CodeGenType> _knownSymbols = new(SymbolEqualityComparer.Default);

    protected override IEnumerable<CodeGenType> GetKnownTypes() => _knownSymbols.Values;

    protected override bool TryResolve(CodeGenPlaceholderType placeholder, out CodeGenType value)
    {
        value = CodeGenType.Unknown;
        return placeholder is CodeGenSymbolPlaceholderType symbolic && _knownSymbols.TryGetValue(symbolic.Symbol, out value);
    }

    public CodeGenType GetContractType(ISymbol symbol)
    {
        if (symbol is null) return CodeGenType.Unknown;
        if (!_knownSymbols.TryGetValue(symbol, out var found))
        {
            found = new CodeGenSymbolPlaceholderType(symbol);
            _knownSymbols.Add(symbol, found);
        }
        return found;
    }
    sealed class CodeGenSymbolPlaceholderType : CodeGenPlaceholderType
    {
        public ISymbol Symbol { get; }
        public CodeGenSymbolPlaceholderType(ISymbol symbol) : base(symbol.Name, symbol.GetFullyQualifiedPrefix())
            => Symbol = symbol;
    }
}