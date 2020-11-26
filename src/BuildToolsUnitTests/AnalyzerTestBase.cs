using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public abstract class AnalyzerTestBase<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer
    {
        // utility anaylzer data, with thanks to Samo Prelog
        private readonly ITestOutputHelper? _testOutputHelper;
        protected AnalyzerTestBase(ITestOutputHelper? testOutputHelper) => _testOutputHelper = testOutputHelper;

        protected static TAnalyzer AnalyzerSingleton { get; } = Activator.CreateInstance<TAnalyzer>();
        protected virtual TAnalyzer Analyzer { get; } = AnalyzerSingleton;

        protected virtual bool ReferenceProtoBuf { get; } = true;

        protected virtual Project SetupProject(Project project) => project;

        protected Task<ICollection<Diagnostic>> AnalyzeAsync(string? sourceCode = null, [CallerMemberName] string callerMemberName = null) =>
            AnalyzeAsync(project => string.IsNullOrWhiteSpace(sourceCode) ? project : project.AddDocument(callerMemberName + ".cs", sourceCode).Project, callerMemberName);

        protected async Task<ICollection<Diagnostic>> AnalyzeAsync(Func<Project, Project> projectModifier, [CallerMemberName] string callerMemberName = null)
        {
            var (project, compilation) = await ObtainProjectAndCompilationAsync(projectModifier);
            var analyzers = project.AnalyzerReferences.SelectMany(x => x.GetAnalyzers(project.Language)).ToImmutableArray();
            var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, project.AnalyzerOptions);
            var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
            if (_testOutputHelper is object)
            {
                foreach (var d in diagnostics)
                {
                    _testOutputHelper.WriteLine(d.ToString());
                }
            }
            return diagnostics;
        }

        protected async Task<(Project Project, Compilation Compilation)> ObtainProjectAndCompilationAsync(Func<Project, Project>? projectModifier = null, [CallerMemberName] string callerMemberName = null)
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("protobuf-net.BuildTools.AnalyzerTests", LanguageNames.CSharp);
            project = project
                .WithCompilationOptions(project.CompilationOptions!.WithOutputKind(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
                .AddAnalyzerReference(new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(Analyzer)));
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
    }
}
