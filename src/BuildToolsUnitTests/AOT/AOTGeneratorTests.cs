using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.AOT;

public class AOTGeneratorTests : GeneratorTestBase<DataContractGenerator>
{
    [Fact]
    public async Task ProtoContractOneTree()
    {
        SyntaxTree[] docs = 
        {
            Code("my.cs", @"[ProtoBuf.ProtoContract] partial class Foo {}"),
        };
        var result = await base.GenerateAsync(source: docs);
        Assert.True(result.Diagnostics.IsEmpty);
        Assert.Single(result.Result.GeneratedTrees);
    }

    [Fact]
    public async Task NotContractNoTrees()
    {
        SyntaxTree[] docs =
        {
            Code("my.cs", @"partial class Foo {}"),
        };
        var result = await base.GenerateAsync(source: docs);
        Assert.True(result.Diagnostics.IsEmpty);
        Assert.Empty(result.Result.GeneratedTrees);
    }

    [Fact]
    public async Task PartialOverTwoTrees()
    {
        SyntaxTree[] docs =
        {
            Code("my.cs", @"partial class Foo {}"),
            Code("myother.cs", @"
using ProtoBuf;
[ProtoContract] partial class Foo {}"),
        };
        var result = await base.GenerateAsync(source: docs);
        Assert.True(result.Diagnostics.IsEmpty);
        Assert.Single(result.Result.GeneratedTrees);
    }
}
