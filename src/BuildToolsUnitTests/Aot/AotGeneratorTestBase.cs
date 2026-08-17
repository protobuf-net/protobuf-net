using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// The compile-time model attributes are <c>[Experimental]</c>, which is an <em>error</em> by
        /// default - deliberately, so that using them is an explicit choice. Every fixture uses them,
        /// so the harness opts in on their behalf; real projects do it with <c>NoWarn</c>.
        /// </summary>
        internal static readonly CSharpCompilationOptions CompilationOptions
            = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
                {
                    ["PBN9001"] = ReportDiagnostic.Suppress,
                });

        private readonly ITestOutputHelper? _log;

        protected AotGeneratorTestBase(ITestOutputHelper? log = null) => _log = log;

        /// <summary>
        /// The path of the calling source file, used to write golden files back to the source tree
        /// rather than the build output.
        /// </summary>
        protected static string? GetOriginCodeLocation([CallerFilePath] string? path = null) => path;

        /// <summary>
        /// Overwrite the golden files in the source tree (not the build output), so that changes are
        /// picked up by git; failures are non-fatal, since the assertions are what actually gate.
        /// </summary>
        /// <param name="originFile">
        /// The calling test file's own path, from <see cref="GetOriginCodeLocation"/>. It has to be
        /// passed in rather than captured here: a <c>[CallerFilePath]</c> resolved in this base class
        /// would always name this file, and the goldens live beside whichever test suite wrote them.
        /// </param>
        protected static void WriteBack(string? originFile, string outputCodePath, string actualCode, string buildOutput)
        {
            if (originFile is null || Path.GetDirectoryName(originFile) is not string originFolder) return;

            // paths are relative to the build output, which mirrors the calling file's own folder
            var outputFirstDir = outputCodePath.Split(Path.DirectorySeparatorChar).First();
            if (originFolder.Split(Path.DirectorySeparatorChar).Last() == outputFirstDir)
            {
                outputCodePath = outputCodePath.Substring(outputFirstDir.Length + 1);
            }

            outputCodePath = Path.Combine(originFolder, outputCodePath);
            WriteOrDelete(outputCodePath, actualCode);
            WriteOrDelete(Path.ChangeExtension(outputCodePath, "txt"), buildOutput);
        }

        private static void WriteOrDelete(string path, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content)) File.Delete(path);
                else File.WriteAllText(path, content);
            }
            catch (IOException) { } // best-effort only
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// A fixture can pin its language version with a sibling <c>.langver</c> file, so that a
        /// generator's minimum-version handling can be exercised. Shared, because both generators
        /// have a floor and both need to prove it fires.
        /// </summary>
        protected static LanguageVersion ReadPinnedLanguageVersion(string path)
        {
            var langVerPath = Regex.Replace(path, @"\.input\.cs$", ".langver", RegexOptions.IgnoreCase);
            if (!File.Exists(langVerPath)) return LanguageVersion.Latest;

            var text = File.ReadAllText(langVerPath).Trim();
            Assert.True(LanguageVersionFacts.TryParse(text, out var langVersion),
                $"unable to parse language version '{text}' from {langVerPath}");
            return langVersion;
        }

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
            Action<TGenerator>? initializer = null,
            LanguageVersion languageVersion = LanguageVersion.Latest,
            IEnumerable<MetadataReference>? extraReferences = null,
            IEnumerable<(string Path, string Source)>? extraSources = null,
            ImmutableDictionary<string, string>? globalOptions = null)
            where TGenerator : class, IIncrementalGenerator, new()
        {
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "input.cs";

            var parseOptions = new CSharpParseOptions(languageVersion, DocumentationMode.Parse, SourceCodeKind.Regular);
            var references = MetadataReferenceHelpers.WellKnownReferences
                .Concat(MetadataReferenceHelpers.ProtoBufReferences)
                .Concat(extraReferences ?? Enumerable.Empty<MetadataReference>());

            // extraSources exists for the gRPC goldens, which need a snapshot of protobuf-net.Grpc's
            // surface in the compilation: that package cannot be *referenced* here, because BuildTools
            // compiles protobuf-net.Core's sources in and every type in Core would become ambiguous
            var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(source, parseOptions, path: fileName!) };
            if (extraSources is not null)
            {
                // Parsed at the fixture's language version, and it has to be: Roslyn rejects a
                // compilation whose trees disagree ("Inconsistent language versions"). So a fixture
                // pinning a low version pins it for the reference snapshot too, which is why the
                // snapshot's own floor - C# 8, for its nullable annotations - is also the floor for
                // anything pinning a version. Compiling the snapshot to a MetadataReference instead
                // would lift that, and would be a truer stand-in for a referenced package.
                foreach (var (path, text) in extraSources)
                {
                    trees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, path: path));
                }
            }

            var inputCompilation = CSharpCompilation.Create(
                "ProtoBuf.BuildTools.AotGeneratorTests",
                trees,
                references,
                CompilationOptions);

            // the input is *expected* to be incomplete on its own (the trigger attributes are supplied by
            // the generator), so input diagnostics are logged for debugging but never asserted on; the
            // output compilation contains the input trees, so checking it alone is sufficient
            LogDiagnostics("Input code", inputCompilation, diagnosticsTo: null);

            var generator = new TGenerator();
            initializer?.Invoke(generator);

            // global options exist so the ProtoBufDisableBuildTools switch can be exercised; without one
            // supplied, the provider is empty and every generator takes its normal path
            var optionsProvider = TestAnalyzeConfigOptionsProvider.Empty.WithGlobalOptions(
                new TestAnalyzerConfigOptions(globalOptions ?? ImmutableDictionary<string, string>.Empty));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { generator.AsSourceGenerator() }, parseOptions: parseOptions,
                optionsProvider: optionsProvider);
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
