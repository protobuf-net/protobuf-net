using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.AOT;

public class AOTGeneratorTests : GeneratorTestBase<DataContractGenerator>
{
    private readonly ITestOutputHelper _log;

    public AOTGeneratorTests(ITestOutputHelper log) => _log = log;
    [Fact]
    public async Task ProtoContractOneTree()
    {
        var result = await RunAsync(@"[ProtoBuf.ProtoGenerate, ProtoBuf.ProtoContract] partial class Foo {}");
        Assert.Single(result.GeneratedTrees);
    }

    private Task<GeneratorDriverRunResult> RunAsync(params string[] docs) => RunAsync(null, docs);
    private Task<GeneratorDriverRunResult> RunAsync(Action<ImmutableArray<Diagnostic>>? validator, params string[] docs)
    {
        docs ??= Array.Empty<string>();
        var trees = new SyntaxTree[docs.Length];
        for (int i = 0; i < docs.Length; i++)
        {
            trees[i] = Code($"doc{i}.cs", docs[i]);
        }
        return RunAsync(validator, trees);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
    private Task<GeneratorDriverRunResult> RunAsync(params SyntaxTree[] docs) => RunAsync(null, docs);
    private async Task<GeneratorDriverRunResult> RunAsync(Action<ImmutableArray<Diagnostic>>? validator, params SyntaxTree[] docs)
    {
        docs ??= Array.Empty<SyntaxTree>();
        var (result, diagnostics) = await base.GenerateAsync(source: docs);
        foreach (var diag in diagnostics)
        {
            _log?.WriteLine(diag.ToString());
        }
        if (validator is null)
        {
            Assert.Empty(diagnostics);
        }
        else
        {
            validator(diagnostics);
        }
        SyntaxTree[] combined = docs;
        if (result.GeneratedTrees.Length > 0)
        {
            combined = new SyntaxTree[docs.Length + result.GeneratedTrees.Length];
            docs.CopyTo(combined, 0);
            result.GeneratedTrees.CopyTo(combined, docs.Length);

            foreach (var tree in result.GeneratedTrees)
            {
                _log?.WriteLine(tree.ToString());
            }
        }
        var emitResult = CreateCompilation(combined).Emit(Stream.Null);
        foreach (var diag in emitResult.Diagnostics)
        {
            if (diag.Severity >= DiagnosticSeverity.Error)
            {
                _log?.WriteLine(diag.ToString());
            }
        }
        Assert.True(emitResult.Success, "source+generated does not compile");
        return result;
    }

    [Fact]
    public async Task NotContractNoTrees()
    {
        var result = await RunAsync(@"partial class Foo {}");
        Assert.Empty(result.GeneratedTrees);
    }

    [Fact]
    public async Task HazMap()
    {
        var result = await RunAsync(diag =>
        {
            Assert.Equal("PBN4001", Assert.Single(diag.Select(x => x.Id).Distinct()));
        }, @"
[assembly:ProtoBuf.ProtoGenerate]
[ProtoBuf.ProtoContract]
partial class Foo {
    [ProtoBuf.ProtoMember(1)]
    public System.Collections.Generic.Dictionary<int, string> Values {get;} = new();
}");
        Assert.Single(result.GeneratedTrees);
    }

    [Fact]
    public async Task PartialOverTwoTrees()
    {
        var result = await RunAsync(@"
[module:ProtoBuf.ProtoGenerate]
partial class Foo {}", @"
using ProtoBuf;
[ProtoContract] partial class Foo {}");
        Assert.Single(result.GeneratedTrees);
    }
}
