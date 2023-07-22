using Grpc.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public abstract class GeneratorTestBase<TGenerator>
    {
        private static readonly CSharpParseOptions ParseOptionsLatestLangVer = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Latest)
        .WithPreprocessorSymbols(new string[] {
#if NETFRAMEWORK
        "NETFRAMEWORK",
#endif
#if NET40_OR_GREATER
        "NET40_OR_GREATER",
#endif
#if NET48_OR_GREATER
        "NET48_OR_GREATER",
#endif
#if NET6_0_OR_GREATER
        "NET6_0_OR_GREATER",
#endif
#if NET7_0_OR_GREATER
        "NET7_0_OR_GREATER",
#endif
#if DEBUG
        "DEBUG",
#endif
#if RELEASE
        "RELEASE",
#endif
    });
        protected static SyntaxTree ParseTree(string source, string fileName)
            => CSharpSyntaxTree.ParseText(source, ParseOptionsLatestLangVer).WithFilePath(fileName);

        protected static AdditionalText[] Text(string path, string content) => new[] { new InMemoryAdditionalText(path, content) };
        protected static AdditionalText[] Texts(params (string path, string content)[] pairs) => pairs.Select(pair => new InMemoryAdditionalText(pair.path, pair.content)).ToArray();

        protected static AdditionalText[] Texts(params (string path, string content, (string key, string value)[]? options)[] pairs) => pairs.Select(pair => new InMemoryAdditionalText(pair.path, pair.content, pair.options)).ToArray();

        protected static ImmutableDictionary<string, string> Options(params (string key, string value)[] pairs) => pairs.ToImmutableDictionary(pair => pair.key, pair => pair.value);

        // utility anaylzer data, with thanks to Samo Prelog
        private readonly ITestOutputHelper? _testOutputHelper;
        protected GeneratorTestBase(ITestOutputHelper? testOutputHelper = null) => _testOutputHelper = testOutputHelper;

        protected virtual TGenerator Generator
        {
            get
            {
                var obj = Activator.CreateInstance<TGenerator>();
                if (obj is ILoggingAnalyzer logging && _testOutputHelper is not null)
                {
                    logging.Log += s => _testOutputHelper.WriteLine(s);
                }
                return obj;
            }
        }

        protected void Log(string message) => _testOutputHelper?.WriteLine(message);

        protected async Task<(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics, int ErrorCount)> GenerateAsync(AdditionalText[] additionalTexts, ImmutableDictionary<string, string>? globalOptions = null,
            Func<Project, Project>? projectModifier = null, [CallerMemberName] string? callerMemberName = null, bool debugLog = true, SyntaxTree[]? trees = null, StringBuilder? buildOutput = null)
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

            GeneratorDriver driver = Generator switch
            {
                IIncrementalGenerator incrementalGenerator => CSharpGeneratorDriver.Create(incrementalGenerator),
                ISourceGenerator generator => CSharpGeneratorDriver.Create(generator),
                _ => throw new InvalidOperationException("Generator is not supported"),
            };
            if (additionalTexts is not null)
            {
                driver = driver.AddAdditionalTexts(additionalTexts.ToImmutableArray());
            }
            if (parseOptions is not null)
            {
                driver = driver.WithUpdatedParseOptions(parseOptions);
            }
            if (optionsProvider is not null)
            {
                driver = driver.WithUpdatedAnalyzerConfigOptions(optionsProvider);
            }

            (var project, var compilation) = await ObtainProjectAndCompilationAsync(projectModifier);
            if (trees is not null)
            {
                compilation = compilation.AddSyntaxTrees(trees);
            }
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            foreach (var d in diagnostics)
            {
                OutputDiagnostic(d);
            }

            var runResult = driver.GetRunResult();
            foreach (var result in runResult.Results)
            {
                if (result.Exception is not null) throw result.Exception;
            }

            var errorCount = ShowDiagnostics("Output code", outputCompilation, buildOutput, "CS1701", "CS1702");
            return (runResult, diagnostics, errorCount);

            void OutputDiagnostic(Diagnostic d)
            {
                Output("", true);
                var loc = d.Location.GetMappedLineSpan();
                Output($"{d.Severity} {d.Id} {loc.Path} L{loc.StartLinePosition.Line + 1} C{loc.StartLinePosition.Character + 1}");
                Output(d.GetMessage(CultureInfo.InvariantCulture));
            }
            void Output(string message, bool force = false)
            {
                if (force || !string.IsNullOrWhiteSpace(message))
                {
                    _testOutputHelper?.WriteLine(message);
                    buildOutput?.AppendLine(message.Replace('\\', '/')); // need to normalize paths
                }
            }
        }
        static int ShowDiagnostics(string caption, Compilation compilation, StringBuilder? diagnosticsTo, params string[] ignore)
        {
            void Output(string message, bool force = false)
            {
                if (force || !string.IsNullOrWhiteSpace(message))
                {
                    diagnosticsTo?.AppendLine(message.Replace('\\', '/')); // need to normalize paths
                }
            }
            int errorCountTotal = 0;
            foreach (var tree in compilation.SyntaxTrees)
            {
                var rawDiagnostics = compilation.GetSemanticModel(tree).GetDiagnostics();
                var diagnostics = Normalize(rawDiagnostics, ignore);
                errorCountTotal += rawDiagnostics.Count(x => x.Severity == DiagnosticSeverity.Error);

                if (diagnostics.Any())
                {
                    Output($"{caption} has {diagnostics.Count} diagnostics from '{tree.FilePath}':");
                    foreach (var d in diagnostics)
                    {
                        OutputDiagnostic(d);
                        if (d.Severity >= DiagnosticSeverity.Error) errorCountTotal++;
                    }
                }
            }
            return errorCountTotal;

            void OutputDiagnostic(Diagnostic d)
            {
                Output("", true);
                var loc = d.Location.GetMappedLineSpan();
                Output($"{d.Severity} {d.Id} {loc.Path} L{loc.StartLinePosition.Line + 1} C{loc.StartLinePosition.Character + 1}");
                Output(d.GetMessage(CultureInfo.InvariantCulture));
            }
            static List<Diagnostic> Normalize(ImmutableArray<Diagnostic> diagnostics, string[] ignore) => (
                from d in diagnostics
                where !ignore.Contains(d.Id)
                let loc = d.Location
                orderby loc.SourceTree?.FilePath, loc.SourceSpan.Start, d.Id, d.ToString()
                select d).ToList();
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
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(TypeModel).Assembly.Location))
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(ServiceAttribute).Assembly.Location))
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(ServiceContractAttribute).Assembly.Location))
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(ClientBase).Assembly.Location));
            }

            project = projectModifier?.Invoke(project) ?? project;

            var compilation = await project.GetCompilationAsync();
            return (project, compilation!);
        }

        protected virtual Project SetupProject(Project project) => project;


        protected static string? GetOriginCodeLocation([CallerFilePath] string? path = null) => path;
    }
}
