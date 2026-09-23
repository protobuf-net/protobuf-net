using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    public class NonNullableCollectionCodeFixProviderTests : CodeFixProviderTestsBase<NonNullableCollectionCodeFixProvider>
    {
        private readonly DiagnosticResult[] _standardExpectedDiagnostics = new[] {
            new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel)
        };

        private Task RunAsync(string source, string expected, string key)
            => RunCodeFixTestAsync<DataContractAnalyzer>(Wrap(source), Wrap(expected),
                codeActionEquivalenceKey: key, standardExpectedDiagnostics: _standardExpectedDiagnostics);

        private static string Wrap(string body) => @"#nullable enable
using ProtoBuf;
using System.Collections.Generic;
using PM = ProtoBuf.ProtoMemberAttribute;
" + body;

        [Theory]
        // the `= null!` existed only to silence CS8618, and goes with it
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public List<int> {|PBN0027:Items|} { get; set; } = null!; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public List<int>? Items { get; set; } }")]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public string[] {|PBN0027:Items|} = default!; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public string[]? Items; }")]
        // a real initializer stays: it is just not enough under SkipConstructor
        [InlineData(
            "[ProtoContract(SkipConstructor = true)] public class Foo { [ProtoMember(1)] public string[] {|PBN0027:Items|} = new string[0]; }",
            "[ProtoContract(SkipConstructor = true)] public class Foo { [ProtoMember(1)] public string[]? Items = new string[0]; }")]
        // the motivating record: the parameter's type is what declares the property's
        [InlineData(
            "[ProtoContract(SkipConstructor = true)] public record TestRecord([property: ProtoMember(1)] string[] {|PBN0027:Array|});",
            "[ProtoContract(SkipConstructor = true)] public record TestRecord([property: ProtoMember(1)] string[]? Array);")]
        public Task DeclaresNullable(string source, string expected)
            => RunAsync(source, expected, NonNullableCollectionCodeFixProvider.NullableKey);

        [Theory]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public List<int> {|PBN0027:Items|} { get; set; } }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public List<int> Items { get; set; } = new(); }")]
        // a spelled-out null is replaced rather than added to
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public Dictionary<int, string> {|PBN0027:Items|} { get; set; } = null!; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public Dictionary<int, string> Items { get; set; } = new(); }")]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public string[] {|PBN0027:Items|} = default!; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public string[] Items = System.Array.Empty<string>(); }")]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public required HashSet<string> {|PBN0027:Items|} { get; init; } }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public required HashSet<string> Items { get; init; } = new(); }")]
        public Task Initializes(string source, string expected)
            => RunAsync(source, expected, NonNullableCollectionCodeFixProvider.InitializeKey);

        [Theory]
        [InlineData(
            "[ProtoContract(SkipConstructor = true)] public class Foo { [ProtoMember(1)] public List<int> {|PBN0027:Items|} { get; set; } = new(); }",
            "[ProtoContract(SkipConstructor = true)] public class Foo { [ProtoMember(1), NullWrappedCollection] public List<int> Items { get; set; } = new(); }")]
        // lands in the list that holds [ProtoMember], so the record parameter keeps its `property:` target
        [InlineData(
            "[ProtoContract(SkipConstructor = true)] public record TestRecord([property: ProtoMember(1)] string[] {|PBN0027:Array|});",
            "[ProtoContract(SkipConstructor = true)] public record TestRecord([property: ProtoMember(1), NullWrappedCollection] string[] Array);")]
        // however [ProtoMember] is spelled, the new attribute is as short as the using directives allow
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoBuf.ProtoMemberAttribute(1)] public IList<int> {|PBN0027:Items|} { get; set; } = null!; }",
            "[ProtoContract] public class Foo { [ProtoBuf.ProtoMemberAttribute(1), NullWrappedCollection] public IList<int> Items { get; set; } = null!; }")]
        [InlineData(
            "[ProtoContract] public class Foo { [PM(1)] public IList<int> {|PBN0027:Items|} { get; set; } = null!; }",
            "[ProtoContract] public class Foo { [PM(1), NullWrappedCollection] public IList<int> Items { get; set; } = null!; }")]
        public Task NullWraps(string source, string expected)
            => RunAsync(source, expected, NonNullableCollectionCodeFixProvider.NullWrapKey);

        // each remedy is offered only where it works: an initializer needs a constructor that runs
        // and a type we can name without guessing, and null-wrapping needs a member that is written
        [Theory]
        [InlineData("[ProtoContract] public class Foo { [ProtoMember(1)] public List<int> Items { get; set; } = null!; }",
            "PBN0027.Nullable", "PBN0027.Initialize", "PBN0027.NullWrap")]
        [InlineData("[ProtoContract(SkipConstructor = true)] public class Foo { [ProtoMember(1)] public List<int> Items { get; set; } = new(); }",
            "PBN0027.Nullable", "PBN0027.NullWrap")]
        [InlineData("[ProtoContract] public struct Foo { [ProtoMember(1)] public List<int> Items; }",
            "PBN0027.Nullable", "PBN0027.NullWrap")]
        [InlineData("[ProtoContract(SkipConstructor = true)] public record TestRecord([property: ProtoMember(1)] string[] Array);",
            "PBN0027.Nullable", "PBN0027.NullWrap")]
        [InlineData("[ProtoContract] public class Foo { public List<int> Items { get; set; } = null!; }",
            "PBN0027.Nullable", "PBN0027.Initialize")]
        [InlineData("[ProtoContract] public class Foo { [ProtoMember(1)] public IList<int> Items { get; set; } = null!; }",
            "PBN0027.Nullable", "PBN0027.NullWrap")]
        // `List<int>?` would change A as well as B, so only the initializer is offered
        [InlineData("[ProtoContract] public class Foo { public List<int> A = new(), B; }",
            "PBN0027.Initialize")]
        public async Task OffersOnlyWhatWorks(string source, params string[] expected)
        {
            var offered = await OfferedFixesAsync(Wrap(source));
            Assert.Equal(expected, offered);
        }

        private static async Task<List<string>> OfferedFixesAsync(string source)
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("fixes", LanguageNames.CSharp)
                .WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(MetadataReferenceHelpers.ProtoBufReferences)
                .AddMetadataReferences(MetadataReferenceHelpers.WellKnownReferences);
            var document = project.AddDocument("fixes.cs", source + @"
namespace System.Runtime.CompilerServices { public class IsExternalInit {} }");
            var compilation = (await document.Project.GetCompilationAsync())!;
            var diagnostics = await compilation
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DataContractAnalyzer()))
                .GetAnalyzerDiagnosticsAsync();
            var diagnostic = Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.NonNullableCollectionLeftNull);

            var offered = new List<string>();
            var context = new CodeFixContext(document, diagnostic, (action, _) => offered.Add(action.EquivalenceKey!), CancellationToken.None);
            await new NonNullableCollectionCodeFixProvider().RegisterCodeFixesAsync(context);
            return offered;
        }
    }
}
