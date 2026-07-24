using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace BuildToolsUnitTests.Aot
{
    /// <summary>
    /// Test support for <see cref="IIncrementalGenerator"/>-based generators, where the interesting
    /// assertion is "what exact code came out", tracked via golden files.
    /// </summary>
    public abstract class AotGeneratorTestBase
    {
        // diagnostics that are an artefact of the test compilation's reference set, not the generator
        private static readonly string[] IgnoredDiagnostics = { "CS1701", "CS1702" };

        private readonly ITestOutputHelper? _log;

        protected AotGeneratorTestBase(ITestOutputHelper? log = null) => _log = log;

        /// <summary>
        /// The path of the calling source file, used to write golden files back to the source tree
        /// rather than the build output.
        /// </summary>
        protected static string? GetOriginCodeLocation([CallerFilePath] string? path = null) => path;

        protected sealed class GeneratorResult
        {
            public GeneratorResult(GeneratorDriverRunResult result, string generatedCode, int errorCount)
            {
                Result = result;
                GeneratedCode = generatedCode;
                ErrorCount = errorCount;
            }

            public GeneratorDriverRunResult Result { get; }

            /// <summary>
            /// All generated sources, concatenated in hint-name order with a banner per file.
            /// </summary>
            public string GeneratedCode { get; }

            /// <summary>
            /// Errors in the *output* compilation, i.e. input plus generated code.
            /// </summary>
            public int ErrorCount { get; }
        }

        /// <summary>
        /// Run a generator over some source, capturing the generated code and any diagnostics.
        /// </summary>
        protected GeneratorResult Execute<TGenerator>(
            string source,
            StringBuilder? diagnosticsTo = null,
            string? fileName = null,
            Action<TGenerator>? initializer = null)
            where TGenerator : class, IIncrementalGenerator, new()
        {
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "input.cs";

            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular);
            var inputCompilation = CSharpCompilation.Create(
                "ProtoBuf.BuildTools.AotGeneratorTests",
                new[] { CSharpSyntaxTree.ParseText(source, parseOptions, path: fileName!) },
                MetadataReferenceHelpers.WellKnownReferences.Concat(MetadataReferenceHelpers.ProtoBufReferences),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // the input is *expected* to be incomplete on its own (the trigger attributes are supplied by
            // the generator), so input diagnostics are logged for debugging but never asserted on; the
            // output compilation contains the input trees, so checking it alone is sufficient
            LogDiagnostics("Input code", inputCompilation, diagnosticsTo: null);

            var generator = new TGenerator();
            initializer?.Invoke(generator);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { generator.AsSourceGenerator() }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            var runResult = driver.GetRunResult();
            foreach (var result in runResult.Results)
            {
                if (result.Exception is not null) throw result.Exception;
            }

            var generatorDiagnostics = Normalize(diagnostics);
            if (generatorDiagnostics.Count != 0)
            {
                Output($"Generator produced {generatorDiagnostics.Count} diagnostics:", diagnosticsTo);
                foreach (var d in generatorDiagnostics) OutputDiagnostic(d, diagnosticsTo);
            }

            var errorCount = LogDiagnostics("Output code", outputCompilation, diagnosticsTo);
            return new GeneratorResult(runResult, Combine(runResult), errorCount);
        }

        /// <summary>
        /// Flatten every generated source into a single reviewable document.
        /// </summary>
        private static string Combine(GeneratorDriverRunResult runResult)
        {
            var sources = (from result in runResult.Results
                           from generated in result.GeneratedSources
                           orderby generated.HintName
                           select generated).ToList();
            if (sources.Count == 0) return "";

            var sb = new StringBuilder();
            foreach (var generated in sources)
            {
                if (sb.Length != 0) sb.AppendLine();
                sb.Append("// ---- ").Append(generated.HintName).AppendLine(" ----");
                sb.AppendLine(generated.SourceText.ToString().TrimEnd());
            }
            return sb.ToString();
        }

        private int LogDiagnostics(string caption, Compilation compilation, StringBuilder? diagnosticsTo)
        {
            if (_log is null && diagnosticsTo is null) return 0; // nothing useful to do

            var errorCount = 0;
            foreach (var tree in compilation.SyntaxTrees)
            {
                var raw = compilation.GetSemanticModel(tree).GetDiagnostics();
                errorCount += raw.Count(static x => x.Severity == DiagnosticSeverity.Error);

                var normalized = Normalize(raw);
                if (normalized.Count == 0) continue;

                Output($"{caption} has {normalized.Count} diagnostics from '{tree.FilePath}':", diagnosticsTo);
                foreach (var d in normalized) OutputDiagnostic(d, diagnosticsTo);
            }
            return errorCount;
        }

        private void OutputDiagnostic(Diagnostic diagnostic, StringBuilder? diagnosticsTo)
        {
            var loc = diagnostic.Location.GetMappedLineSpan();
            Output($"{diagnostic.Severity} {diagnostic.Id} {loc.Path} L{loc.StartLinePosition.Line + 1} C{loc.StartLinePosition.Character + 1}", diagnosticsTo);
            Output(diagnostic.GetMessage(CultureInfo.InvariantCulture), diagnosticsTo);
        }

        private void Output(string message, StringBuilder? diagnosticsTo)
        {
            _log?.WriteLine(message);
            diagnosticsTo?.AppendLine(message.Replace('\\', '/')); // normalize paths for cross-platform goldens
        }

        private static List<Diagnostic> Normalize(ImmutableArray<Diagnostic> diagnostics)
            => (from d in diagnostics
                where !IgnoredDiagnostics.Contains(d.Id)
                let loc = d.Location
                orderby loc.SourceTree?.FilePath, loc.SourceSpan.Start, d.Id, d.ToString()
                select d).ToList();
    }
}
