using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Nano;
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
    public abstract class GeneratorTestBase<TGenerator> where TGenerator : ISourceGenerator
    {
        public SyntaxTree Code(string fileName, string source) => CSharpSyntaxTree.ParseText(source).WithFilePath(fileName);

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

        protected Task<(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics)> GenerateAsync(AdditionalText[]? additionalTexts = null, SyntaxTree[]? source = null, ImmutableDictionary<string, string>? globalOptions = null,
            [CallerMemberName] string? callerMemberName = null, bool debugLog = true)
        {
            if (!typeof(TGenerator).IsDefined(typeof(GeneratorAttribute)))
            {
                throw new InvalidOperationException($"Type is not marked [Generator]: {typeof(TGenerator)} in {callerMemberName}");
            }

            var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);

            globalOptions ??= ImmutableDictionary<string, string>.Empty;
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
            var compilation = CreateCompilation(source);
            var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
            if (_testOutputHelper is object)
            {
                foreach (var d in diagnostics)
                {
                    _testOutputHelper.WriteLine(d.ToString());
                }
            }
            return Task.FromResult((result.GetRunResult(), diagnostics));
        }

        protected virtual bool ReferenceProtoBuf => true;

        protected Compilation CreateCompilation(SyntaxTree[]? source)
        {
            source ??= Array.Empty<SyntaxTree>();
            List<MetadataReference> refs = new(10)
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
            };
            if (ReferenceProtoBuf)
            {
                refs.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location));
                refs.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));
                refs.Add(MetadataReference.CreateFromFile(typeof(TypeModel).Assembly.Location));
                refs.Add(MetadataReference.CreateFromFile(typeof(Writer).Assembly.Location));
            }
            return CSharpCompilation.Create("compilation", source, refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
