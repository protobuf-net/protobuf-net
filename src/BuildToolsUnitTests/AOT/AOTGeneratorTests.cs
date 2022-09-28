using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System;
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
        var result = await RunAsync(@"[ProtoBuf.ProtoContract] partial class Foo {}");
        Assert.Single(result.GeneratedTrees);
    }

    private Task<GeneratorDriverRunResult> RunAsync(params string[] docs)
    {
        docs ??= Array.Empty<string>();
        var trees = new SyntaxTree[docs.Length];
        for (int i = 0; i < docs.Length; i++)
        {
            trees[i] = Code($"doc{i}.cs", docs[i]);
        }
        return RunAsync(trees);
    }
    private async Task<GeneratorDriverRunResult> RunAsync(params SyntaxTree[] docs)
    {
        var (result, diagnostics) = await base.GenerateAsync(source: docs);
        foreach (var diag in diagnostics)
        {
            _log?.WriteLine(diag.ToString());
        }
        Assert.Empty(diagnostics);
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
        var result = await RunAsync(@"
[ProtoBuf.ProtoContract]
partial class Foo {
    [ProtoBuf.ProtoMember(1)]
    public System.Collections.Generic.Dictionary<int, string> Values {get;} = new();
}");
        Assert.Empty(result.GeneratedTrees);
    }

    [Fact]
    public async Task PartialOverTwoTrees()
    {
        var result = await RunAsync(@"partial class Foo {}", @"
using ProtoBuf;
[ProtoContract] partial class Foo {}");
        Assert.Single(result.GeneratedTrees);
    }
}
