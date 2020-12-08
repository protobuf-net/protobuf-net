using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.Meta;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public abstract class GeneratorTestBase<TGenerator> where TGenerator : ISourceGenerator
    {
        protected static AdditionalText[] Text(string path, string content) => new[] { new InMemoryAdditionalText(path, content) };
        protected static AdditionalText[] Texts(params (string path, string content)[] pairs) => pairs.Select(pair => new InMemoryAdditionalText(pair.path, pair.content)).ToArray();

        protected static ImmutableDictionary<string, string> Options(params (string key, string value)[] pairs) => pairs.ToImmutableDictionary(pair => pair.key, pair => pair.value);

        // utility anaylzer data, with thanks to Samo Prelog
        private readonly ITestOutputHelper? _testOutputHelper;
        protected GeneratorTestBase(ITestOutputHelper? testOutputHelper = null) => _testOutputHelper = testOutputHelper;

        protected static TGenerator GeneratorSingleton { get; } = Activator.CreateInstance<TGenerator>();
        protected virtual TGenerator Generator { get; } = GeneratorSingleton;

        protected async Task<(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics)> GenerateAsync(AdditionalText[] additionalTexts, ImmutableDictionary<string, string>? globalOptions = null,
            Func<Project, Project>? projectModifier = null, [CallerMemberName] string callerMemberName = null, bool debugLog = true)
        {
            if (!typeof(TGenerator).IsDefined(typeof(GeneratorAttribute)))
            {
                throw new InvalidOperationException($"Type is not marked [Generator]: {typeof(TGenerator)} in {callerMemberName}");
            }

            var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);

            if (globalOptions is null) globalOptions = ImmutableDictionary<string, string>.Empty;
            if (debugLog) globalOptions = globalOptions.SetItem("pbn_debug_log", "true");

            var optionsProvider = TestAnalyzeConfigOptionsProvider.Empty.WithGlobalOptions(new TestAnalyzerConfigOptions(globalOptions));
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { Generator }, additionalTexts, parseOptions: parseOptions, optionsProvider: optionsProvider);
            (var project, var compilation) = await ObtainProjectAndCompilationAsync(projectModifier);
            var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
            if (_testOutputHelper is object)
            {
                foreach (var d in diagnostics)
                {
                    _testOutputHelper.WriteLine(d.ToString());
                }
            }
            return (result.GetRunResult(), diagnostics.RemoveAll(x => x.Id == "PBN9999" && x.Severity == DiagnosticSeverity.Info));
        }

        protected virtual bool ReferenceProtoBuf => true;

        protected async Task<(Project Project, Compilation Compilation)> ObtainProjectAndCompilationAsync(Func<Project, Project>? projectModifier = null, [CallerMemberName] string callerMemberName = null)
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
                    .AddMetadataReference(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location))
                    .AddMetadataReference(MetadataReference.CreateFromFile(typeof(TypeModel).Assembly.Location));
            }

            project = projectModifier?.Invoke(project) ?? project;

            var compilation = await project.GetCompilationAsync();
            return (project, compilation!);
        }

        protected virtual Project SetupProject(Project project) => project;


        
    }
}
