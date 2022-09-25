using System.Linq;
using BuildToolsUnitTests.AOT.CSharpToCodeGen.Abstractions;
using ProtoBuf.Reflection.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen.Error;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.AOT.CSharpToCodeGen;

public class EnumAotTests : CSharpToCodeGenTestsBase
{
    private const string SchemaType = "Enums";

    public EnumAotTests(ITestOutputHelper output) : base(output, SchemaType)
    {
    }
    
    [Fact]
    public void BasicEnum_Passes()
    {
        var codeGenSet = GetCodeGenSet("BasicEnum.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);
        
        var @enum = codeGenSet.Files.First().Enums.First();
        Assert.NotNull(@enum);
        
        Assert.Equal("Corpus", @enum.Name);
        Assert.Equal(CodeGenSimpleType.Int32, @enum.Type);
        Assert.Equal(3, @enum.EnumValues.Count);
    }
    
    [Fact]
    public void EnumBasedOnByte_Passes()
    {
        var codeGenSet = GetCodeGenSet("EnumBasedOnByte.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);
        
        var @enum = codeGenSet.Files.First().Enums.First();
        Assert.NotNull(@enum);
        
        Assert.Equal(@enum.Type, CodeGenSimpleType.Byte);
    }
    
    [Fact]
    public void NoProtoContract_SavesWarning()
    {
        var codeGenSet = GetCodeGenSet("NoProtoContract.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);

        var errors = codeGenSet.ErrorContainer.Errors;
        Assert.NotEmpty(errors);

        var noProtoContractError = errors.First();
        Assert.Equal(CodeGenErrorLevel.Warning, noProtoContractError.Level);
        Assert.Contains("NoProtoContract.cs", noProtoContractError.Location);
        Assert.Contains("Corpus", noProtoContractError.SymbolType);
    }
    
    [Fact]
    public void NoProtoEnum_SavesWarning()
    {
        var codeGenSet = GetCodeGenSet("NoProtoEnum.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);

        var errors = codeGenSet.ErrorContainer.Errors;
        Assert.NotEmpty(errors);

        var noProtoEnumError = errors.First();
        Assert.Equal(CodeGenErrorLevel.Warning, noProtoEnumError.Level);
        Assert.Contains("NoProtoEnum.cs", noProtoEnumError.Location);
        Assert.Contains("CorpusUnspecified", noProtoEnumError.SymbolType);
    }
}