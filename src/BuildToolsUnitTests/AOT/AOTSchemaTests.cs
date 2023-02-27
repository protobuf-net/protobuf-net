﻿// uncomment this to cause the source folder to be updated with the current outputs;
// this makes changes visible in the source repo, for comparison
#define UPDATE_FILES

using Google.Protobuf.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Internal.CodeGen;
using ProtoBuf.Nano;
using ProtoBuf.Reflection.Internal.CodeGen;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.AOT;

public class AOTSchemaTests
{
    private readonly ITestOutputHelper _output;

    public AOTSchemaTests(ITestOutputHelper output)
        => _output = output;

    private const string SchemaPath = "AOT/Schemas";
    public static IEnumerable<object[]> GetSchemas()
    {
        foreach (var file in Directory.GetFiles(SchemaPath, "*.proto", SearchOption.AllDirectories))
        {
            yield return new object[] { Regex.Replace(file.Replace('\\', '/'), "^Schemas/", "") };
        }
    }

    private static JsonSerializerSettings JsonSettings { get; } = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        Converters = { new StringEnumConverter() },
    };

    [Theory]
    [MemberData(nameof(GetSchemas))]
    public void ParseProtoToModel(string protoPath)
    {
        // load .proto file
        var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), SchemaPath);
        _output.WriteLine(protoPath);
        var file = Path.GetFileName(protoPath);
        _output.WriteLine($"{file} in {Path.GetDirectoryName(Path.Combine(schemaPath, protoPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))}");

        // build FDS out of proto definition
        var fds = new FileDescriptorSet();
        fds.AddImportPath(schemaPath);
        Assert.True(fds.Add(file, true));
        fds.Process();
        var errors = fds.GetErrors();
        foreach (var error in errors)
        {
            _output.WriteLine($"{error.LineNumber}:{error.ColumnNumber}: {error.Message}");
        }
        Assert.Empty(errors);

        // build CodeGenSet out of FDS and check expected json equality
        var context = new CodeGenParseContext();
        var parsed = CodeGenSet.Parse(fds, context);
        var json = JsonConvert.SerializeObject(parsed, JsonSettings);
        // _output.WriteLine(json);

        var jsonPath = Path.ChangeExtension(protoPath, ".json");
#if UPDATE_FILES
        var target = Path.Combine(Path.GetDirectoryName(CallerFilePath())!, "..", jsonPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        _output.WriteLine($"updating {target}...");
        File.WriteAllText(target, json);
#else
        Assert.True(File.Exists(jsonPath), $"{jsonPath} does not exist");
        var expectedJson = File.ReadAllText(jsonPath);
        Assert.Equal(expectedJson, json);
#endif

        // generate C# model out of Code-Gen model
        var generateOptions = new Dictionary<string, string>
        {
            { "toolversion", "test" }
        };
        var codeFile = Assert.Single(new CodeGenCSharpCodeGenerator().Generate(parsed, generateOptions));
        Assert.Equal(Path.ChangeExtension(Path.GetFileName(protoPath), "cs"), codeFile.Name);
        Assert.NotNull(codeFile);

        var csPath = Path.ChangeExtension(protoPath, ".cs");
#if UPDATE_FILES
        target = Path.Combine(Path.GetDirectoryName(CallerFilePath())!, "..", csPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        _output.WriteLine($"updating {target}...");
        File.WriteAllText(target, codeFile.Text);
#else
        Assert.True(File.Exists(csPath), $"{csPath} does not exist");
        var expectedCs = File.ReadAllText(csPath);
        Assert.Equal(expectedCs, codeFile.Text);
#endif

        // now we run the *generated* code (minus the serializers) through Roslyn, and see if we can parse it again
        const CodeGenGenerate options = ~(CodeGenGenerate.DataSerializer | CodeGenGenerate.ServiceProxy);
        generateOptions["emit"] = options.ToString();
        codeFile = Assert.Single(new CodeGenCSharpCodeGenerator().Generate(parsed, generateOptions));
        var dataContractSyntaxTree = CSharpSyntaxTree.ParseText(codeFile.Text, new CSharpParseOptions(LanguageVersion.Preview), path: Path.GetFileName(csPath));
        var libs = new[]
        {
            GetLib<object>(),
            GetLib<ProtoWriter>(),
            GetLib<INanoSerializer<int>>(),
            GetLib<CallContext>(),
            GetLib<ServiceContractAttribute>(),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.2.2.0").Location),
        };
        var compilation = CSharpCompilation.Create("MyCompilation.dll",
            syntaxTrees: new[] { dataContractSyntaxTree }, references: libs, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // try the compile; if it doesn't look good here, everything else is broken
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        foreach (var diag in result.Diagnostics)
        {
            if (diag.Severity == DiagnosticSeverity.Error)
            {
                _output.WriteLine(diag.ToString());
            }
        }

        Assert.True(result.Success, "generated code does not compile");

        var parser = new CodeGenSemanticModelParser();
        parser.Parse(compilation, dataContractSyntaxTree);
        var parsedFromCode = parser.Process();

        json = JsonConvert.SerializeObject(parsedFromCode, JsonSettings);
        // _output.WriteLine(json);
        jsonPath = Path.ChangeExtension(protoPath, ".pjson");
#if UPDATE_FILES
        target = Path.Combine(Path.GetDirectoryName(CallerFilePath())!, "..", jsonPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        _output.WriteLine($"updating {target}...");
        File.WriteAllText(target, json);
#else
        Assert.True(File.Exists(jsonPath), $"{jsonPath} does not exist");
        expectedJson = File.ReadAllText(jsonPath);
        Assert.Equal(expectedJson, json);
#endif

        // generate C# model out of Code-Gen model
        generateOptions.Remove("emit");
        codeFile = Assert.Single(new CodeGenCSharpCodeGenerator().Generate(parsedFromCode, generateOptions));
        Assert.Equal(Path.ChangeExtension(Path.GetFileName(protoPath), "generated.cs"), codeFile.Name);
        Assert.NotNull(codeFile);

        csPath = Path.ChangeExtension(protoPath, ".pcs");
#if UPDATE_FILES
        target = Path.Combine(Path.GetDirectoryName(CallerFilePath())!, "..", csPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        _output.WriteLine($"updating {target}...");
        File.WriteAllText(target, codeFile.Text);
#else
        Assert.True(File.Exists(csPath), $"{csPath} does not exist");
        var expectedCs = File.ReadAllText(csPath);
        Assert.Equal(expectedCs, codeFile.Text);
#endif

        var serializerSyntaxTree = CSharpSyntaxTree.ParseText(codeFile.Text, new CSharpParseOptions(LanguageVersion.Preview), path: Path.GetFileName(csPath));
        compilation = CSharpCompilation.Create("MyCompilation.dll",
            syntaxTrees: new[] { serializerSyntaxTree, dataContractSyntaxTree }, references: libs, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // try the compile; if it doesn't look good here, everything else is broken
        ms.Position = 0; // reset
        ms.SetLength(0);
        result = compilation.Emit(ms);
        foreach (var diag in result.Diagnostics)
        {
            if (diag.Severity == DiagnosticSeverity.Error)
            {
                _output.WriteLine(diag.ToString());
            }
        }

        Assert.True(result.Success, "generated code (from parse) does not compile");
    }

    static MetadataReference GetLib<T>()
        => MetadataReference.CreateFromFile(typeof(T).Assembly.Location);

    private static string CallerFilePath([CallerFilePath] string path = "") => path;
}
