#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace BuildToolsUnitTests.Grpc
{
    /// <summary>
    /// Test support for the gRPC proxy generator, where the interesting assertions are "what exact
    /// code came out" (tracked via golden files) and "does that code compile".
    /// </summary>
    public abstract class GrpcGeneratorTestBase
    {
        /// <summary>
        /// Set to <c>true</c> locally to rewrite the golden files from the current generator output;
        /// review the resulting diff, don't trust it.
        /// </summary>
        private const bool OverwriteGoldenFiles = false;

        private readonly ITestOutputHelper? _log;

        protected GrpcGeneratorTestBase(ITestOutputHelper? log = null) => _log = log;

        /// <summary>
        /// The protobuf-net.Grpc surface that generated code binds to; a snapshot, since this
        /// repository deliberately doesn't reference protobuf-net.Grpc.
        /// </summary>
        private static readonly Lazy<string> s_contractSurface = new(static ()
            => File.ReadAllText(Path.Combine(DataDirectory(), "_ContractSurface.cs")));

        private static readonly MetadataReference[] s_references = BuildReferences();

        private static MetadataReference[] BuildReferences()
        {
            // netstandard + System.Runtime are facades, so between them they cover everything the
            // fixtures and the generated code use (Task, ValueTask, IAsyncEnumerable, ...)
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            };
            return references.ToArray();
        }

        protected sealed class GeneratorResult
        {
            public GeneratorResult(string generatedCode, ImmutableArray<Diagnostic> generatorDiagnostics, ImmutableArray<Diagnostic> outputErrors)
            {
                GeneratedCode = generatedCode;
                GeneratorDiagnostics = generatorDiagnostics;
                OutputErrors = outputErrors;
            }

            /// <summary>All generated sources, concatenated in hint-name order with a banner per file.</summary>
            public string GeneratedCode { get; }

            public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

            /// <summary>Errors in the *output* compilation, i.e. the input plus the generated code.</summary>
            public ImmutableArray<Diagnostic> OutputErrors { get; }

            /// <summary>Assert that the generated code compiles, and that nothing was reported.</summary>
            public GeneratorResult AssertClean()
            {
                Assert.Empty(OutputErrors);
                Assert.Empty(GeneratorDiagnostics);
                return this;
            }

            public GeneratorResult AssertCompiles()
            {
                Assert.Empty(OutputErrors);
                return this;
            }

            public GeneratorResult AssertNoOutput()
            {
                Assert.Equal("", GeneratedCode);
                return this;
            }

            /// <summary>Assert exactly one diagnostic, with the given id.</summary>
            public Diagnostic AssertSingleDiagnostic(string id)
            {
                var diagnostic = Assert.Single(GeneratorDiagnostics);
                Assert.Equal(id, diagnostic.Id);
                return diagnostic;
            }
        }

        /// <summary>
        /// Run the generator over a fixture file, comparing the output against its golden file.
        /// </summary>
        protected GeneratorResult ExecuteFixture(string name, bool includeContractSurface = true)
        {
            var path = Path.Combine(DataDirectory(), name + ".input.cs");
            var result = Execute(File.ReadAllText(path), includeContractSurface, fileName: Path.GetFileName(path));
            AssertGolden(Path.Combine(DataDirectory(), name + ".output.cs"), result.GeneratedCode);
            return result;
        }

        /// <summary>
        /// Run the generator over some source.
        /// </summary>
        /// <param name="includeContractSurface">
        /// When <c>false</c>, protobuf-net.Grpc is absent from the compilation - which must make the
        /// generator stand down silently rather than emit code that cannot compile.
        /// </param>
        protected GeneratorResult Execute(
            string source,
            bool includeContractSurface = true,
            string? fileName = null,
            LanguageVersion languageVersion = LanguageVersion.Latest)
        {
            var parseOptions = new CSharpParseOptions(languageVersion, DocumentationMode.Parse, SourceCodeKind.Regular);
            var inputCompilation = Compile(source, parseOptions, includeContractSurface, fileName);

            // the input must be clean on its own; anything else makes the output assertions meaningless
            var inputErrors = Errors(inputCompilation);
            if (!inputErrors.IsEmpty)
            {
                Log("Input code:");
                foreach (var error in inputErrors) Log("    " + error);
                Assert.Empty(inputErrors);
            }

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new ProtoBuf.BuildTools.Generators.GrpcProxyGenerator().AsSourceGenerator() },
                parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var generatorDiagnostics);

            foreach (var result in driver.GetRunResult().Results)
            {
                if (result.Exception is not null) throw result.Exception;
            }

            var generated = Combine(driver.GetRunResult());
            if (generated.Length != 0) Log(generated);

            var outputErrors = Errors(outputCompilation);
            foreach (var error in outputErrors) Log("Output error: " + error);
            foreach (var diagnostic in generatorDiagnostics) Log("Generator: " + diagnostic);

            return new GeneratorResult(generated, generatorDiagnostics, outputErrors);
        }

        /// <summary>
        /// Build a compilation of the source plus (optionally) the protobuf-net.Grpc contract surface.
        /// </summary>
        /// <remarks>
        /// Every tree in a compilation must share one language version, and the surface is C# 8+, so a
        /// test that pins an older version has to supply its own stand-in instead.
        /// </remarks>
        protected static CSharpCompilation Compile(
            string source, CSharpParseOptions parseOptions, bool includeContractSurface = true, string? fileName = null)
        {
            var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(source, parseOptions, path: fileName ?? "input.cs") };
            if (includeContractSurface)
            {
                trees.Add(CSharpSyntaxTree.ParseText(s_contractSurface.Value, parseOptions, path: "_ContractSurface.cs"));
            }

            return CSharpCompilation.Create(
                "ProtoBuf.BuildTools.GrpcGeneratorTests",
                trees,
                s_references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static ImmutableArray<Diagnostic> Errors(Compilation compilation)
            => compilation.GetDiagnostics().Where(static x => x.Severity == DiagnosticSeverity.Error).ToImmutableArray();

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

        private void AssertGolden(string path, string actual)
        {
            // "nothing was emitted" is pinned by the caller's AssertNoOutput, not by an empty file
            if (actual.Length == 0 && !File.Exists(path)) return;

            if (OverwriteGoldenFiles || !File.Exists(path))
            {
                File.WriteAllText(path, actual);
                Log($"Golden file written: {path}");
                Assert.False(OverwriteGoldenFiles, "golden files were rewritten; set OverwriteGoldenFiles back to false");
                return;
            }

            // normalize line endings: the golden files travel through git, the generator uses Environment.NewLine
            Assert.Equal(
                File.ReadAllText(path).Replace("\r\n", "\n"),
                actual.Replace("\r\n", "\n"));
        }

        /// <summary>
        /// The fixture directory in the source tree, so golden files can be written back rather than
        /// landing in the build output.
        /// </summary>
        private static string DataDirectory([CallerFilePath] string path = "")
            => Path.Combine(Path.GetDirectoryName(path)!, "Data");

        private void Log(string message) => _log?.WriteLine(message);
    }
}
