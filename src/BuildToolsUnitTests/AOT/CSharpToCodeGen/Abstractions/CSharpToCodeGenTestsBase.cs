using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.AOT.CSharpToCodeGen.Abstractions;

public abstract class CSharpToCodeGenTestsBase
{
    protected const string SchemaTypesPath = "AOT/CSharpToCodeGen/Schemas";
    protected readonly ITestOutputHelper Output;

    private readonly string _schemaType;
    
    public CSharpToCodeGenTestsBase(ITestOutputHelper output, string schemaType)
    {
        _schemaType = schemaType;
        Output = output;
    }

    private protected sealed class SimpleDiagnosticSink : IDiagnosticSink
    {
        private readonly List<(string Id, CodeGenDiagnostic.DiagnosticSeverity Severity, string? Location, string Message)> _reported = new();
        void IDiagnosticSink.ReportDiagnostic(CodeGenDiagnostic diagnostic, ILocated? source, params object[] messageArgs)
        {
            var loc = (source?.Origin as ISymbol)?.Locations.FirstOrDefault()?.ToString();
            var msg = string.Format(diagnostic.MessageFormat, messageArgs);
            _reported.Add((diagnostic.Id, diagnostic.Severity, loc, msg));
        }
        public (string Id, CodeGenDiagnostic.DiagnosticSeverity Severity, string? Location, string Message)[] ToArray()
            => _reported.ToArray();
    }
    private protected CodeGenSet GetCodeGenSet(string csFileName, out SimpleDiagnosticSink diagnostics)
    {
        var (csFilePath, csFileText) = LoadCSharpFile(csFileName);
        
        var syntaxTree = CSharpSyntaxTree.ParseText(csFileText, new CSharpParseOptions(LanguageVersion.Preview), path: Path.GetFileName(csFilePath));
        var libs = new[]
        {
            GetLib<object>(),
            GetLib<ProtoWriter>(),
            GetLib<CallContext>(),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.2.2.0").Location),
        };
        var compilation = CSharpCompilation.Create("MyCompilation.dll",
            syntaxTrees: new[] { syntaxTree }, references: libs, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        foreach (var diag in result.Diagnostics)
        {
            if (diag.Severity == DiagnosticSeverity.Error)
            {
                Output.WriteLine(diag.ToString());
            }
        }

        diagnostics = new();
        var parser = new CodeGenSemanticModelParser(diagnostics);
        parser.Parse(compilation, syntaxTree);
        return parser.Process();
    }
    
    private (string csFilePath, string csFileText) LoadCSharpFile(string csFileName)
    {
        var csFilePath = Path.Combine(Directory.GetCurrentDirectory(), SchemaTypesPath, _schemaType, csFileName);
        var csFileText = File.ReadAllText(csFilePath);

        return (csFilePath, csFileText);
    }
    
    private static MetadataReference GetLib<T>()
        => MetadataReference.CreateFromFile(typeof(T).Assembly.Location);
}