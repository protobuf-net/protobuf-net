﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.Generators.Abstractions
{
    public abstract class GeneratorTestBase<TGenerator> where TGenerator : ISourceGenerator
    {
        protected static AdditionalText[] Text(string path, string content) => new[] { new InMemoryAdditionalText(path, content) };
        protected static AdditionalText[] Texts(params (string path, string content)[] pairs) => pairs.Select(pair => new InMemoryAdditionalText(pair.path, pair.content)).ToArray();

        protected static AdditionalText[] Texts(params (string path, string content, (string key, string value)[]? options)[] pairs) => pairs.Select(pair => new InMemoryAdditionalText(pair.path, pair.content, pair.options)).ToArray();

        protected static ImmutableDictionary<string, string> Options(params (string key, string value)[] pairs) => pairs.ToImmutableDictionary(pair => pair.key, pair => pair.value);

        // utility anaylzer data, with thanks to Samo Prelog
        protected readonly ITestOutputHelper? TestOutputHelper;
        protected GeneratorTestBase(ITestOutputHelper? testOutputHelper = null) => TestOutputHelper = testOutputHelper;

        protected virtual TGenerator Generator
        {
            get
            {
                var obj = Activator.CreateInstance<TGenerator>();
                if (obj is ILoggingAnalyzer logging && TestOutputHelper is not null)
                {
                    logging.Log += s => TestOutputHelper.WriteLine(s);
                }
                return obj;
            }
        }

        protected Task<(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics)> GenerateAsync(
            string csharpCodeText,
            string sourceFileName,
            AdditionalText[]? additionalTexts = null,
            ImmutableDictionary<string, string>? globalOptions = null,
            [CallerMemberName] string? callerMemberName = null,
            bool debugLog = true)
        {
            var addSourcesProjectModifier = (Project project) => project.AddDocument(sourceFileName + ".cs", csharpCodeText).Project;
            return GenerateAsync(additionalTexts, globalOptions, addSourcesProjectModifier, callerMemberName, debugLog);
        }
        
        protected Task<(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics)> GenerateAsync(
            string[] cSharpProjectSourceTexts,
            AdditionalText[]? additionalTexts = null,
            ImmutableDictionary<string, string>? globalOptions = null,
            [CallerMemberName] string? callerMemberName = null,
            bool debugLog = true)
        {
            var addSourcesProjectModifier = (Project project) =>
            { 
                var index = 1;
                foreach (var sourceText in cSharpProjectSourceTexts)
                {
                    var newDoc = project.AddDocument("sourceFile_" + index++ + ".cs", sourceText);
                    project = newDoc.Project;
                }

                return project;
            };

            return GenerateAsync(additionalTexts, globalOptions, addSourcesProjectModifier, callerMemberName, debugLog);
        }

        protected async Task<(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics)> GenerateAsync(AdditionalText[]? additionalTexts = null, ImmutableDictionary<string, string>? globalOptions = null,
            Func<Project, Project>? projectModifier = null, [CallerMemberName] string? callerMemberName = null, bool debugLog = true)
        {
            if (!typeof(TGenerator).IsDefined(typeof(GeneratorAttribute)))
            {
                throw new InvalidOperationException($"Type is not marked [Generator]: {typeof(TGenerator)} in {callerMemberName}");
            }

            var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);

            if (globalOptions is null) globalOptions = ImmutableDictionary<string, string>.Empty;
            if (debugLog) globalOptions = globalOptions.SetItem("pbn_debug_log", "true");

            var optionsProvider = TestAnalyzeConfigOptionsProvider.Empty.WithGlobalOptions(new TestAnalyzerConfigOptions(globalOptions));
            if (additionalTexts is not null && additionalTexts.Length != 0)
            {
                var map = ImmutableDictionary.CreateBuilder<object, AnalyzerConfigOptions>();
                foreach (var text in additionalTexts)
                {
                    if (text is InMemoryAdditionalText mem)
                    {
                        map.Add(text, mem.GetOptions());
                    }
                }
                optionsProvider = optionsProvider.WithAdditionalTreeOptions(map.ToImmutable());
            }

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { Generator }, additionalTexts, parseOptions: parseOptions, optionsProvider: optionsProvider);
            (var project, var compilation) = await ObtainProjectAndCompilationAsync(projectModifier);
            var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
            if (TestOutputHelper is object)
            {
                foreach (var d in diagnostics)
                {
                    TestOutputHelper.WriteLine(d.ToString());
                }
            }
            
            return (result.GetRunResult(), diagnostics);
        }

        protected virtual bool ReferenceProtoBuf => true;

        protected async Task<(Project Project, Compilation Compilation)> ObtainProjectAndCompilationAsync(Func<Project, Project>? projectModifier = null, [CallerMemberName] string? callerMemberName = null)
        {
            _ = callerMemberName;
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("protobuf-net.BuildTools.GeneratorTests", LanguageNames.CSharp);
            project = project
                .WithCompilationOptions(project.CompilationOptions!.WithOutputKind(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location));

            project = SetupProject(project);

            if (ReferenceProtoBuf)
            {
                project = project
                    .AddMetadataReference(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location))
                    .AddMetadataReference(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location))
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(TypeModel).Assembly.Location));
            }

            project = projectModifier?.Invoke(project) ?? project;

            var compilation = await project.GetCompilationAsync();
            return (project, compilation!);
        }

        protected virtual Project SetupProject(Project project) => project;

        protected Assembly TryBuildAssemblyFromSourceCode(string sourceCode, string assemblyName = "MyAssembly", bool withProtobufReferences = true)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            var metadataReferences = withProtobufReferences
                ? MetadataReferenceHelpers.ProtobufReferences
                : MetadataReferenceHelpers.CoreLibReference;

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                metadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var dllStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var emitResult = compilation.Emit(dllStream, pdbStream);
            if (!emitResult.Success)
            {
                var errors = new StringBuilder();
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    errors.AppendLine(diagnostic.ToString());
                }
                
                TestOutputHelper?.WriteLine("Errors on sourceCode compilation: \n-----\n");
                TestOutputHelper?.WriteLine(errors.ToString());
            }

            dllStream.Position = pdbStream.Position = 0;
            return Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());
        } 
    }
}
