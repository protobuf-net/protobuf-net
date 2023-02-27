using BuildToolsUnitTests.AOT.CSharpToCodeGen.Abstractions;
using ProtoBuf.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen;
using System.Linq;
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
        var codeGenSet = GetCodeGenSet("BasicEnum.cs", out _);
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
        var codeGenSet = GetCodeGenSet("EnumBasedOnByte.cs", out _);
        Assert.NotNull(codeGenSet);
        
        var @enum = codeGenSet.Files.First().Enums.First();
        Assert.NotNull(@enum);
        
        Assert.Equal(@enum.Type, CodeGenSimpleType.Byte);
    }
    
    [Fact]
    public void NoProtoContract_SavesWarning()
    {
        var codeGenSet = GetCodeGenSet("NoProtoContract.cs", out var diagnostics);
        Assert.NotNull(codeGenSet);

        var errors = diagnostics.ToArray();
        var (Id, Severity, Location, _) = errors.Single();
        Assert.Equal(CodeGenDiagnostic.DiagnosticSeverity.Warning, Severity);
        Assert.Equal("SourceFile(NoProtoContract.cs[447..453))", Location);
        Assert.Equal("PBN3003", Id);
    }
    
    [Fact]
    public void NoProtoEnum_SavesWarning()
    {
        var codeGenSet = GetCodeGenSet("NoProtoEnum.cs", out var diagnostics);
        Assert.NotNull(codeGenSet);

        var errors = diagnostics.ToArray();
        var (Id, Severity, Location, _) = errors.Single();
        Assert.Equal(CodeGenDiagnostic.DiagnosticSeverity.Warning, Severity);
        Assert.Equal("SourceFile(NoProtoEnum.cs[467..484))", Location);
        Assert.Equal("PBN3002", Id);
    }
    
    [Fact]
    public void NoProtoMember_SavesWarning()
    {
        var codeGenSet = GetCodeGenSet("NoProtoMember.cs", out var diagnostics);
        Assert.NotNull(codeGenSet);

        var errors = diagnostics.ToArray();
        var (Id, Severity, Location, _) = errors.Single();
        Assert.Equal(CodeGenDiagnostic.DiagnosticSeverity.Warning, Severity);
        Assert.Equal("SourceFile(NoProtoMember.cs[1009..1023))", Location);
        Assert.Equal("PBN3001", Id);
    }
}