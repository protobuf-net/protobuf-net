using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen;
using Xunit;
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

    protected object GetCodeGenSet(string csFileName)
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
        
        var model = compilation.GetSemanticModel(syntaxTree, false);
        Assert.NotNull(model);

        CodeGenSet parsedFromCode = new();
        foreach (var symbol in model.LookupNamespacesAndTypes(0))
        {
            var firstRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (firstRef is not null && firstRef.SyntaxTree == syntaxTree)
            {
                parsedFromCode = CodeGenSemanticModelParser.Parse(parsedFromCode, symbol);
            }
        }

        return parsedFromCode;
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