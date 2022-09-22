using System.Linq;
using BuildToolsUnitTests.AOT.CSharpToCodeGen.Abstractions;
using ProtoBuf.Reflection.Internal.CodeGen;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.AOT.CSharpToCodeGen;

public class ServicesAotTests : CSharpToCodeGenTestsBase
{
    private const string ServicesSchemaType = "Services";

    public ServicesAotTests(ITestOutputHelper output) : base(output, ServicesSchemaType)
    {
    }
    
    [Fact]
    public void RawReturnType_Passes()
    {
        var codeGenSet = GetCodeGenSet("RawReturnTypecs.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);
        
        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.Raw, serviceMethod.ResponseType.Representation);
    }
    
    [Fact]
    public void StreamableResponse_Passes()
    {
        var codeGenSet = GetCodeGenSet("IAsyncEnumerableReturnType.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);

        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.AsyncEnumerable, serviceMethod.ResponseType.Representation);
    }
    
    [Fact]
    public void TaskReturnType_Passes()
    {
        var codeGenSet = GetCodeGenSet("TaskReturnType.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);

        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.Task, serviceMethod.ResponseType.Representation);
    }
    
    [Fact]
    public void ValueTaskReturnType_Passes()
    {
        var codeGenSet = GetCodeGenSet("ValueTaskReturnType.cs") as CodeGenSet;
        Assert.NotNull(codeGenSet);

        var serviceMethod = codeGenSet.Files.First().Services.First().ServiceMethods.First();
        Assert.NotNull(serviceMethod);
        Assert.Equal(CodeGenTypeRepresentation.ValueTask, serviceMethod.ResponseType.Representation);
    }
}